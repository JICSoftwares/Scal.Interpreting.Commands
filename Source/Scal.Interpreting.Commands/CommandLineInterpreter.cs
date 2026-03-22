using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Scal.Interpreting.Commands;

/// The command-line interpreter, core of this library.
/// <param name="prefixes">The optional array of parameter prefixes to trim from parameters.</param>
/// <param name="delimiters">The optional array of delimiters to trim from parameters.</param>
/// <param name="factory">The optional factory delegate to create the command instance.</param>
public class CommandLineInterpreter(
    char[]?                 prefixes = null,
    char[]?                 delimiters = null,
    Func<Type, object?>?    factory = null
) {

    /// The optional array of parameter prefixes to trim from parameters.
    public char[] Prefixes { get; } = prefixes ?? ['-', '/'];

    /// he optional array of delimiters to trim from parameters.
    public char[] Delimiters { get; } = delimiters ?? ['"', '\''];

    /// The optional factory delegate to create the command instance.
    public Func<Type, object?>? Factory { get; set; } = factory;

    /// Interpret the given arguments to create an instance of a class deriving from TCommand.
    /// <param name="args">The array of command-line arguments.</param>
    /// <typeparam name="TCommand">The base type the commands are derived from.</typeparam>
    /// <returns>Returns the command interpretation result containing either a command instance or error results.</returns>
    public CommandInterpretation<TCommand> Interpret<TCommand>(string[] args)
        where TCommand: class
    {
        var verb            = (args.Length >= 1) ? args[0] : null;
        var noun            = (args.Length >= 2) ? args[1] : null;
        var interpretation  = new CommandInterpretation<TCommand>(args);
        Dictionary<string, string> pairParameters   = [];
        Dictionary<string, string> verbParameters   = [];
        try {
            pairParameters  = this.GetParametersDictionary(args.Skip(2));
            try {
                verbParameters  = this.GetParametersDictionary(args.Skip(1));
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
                    interpretation.Results.Add(new ValidationResult($"Unknown command: {verb} {noun}"));
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

}
