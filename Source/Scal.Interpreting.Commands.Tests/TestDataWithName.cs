using System;

namespace Scal.Interpreting.Commands.Tests;

public record TestDataWithName(int Id, string? Error, Type? ExpectedType, string? Name, int TypeId, params string[] Args)
    : TestData(Id, Error, ExpectedType, Args)
{
    public override string ToString()
    {
        return $"#{this.Id:d3} Expected {this.Error ?? "Success"}";
    }
};

