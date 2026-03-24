using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Scal.Interpreting.Commands.Tests.WithResponseFile;

internal abstract class Program
{
}

internal class ListImageByType : Program
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Range(1, 9)]
    public int TypeId { get; set; }
}
