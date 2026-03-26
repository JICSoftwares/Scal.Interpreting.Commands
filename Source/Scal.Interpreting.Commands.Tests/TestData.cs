using System;

namespace Scal.Interpreting.Commands.Tests;

public record TestData(int Id, string? Error, Type? ExpectedType, params string[] Args)
{
    public override string ToString()
    {
        return $"#{this.Id:d3} Expected {this.Error ?? "Success"}";
    }
};
