using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Scal.Interpreting.Commands.Extensions;

namespace Scal.Interpreting.Commands;

/// Model the metadata concerning a command type.
/// <param name="Type">The type of the command.</param>
/// <param name="Verb">The full command verb name.</param>
/// <param name="Noun">The full command noun name, may be empty.</param>
/// <param name="Description">The optional help description text..</param>
/// <param name="Parameters">The array of parameters, i.e. the type properties.</param>
public record CommandType(
    Type Type,
    string Verb,
    string Noun,
    string? Description,
    CommandParameter[] Parameters
) {

    /// The minimal verb abbreviation required to avoid abiguity.
    public string VerbAbbreviation { get; private set; } = Verb;

    /// The minimal noun abbreviation required to avoid abiguity.
    public string NounAbbreviation { get; private set; } = Noun;

    #region Static Methods

    /// Build an array of fully defined descriptors for command type deriving from the base type.
    /// <param name="baseType">The base type the commands are derived from.</param>
    /// <returns>Returns the builded command type descriptors array.</returns>
    public static CommandType[] GetCommandTypes(Type baseType)
    {
        var commandTypes = DiscoverCommandTypes(baseType);
        EvaludateAbbreviations(commandTypes);
        return commandTypes.ToArray();
    }

    /// Discover the command type deriving from the base type.
    /// <param name="baseType">The base type the commands are derived from.</param>
    /// <returns>Returns the list of discovered command types.</returns>
    private static List<CommandType> DiscoverCommandTypes(Type baseType)
    {
        List<CommandType> commandTypes = [];
        foreach (var type in baseType.EnumerateDerivedTypes().Where(type => ! type.IsAbstract)) {
            // Cannot use TypeDescriptor.GetAttributes because it returns base class attributes also
            var attributes  = type.GetCustomAttributes(inherit: false);
            var contract    = attributes.OfType<DataContractAttribute>().FirstOrDefault();
            var nameParts   = type.Name.SplitCamelCase().ToArray();
            var verb        = contract?.Namespace ?? nameParts[0];
            var noun        = (contract is null) ? (nameParts.Length > 1 ? nameParts[1] : string.Empty) : contract?.Name ?? string.Empty;
            commandTypes.Add(new CommandType(type, verb, noun,
                attributes.OfType<DescriptionAttribute>().FirstOrDefault()?.Description,
                TypeDescriptor.GetProperties(type)
                    .Cast<PropertyDescriptor>()
                    .Select(property => new CommandParameter(property, property.Name,
                        property.Attributes.OfType<DescriptionAttribute>().FirstOrDefault()?.Description
                    )).ToArray()
            ));
        }
        return commandTypes;
    }

    /// Evaluate the minimum abbreviations of a discovered list of command types.
    /// <param name="commandTypes">The list of discovered command types to evaluate.</param>
    private static void EvaludateAbbreviations(List<CommandType> commandTypes)
    {
        var verbs   = commandTypes.Select(commandType => commandType.Verb).ToArray();
        foreach (var commandType in commandTypes) {
            var nouns       = commandTypes
                .Where(cmdType => StringComparer.OrdinalIgnoreCase.Equals(cmdType.Verb, commandType.Verb))
                .Select(cmdType => cmdType.Noun).ToArray();
            commandType.VerbAbbreviation = commandType.Verb.GetMinimalAbbreviation(verbs);
            commandType.NounAbbreviation = commandType.Noun.GetMinimalAbbreviation(nouns);
            var parameters  = commandType.Parameters.Select(parameter => parameter.Name).ToArray();
            foreach (var parameter in commandType.Parameters) {
                parameter.NameAbbreviation = parameter.Name.GetMinimalAbbreviation(parameters);
            }
        }
    }

    #endregion
    #region Properties

    /// Determine if this command has a noun or is verb-only.
    /// Returns true if command has a noun or false if it is a verb-only command.
    public bool IsVerbOnly => string.IsNullOrWhiteSpace(this.Noun);

    #endregion
    #region Methods

    /// Get the array of parameters matching the given name which is possibly an abbreviation.
    /// <param name="name">The parameter name or abbreviation.</param>
    /// <returns>Returns an array of all the matching parameters, 0 unknown, 1 found, more if there is ambiguity.</returns>
    public CommandParameter[] GetParameters(string name)
    {
        var exact = this.Parameters
            .Where(parameter => string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exact.Length == 1) {
            return exact;
        }
        return this.Parameters
            .Where(parameter => parameter.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// Test whether this command type correspodnds to the verb/noun pair.
    /// <param name="verb">The verb to test the match of.</param>
    /// <param name="noun">The noun to test the match of.</param>
    /// <returns>Returns true if matching, otherwise false.</returns>
    public bool IsMatchingCommand(string verb, string noun)
    {
        return (
            this.Verb.StartsWith(verb, StringComparison.OrdinalIgnoreCase) &&
            (
                this.IsVerbOnly ||
                (
                    ! string.IsNullOrWhiteSpace(noun) &&
                    this.Noun.StartsWith(noun, StringComparison.OrdinalIgnoreCase)
                )
            )
        );
    }

    /// Test whether this command accepts the specified parameters.
    /// <param name="parameters">The parameters to test the acceptance of.</param>
    /// <param name="results">The list of validation results to fill with ambiguity error messages, if any.</param>
    /// <returns>Returns true if matching without ambiguity, otherwise false.</returns>
    public bool IsMatchingParameters(IEnumerable<string> parameters, List<ValidationResult> results)
    {
        return parameters.All(parameterName => {
            var matching = this.GetParameters(parameterName);
            if (matching.Length > 1) {
                results.Add(new ValidationResult("Parameter abbreviation is ambiguous", [ parameterName ]));
            }
            return matching.Length == 1;
        });
    }

    #endregion
};
