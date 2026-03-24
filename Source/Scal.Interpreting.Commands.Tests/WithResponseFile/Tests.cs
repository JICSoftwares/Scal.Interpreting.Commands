using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Scal.Interpreting.Commands.Tests.WithResponseFile;

public class Tests(ITestOutputHelper output)
{
    private ITestOutputHelper _output = output;

    public static IEnumerable<object[]> GetTestData() => [
        [new TestDataWithName("TypeId must be between 1 and 9",     null, null, 0,  "@ParametersListImageKOMultiLines.rsp")],
        [new TestDataWithName("TypeId must be between 1 and 9",     null, null, 0,  "@ParametersListImageKOOneLine.rsp")],
        [new TestDataWithName(null, typeof(ListImageByType),        "a b c", 1,     "@ParametersListImageOKMultiLines.rsp")],
        [new TestDataWithName(null, typeof(ListImageByType),        "a b c", 1,     "@ParametersListImageOKOneLine.rsp")],
        [new TestDataWithName(null, typeof(ListImageByType),        "a b c", 1,     "@ParametersListImageOKMultiFiles.rsp")]
    ];


    [Theory]
    [MemberData(nameof(GetTestData))]
    public void ShouldInterpretOrReject(TestDataWithName testData)
    {
        var interpreter     = new CommandLineInterpreter(
            responseFilePath: this.GetResponseFilePath()
        );
        var interpretation  = interpreter.Interpret<Program>(testData.Args);
        this._output.WriteLine(string.Join(' ', testData.Args));
        interpretation.Feedback(this._output.WriteLine);
        if (testData.ExpectedType is null) {
            Assert.Null(interpretation.Command);
            Assert.NotEmpty(interpretation.Results);
        } else {
            Assert.IsType(testData.ExpectedType, interpretation.Command);
            Assert.Empty(interpretation.Results);
        }
        List<string> feedback = [];
        interpretation.Feedback(feedback.Add);
        Assert.NotEmpty(feedback);
        if (testData.Error is not null) {
            Assert.Contains(feedback, msg => msg.Contains(testData.Error, StringComparison.OrdinalIgnoreCase));
        }
        if (interpretation.Command is ListImageByType listImageByType) {
            if (! string.IsNullOrWhiteSpace(testData.Name)) {
                Assert.Equal(testData.Name, listImageByType.Name);
            }
            if (testData.TypeId > 0) {
                Assert.Equal(testData.TypeId, listImageByType.TypeId);
            }
        }
    }

    private string GetResponseFilePath([CallerFilePath]string filePath = "") {
        return Path.GetDirectoryName(filePath)!;
    }

}
