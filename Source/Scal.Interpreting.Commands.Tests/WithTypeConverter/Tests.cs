using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Scal.Interpreting.Commands.Tests.WithTypeConverter;

public class Tests(ITestOutputHelper output)
{
    private ITestOutputHelper _output = output;

    static Reference ExpectedReference = new Reference("abc", "def");

    public static IEnumerable<object[]> GetTestData() => [
        [new TestData(  1, null, typeof(AddReference),                   "Add", "Reference", "Ref=abc-def")],
        [new TestData(  2, "Ref field is required",              null,   "Add", "Reference")],
        [new TestData(  3, null, typeof(AddReference),                   "A",    "R",        "-R=abc-def")],
        [new TestData(  4, "Reference not well formed",          null,   "Add", "Reference", "Ref=abc-def-ghi")],
    ];


    [Theory]
    [MemberData(nameof(GetTestData))]
    public void ShouldInterpretOrReject(TestData testData)
    {
        var interpreter     = new CommandLineInterpreter();
        var interpretation  = interpreter.Interpret<Program>(testData.Args);
        this._output.WriteLine(string.Join(' ', testData.Args));
        interpretation.Feedback(this._output.WriteLine);
        if (testData.ExpectedType is null) {
            Assert.Null(interpretation.Command);
            Assert.NotEmpty(interpretation.Results);
        } else {
            Assert.IsType(testData.ExpectedType, interpretation.Command);
            Assert.Empty(interpretation.Results);
            if (interpretation.Command is AddReference addReference) {
                Assert.Equal(ExpectedReference.Scope, addReference.Ref?.Scope);
                Assert.Equal(ExpectedReference.ContractId, addReference.Ref?.ContractId);
            }
        }
        List<string> feedback = [];
        interpretation.Feedback(feedback.Add);
        Assert.NotEmpty(feedback);
        if (testData.Error is not null) {
            Assert.Contains(feedback, msg => msg.Contains(testData.Error, StringComparison.OrdinalIgnoreCase));
        }
    }

}
