# Scal.Interpreting.Commands

A lightweight, deterministic command-line interpreter for DotNet with attribute-based validation and type conversion.

Unlike reflection-heavy or attribute-mandatory CLI frameworks, **Scal.Interpreting.Commands** prioritizes deterministic resolution and minimal dependencies.

## Motivation

### Philosophy

- Simple deterministic grammar: **verb (noun) (arguments)**
- Case-insensitive
- Dash-tolerant but dash-agnostic
- Accept abbreviations but detect collisions
- Bulletproof and predictable behavior

### Technical features

- Verb/noun definition by attributes or Pascal-case naming convention
- Strongly-typed command instantiation
- Validation via DataAnnotations
- TypeConverter support
- Contextual help generation
- Dependency-free
- DI-agnostic construction
- DotNet 8.0 LTS compatible (console or AspNet)
- Lightweight (425 lines in total including comments, 4 classes and 2 extensions)

## Dependencies

None.

## Syntax

![CommandLine-ebnf](https://raw.githubusercontent.com/Scal-Human/Scal.Interpreting.Commands/refs/heads/main/Documentation/CommandLine-ebnf.svg)

## Usage example

```cli
dotnet add package Scal.Interpreting.Commands
```

Example of a program accepting as **List Image Name=abc** command

```c#
[DataContract(Name = "CliArgs")]
[Description("Cli arguments interpreter example")]
public abstract class Program
{
    private static async Task<int> Main(string[] args)
    {
        var interpretation = new CommandLineInterpreter().Interpret<Program>(args);
        if (interpretation.Command is null) {
            interpretation.Feedback(Console.WriteLine);
            return 1;
        }
        interpretation.Feedback(Console.WriteLine, showHelp: false);
        return await interpretation.Command.ExecuteAsync();
    }

    public abstract Task<int> ExecuteAsync();

    [Description("List the images")]
    [DataContract(Namespace = "List", Name = "Image")]
    public class ListImage : Program
    {
        [Description("The image name pattern")]
        [Required]
        [MinLength(1)]
        public string Name { get; set; } = string.Empty;

        [Description("The image type Id")]
        [Range(1, 9)]
        public int TypeId { get; set; } = 1;

        public override Task<int> ExecuteAsync()
        {
            Console.WriteLine("Simulate {0} {1}", nameof(ListImage), this.Name);
            return Task.FromResult(0);
        }
    }
}
```

Mention that:
- The **Program** itself is an abstract with just an entrypoint and the **ExecuteAsync** contract
- Commands are classes deriving from **Program** containg the methods you desire (ExecuteAsync in the example)
- It is derived in a **ListImage** class that is instantiated
- Help is generated using text from **DesriptionAttribute**
- I choose to output the feedback without help in case of success which shows the program title

Executing:

```cli
CliArgs.exe List Image Name=abc
or
CliArgs.exe L I N=abc
```
gives:
```cli
CliArgs Cli arguments interpreter example
Simulate ListImage abc
```

## Help

Executing the program without parameter will output this with the accepted abbreviations in parentheses:

```cli
CliArgs Cli arguments interpreter example
*** : Usage: verb (noun) (parameters)
  List     Image            List the images (L I)
    TypeId                  The image type Id (T)
    Name                    The image name pattern (N)
```

## Validation using **DataAnnotations** attributes or your custom attributes

Executing it with:

```cli
CliArgs.exe List Image Name= Type=10
or
CliArgs.exe L I N= T=10
```

gives:

```cli
CliArgs Cli arguments interpreter example
*** TypeId: The field TypeId must be between 1 and 9.
*** Name: The Name field is required.
  List     Image            List the images (L I)
    TypeId                  The image type Id (T)
    Name                    The image name pattern (N)
```

## New ListImport command without attribute

When adding a new command **List Import** to the same program:

```c#
    public class ListImportWithoutParameter : Program
    {
        public override Task<int> ExecuteAsync()
        {
            Console.WriteLine("Simulate {0}", nameof(ListImportWithoutParameter));
            return Task.FromResult(0);
        }
    }
```

Mention that:
- The class does not require any attribute and VerbNoun is extracted using the first two words of the class name Pascal-casing
- Acronyms such as XML or HTTP will be split into individual letters unless explicitly configured using attributes,
e.g. a **ListXMLFile** will be interpreted as **List X** by convention.
In such a case, use a **DataContract** attribute to clarify your intent.
- The abbreviations now become **l ima** for **List Image** and **l imp** for **List Import**
as those are the minimum required to prevent ambiguity.

> :warning: Please note that abbreviations **should never** be used is scripts or documentation for many reasons
(clarity, newbie-friendly), including the fact that they may change by adding new commands.

## Verb-only command

You may define a verb-only command by creating a one-word class like **Cleanup**,
or by specifying a **Namespace** in a **DataContract** without **Name**:

```c#
    public class Cleanup : Program
    {
        public override Task<int> ExecuteAsync()
        {
            Console.WriteLine("Simulate {0}", nameof(Cleanup));
            return Task.FromResult(0);
        }
    }
```

## Type converter and custom validation

Custom type converter and custom validation may be used:

```c#
    public class SomeCommand : Program
    {
        [TypeConverter(typeof(YourTypeConverter))]
        [YourCustomValidation]
        [Required]
        public YourType Reference { get; set; }

        public override Task<int> ExecuteAsync()
        {
            Console.WriteLine("Simulate {0} {1}", nameof(SomeCommand), this.Reference);
            return Task.FromResult(0);
        }
    }
```

## Factory constructor

You may provide a factory delegate to integrate with your preferred DI framework:

```c#
var interpreter = new CommandLineInterpreter(
    factory: type => MyContainer.Resolve(type));
```

If no factory delegate is provided, Activator is used:

```c#
... = Activator.CreateInstance(type);
```

## Examples

To view examples, see the tests models: by convention, by annotation and with type converter.

## Thanks

Thanks to Dan (aka ChatGPT) for they advices and making doubts disappear.
