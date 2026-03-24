using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

namespace Scal.Interpreting.Commands;

/// Command interpretation result class.
/// <typeparam name="TCommand">The base type the commands are derived from.</typeparam>
public class CommandInterpretation<TCommand>()
    where TCommand : class
{

    /// The array of arguments as received from the program's Main entrypoint.
    public string[] Args { get; internal set; } = [];

    /// The instantiated command after interpretation, if possible, otherwise null.
    public TCommand? Command { get; set; }

    /// The full or reduced array of commands matching the verb/noun pair.
    public CommandType[] CommandTypes { get; set; } = CommandType.GetCommandTypes(typeof(TCommand));

    /// The dictionary of parameters name/value pairs.
    public Dictionary<string, string> Parameters { get; set; } = [];

    /// The list of validation results after interpretation, on successful interpretation this list must be empty.
    public List<ValidationResult> Results { get; } = [];

    /// Create an intance of the interpreted command if possible and if there is no ambiguity.
    /// <param name="factory">The optional factory delegate to create the command instance.</param>
    public void CreateCommand(Func<Type, object?>? factory = null)
    {
        if (this.CommandTypes.Length != 1) {
            if (! this.Results.Any()) {
                this.Results.Add(new ValidationResult($"Expecting one command type to instantiate but got {this.CommandTypes.Length}"));
            }
            return;
        }
        var commandType = this.CommandTypes[0];
        try {
            if (factory is not null) {
                this.Command = factory(commandType.Type) as TCommand;
            }
            this.Command ??= (TCommand?)Activator.CreateInstance(commandType.Type);
        } catch (Exception exception) {
            this.Results.Add(new ValidationResult(exception.Message, [ commandType.Type.Name ]));
        }
        if (this.Command is not null) {
            foreach (var parameterKvp in this.Parameters) {
                try {
                    var matches = commandType.GetParameters(parameterKvp.Key);
                    if (matches.Length == 1) {
                        matches[0].SetValue(this.Command, parameterKvp.Value);
                    }
                } catch (Exception exception) {
                    this.Results.Add(new ValidationResult(exception.Message, [ parameterKvp.Key ]));
                }
            }
            Validator.TryValidateObject(this.Command, new ValidationContext(this.Command), this.Results, validateAllProperties: true);
        }
        if (this.Results.Any()) {
            this.Command = default;
        }
    }

    /// Emit feedback strings, i.e. the possible error messages and the context-aware help texts.
    /// <param name="feedbackAction">The delegate to invoke to emit the feedback (e.g. <see cref="Console.WriteLine()"/>).</param>
    /// <param name="showHelp">True to emit help texts, false to only emit title and errors.</param>
    public void Feedback(Action<string> feedbackAction, bool showHelp = true)
    {
        var attributes  = typeof(TCommand).GetCustomAttributes(inherit: false);
        var contract    = attributes.OfType<DataContractAttribute>().FirstOrDefault();
        var description = attributes.OfType<DescriptionAttribute>().FirstOrDefault();
        if ((contract is not null) || (description is not null)) {
            feedbackAction($"{contract?.Name} {description?.Description}");
        }
        foreach (var result in this.Results) {
            feedbackAction($"*** {string.Join(", ", result.MemberNames)}: {result.ErrorMessage}");
        }
        if (! showHelp) {
            return;
        }
        foreach (var type in this.CommandTypes) {
            feedbackAction($"  {type.Verb,-8} {type.Noun,-16} {type.Description} ({type.VerbAbbreviation} {type.NounAbbreviation})");
            foreach (var parameter in type.Parameters) {
                feedbackAction($"    {parameter.Name,-23} {parameter.Description} ({parameter.NameAbbreviation})");
            }
        }
    }
    
};
