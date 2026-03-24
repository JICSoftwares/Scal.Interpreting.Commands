using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Scal.Interpreting.Commands;

/// The command-line interpreter, core of this library.
/// <param name="prefixes">The optional array of parameter prefixes to trim from parameters.</param>
/// <param name="delimiters">The optional array of delimiters to trim from parameters.</param>
/// <param name="factory">The optional factory delegate to create the command instance.</param>
/// <param name="responseFilePath">Path to use to find response files when theit path is not rooted.</param>
public partial class CommandLineInterpreter(
    char[]?                 prefixes = null,
    char[]?                 delimiters = null,
    Func<Type, object?>?    factory = null,
    string?                 responseFilePath = null
) {

    /// The optional array of parameter prefixes to trim from parameters.
    public char[] Prefixes { get; } = prefixes ?? ['-', '/'];

    /// he optional array of delimiters to trim from parameters.
    public char[] Delimiters { get; } = delimiters ?? ['"', '\''];

    /// The optional factory delegate to create the command instance.
    public Func<Type, object?>? Factory { get; set; } = factory;

    /// Path to use to find response files when theit path is not rooted.
    public string? ResponseFilePath { get; set; } = responseFilePath;

    /// Maximum depth when response files references other response files.
    public const int MaximumResponseFileDepth = 9;

    /// The character used to indicate parameters must be read from a response flie. 
    public const char ResponseFileMarker = '@';

    private const string ArgumentPattern = """ ( ((?<Arg>(\@)?\"[^\"]+\")) | (?<Arg>(\@)?[^\s\"]+) ) """;
    private const string ResponseFileLinePattern = "(?x) ^" + ArgumentPattern + @"( \s" + ArgumentPattern + ")* $";

    /// The expression used to parse a response file line.
    [GeneratedRegex(ResponseFileLinePattern)]
    public static partial Regex ResponseFileLineExpression();


    /// Interpret the given arguments to create an instance of a class deriving from TCommand.
    /// <param name="args">The array of command-line arguments.</param>
    /// <typeparam name="TCommand">The base type the commands are derived from.</typeparam>
    /// <returns>Returns the command interpretation result containing either a command instance or error results.</returns>
    public CommandInterpretation<TCommand> Interpret<TCommand>(string[] args)
        where TCommand: class
    {
        string[] allArgs    = [];
        var interpretation  = new CommandInterpretation<TCommand>(allArgs);
        Dictionary<string, string> pairParameters   = [];
        Dictionary<string, string> verbParameters   = [];
        try {
            allArgs         = this.GetRawParameters(args).ToArray();
        } catch (Exception exception) {
            interpretation.Results.Add(new ValidationResult(exception.Message));
            return interpretation;
        }
        var verb            = (allArgs.Length >= 1) ? allArgs[0] : null;
        var noun            = (allArgs.Length >= 2) ? allArgs[1] : null;
        try {
            pairParameters  = this.GetParametersDictionary(allArgs.Skip(2));
            try {
                verbParameters  = this.GetParametersDictionary(allArgs.Skip(1));
            } catch (ArgumentException ) { // May occur when an abbreviated parameter is the same as the noun.
                verbParameters  = pairParameters;
            }
        } catch (Exception exception) {
            interpretation.Results.Add(new ValidationResult(exception.Message));
        }
        if (string.IsNullOrWhiteSpace(verb)) {
            interpretation.Results.Add(new ValidationResult("Usage: verb (noun) (parameters)"));
        }
        if (interpretation.Results.Any()) { // Because of previous line or because of instantiation issue.
            return interpretation;
        }
        var knownCommandTypes       = interpretation.CommandTypes;
        interpretation.CommandTypes = interpretation.CommandTypes
            .Where(commandType => commandType.IsMatchingCommand(verb!, noun ?? string.Empty))
            .OrderBy(commandType => $"{commandType.Verb}.{commandType.Noun}")
            .ToArray();
        if (interpretation.CommandTypes.Length > 0) {
            var commandTypes = interpretation.CommandTypes
                .Where(commandType => commandType.IsMatchingParameters(
                    commandType.IsVerbOnly ? verbParameters.Keys : pairParameters.Keys,
                    interpretation.Results
                ))
                .ToArray();
            // Preserve list of command to show help when ambiguous parameters found
            if (commandTypes.Length == 1) {
                interpretation.CommandTypes = commandTypes;
                interpretation.Parameters =  interpretation.CommandTypes[0].IsVerbOnly ? verbParameters : pairParameters;
            }
        }
        switch (interpretation.CommandTypes.Length) {
            case 1:
                if (interpretation.Results.Any()) {
                    return interpretation;
                }
                break;
            case 0:
                if (! interpretation.Results.Any()) {
                    var commandDisplay = $"\"{string.Join("\" \"", allArgs)}\"";
                    interpretation.Results.Add(new ValidationResult($"Unknown command: {commandDisplay}"));
                    interpretation.CommandTypes = knownCommandTypes;
                }
                return interpretation;
            default:
                interpretation.Results.Add(new ValidationResult($"Ambiguous command: {verb} {noun}"));
                return interpretation;
        }
        interpretation.CreateCommand(this.Factory);
        return interpretation;
    }

    /// Get the dictionary of name/value pairs parameters.
    /// <param name="args">The command-line arguments.</param>
    /// <returns>Return the builded parameters dictionary.</returns>
    private Dictionary<string, string> GetParametersDictionary(IEnumerable<string> args)
    {
        return args
            .Where(arg => ! string.IsNullOrWhiteSpace(arg))
            .Select(arg => {
                var parts = arg.Split([ '=' ], 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return KeyValuePair.Create(parts[0].TrimStart(this.Prefixes).Trim(this.Delimiters),
                    (parts.Length > 1) ? parts[1].Trim(this.Delimiters) : string.Empty
                );
            })
            .ToDictionary();
    }

    /// Get the raw parameters from either the supplied arguments or response file(s).
    /// <param name="args">The command-line arguments.</param>
    /// <param name="depth">The response file depth.</param>
    /// <returns>Return the enumeration of the parameters given in arguments or found in response file(s).</returns>
    private IEnumerable<string> GetRawParameters(string[] args, int depth = 0)
    {
        if (depth > MaximumResponseFileDepth) {
            throw new InvalidOperationException($"Maximum response file depth {MaximumResponseFileDepth} reached");
        }
        foreach (var argSource in args.Where(arg => ! string.IsNullOrWhiteSpace(arg))) {
            var arg = argSource.Trim(this.Delimiters);
            if (arg.StartsWith(ResponseFileMarker)) {
                foreach (var readArg in this.ReadResponseFile(arg.Substring(1), depth + 1)) {
                    yield return readArg;
                }
            } else {
                yield return arg;
            }
        }
    }

    /// Read a response file.
    /// <param name="path">The path to the response file in <see cref="ResponseFilePath"/> if path is not rooted.</param>
    /// <param name="depth">The response file depth.</param>
    /// <returns>Return the enumeration of the parameters found in response file(s).</returns>
    private IEnumerable<string> ReadResponseFile(string path, int depth)
    {
        if ((! Path.IsPathRooted(path)) && (! string.IsNullOrWhiteSpace(this.ResponseFilePath))) {
            path = Path.Combine(this.ResponseFilePath, path);
        }
        if (! File.Exists(path)) {
            throw new FileNotFoundException($"Cannot find response file {path}", path);
        }
        path = Path.GetFullPath(path);
        if (this.ResponseFilePath is null) {
            this.ResponseFilePath = Path.GetDirectoryName(path);
        }
        foreach (var line in File.ReadLines(path).Where(line => ! string.IsNullOrWhiteSpace(line))) {
            var matches = ResponseFileLineExpression().Matches(line);
            var args = matches
                .SelectMany(match => match.Groups.Cast<Group>())
                .Where(group => group.Name[0] > '9') // Skip groups without name
                .SelectMany(group => group.Captures
                    .Where(capture => capture.Length > 0)
                    .Select(capture => capture.Value)
                )
                .ToArray();
            foreach (var arg in this.GetRawParameters(args, depth)) {
                yield return arg;
            }
        }
    }

}
