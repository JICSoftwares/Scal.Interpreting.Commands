using System;

namespace Scal.Interpreting.Commands.Tests;

public record TestDataWithName(string? Error, Type? ExpectedType, string? Name, int TypeId, params string[] Args);
