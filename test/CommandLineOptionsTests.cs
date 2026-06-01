using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class CommandLineOptionsTests
{
    [Fact]
    public void Parse_EmptyArgs_ReturnsDefaultOptions()
    {
        var options = CommandLineOptions.Parse(Array.Empty<string>());

        Assert.False(options.ShowHelp);
        Assert.False(options.ShowVersion);
        Assert.False(options.NoBanner);
        Assert.False(options.NoConfig);
        Assert.False(options.NoRepl);
        Assert.Empty(options.Targets);
        Assert.Empty(options.Errors);
    }

    [Fact]
    public void Parse_HelpFlags_SetsShowHelp()
    {
        var options1 = CommandLineOptions.Parse(new[] { "-h" });
        var options2 = CommandLineOptions.Parse(new[] { "--help" });

        Assert.True(options1.ShowHelp);
        Assert.True(options2.ShowHelp);
    }

    [Fact]
    public void Parse_VersionFlags_SetsShowVersion()
    {
        var options1 = CommandLineOptions.Parse(new[] { "-v" });
        var options2 = CommandLineOptions.Parse(new[] { "--version" });

        Assert.True(options1.ShowVersion);
        Assert.True(options2.ShowVersion);
    }

    [Fact]
    public void Parse_NoReplNoBannerNoConfig_SetsFlags()
    {
        var options = CommandLineOptions.Parse(new[] { "--no-repl", "--no-banner", "--no-config" });

        Assert.True(options.NoRepl);
        Assert.True(options.NoBanner);
        Assert.True(options.NoConfig);
        Assert.Empty(options.Targets);
        Assert.Empty(options.Errors);
    }

    [Fact]
    public void Parse_FileAndEval_PreservesOrderAndValues()
    {
        var options = CommandLineOptions.Parse(new[] {
            "-f", "script1.r",
            "-e", "print 1",
            "--file", "script2.r",
            "--eval", "print 2"
        });

        Assert.Empty(options.Errors);
        Assert.Equal(4, options.Targets.Count);

        Assert.Equal(EvaluationType.File, options.Targets[0].Type);
        Assert.Equal("script1.r", options.Targets[0].Value);

        Assert.Equal(EvaluationType.Expression, options.Targets[1].Type);
        Assert.Equal("print 1", options.Targets[1].Value);

        Assert.Equal(EvaluationType.File, options.Targets[2].Type);
        Assert.Equal("script2.r", options.Targets[2].Value);

        Assert.Equal(EvaluationType.Expression, options.Targets[3].Type);
        Assert.Equal("print 2", options.Targets[3].Value);
    }

    [Fact]
    public void Parse_MissingValueForFile_AddsError()
    {
        var options = CommandLineOptions.Parse(new[] { "-f" });

        Assert.Single(options.Errors);
        Assert.Contains("Missing value for option -f/--file", options.Errors[0]);
    }

    [Fact]
    public void Parse_MissingValueForEval_AddsError()
    {
        var options = CommandLineOptions.Parse(new[] { "--eval" });

        Assert.Single(options.Errors);
        Assert.Contains("Missing value for option -e/--eval", options.Errors[0]);
    }

    [Fact]
    public void Parse_UnrecognizedArgument_AddsError()
    {
        var options = CommandLineOptions.Parse(new[] { "--invalid" });

        Assert.Single(options.Errors);
        Assert.Contains("Unrecognized argument: --invalid", options.Errors[0]);
    }

    [Fact]
    public void Parse_ScriptArguments_SetsScriptMode()
    {
        var options = CommandLineOptions.Parse(new[] { "script.r", "arg1", "--flag" });

        Assert.Empty(options.Errors);
        Assert.True(options.ScriptMode);
        Assert.Equal("script.r", options.ScriptPath);
        Assert.True(options.NoRepl);
        Assert.True(options.NoBanner);
        Assert.Equal(2, options.ScriptArgs.Count);
        Assert.Equal("arg1", options.ScriptArgs[0]);
        Assert.Equal("--flag", options.ScriptArgs[1]);
    }

    [Fact]
    public void Parse_ScriptSeparator_SetsScriptMode()
    {
        var options = CommandLineOptions.Parse(new[] { "--", "script.r", "arg1" });

        Assert.Empty(options.Errors);
        Assert.True(options.ScriptMode);
        Assert.Equal("script.r", options.ScriptPath);
        Assert.True(options.NoRepl);
        Assert.True(options.NoBanner);
        Assert.Single(options.ScriptArgs);
        Assert.Equal("arg1", options.ScriptArgs[0]);
    }
}
