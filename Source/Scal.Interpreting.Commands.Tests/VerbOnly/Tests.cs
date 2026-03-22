using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Scal.Interpreting.Commands.Tests.VerbOnly;

public class Tests(ITestOutputHelper output)
{
    private ITestOutputHelper _output = output;

    [Fact]
    public void ShouldInterpretVerbOnlyWithParameter()
    {
        var what            = "Something";
        var interpreter     = new CommandLineInterpreter();
        var interpretation  = interpreter.Interpret<Program>([ "Do", "What=" + what ]);
        interpretation.Feedback(this._output.WriteLine);
        Assert.Empty(interpretation.Results);
        var doCommand = Assert.IsType<Do>(interpretation.Command);
        Assert.Equal(what, doCommand.What);
    }

    [Fact]
    public void ShouldDistinguishVerbNounFromVerbOnly()
    {
        var what            = "Something";
        var interpreter     = new CommandLineInterpreter();
        var interpretation  = interpreter.Interpret<Program>([ "Do", "More", "What=" + what ]);
        interpretation.Feedback(this._output.WriteLine);
        Assert.Empty(interpretation.Results);
        var doMoreCommand = Assert.IsType<DoMore>(interpretation.Command);
        Assert.Equal(what, doMoreCommand.What);
    }

}
