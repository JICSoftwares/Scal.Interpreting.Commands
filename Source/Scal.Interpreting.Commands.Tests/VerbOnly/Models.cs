using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Scal.Interpreting.Commands.Tests.VerbOnly;

internal abstract class Program
{
    internal abstract void Execute();
}

[Description("Do something with what")]
internal class Do : Program
{
    [Description("A parameter")]
    [Required]
    public string? What { get; set; }

    internal override void Execute()
    {
        Console.WriteLine("{0}: '{1}'", this.GetType().Name, this.What);
    }
}

[Description("Do something more with what")]
internal class DoMore : Program
{
    [Description("A parameter")]
    [Required]
    public string? What { get; set; }

    internal override void Execute()
    {
        Console.WriteLine("{0}: '{1}'", this.GetType().Name, this.What);
    }
}
