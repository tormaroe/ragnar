using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class PrettyTableTests : TestBase
{
    [Fact]
    public void PrettyTable_BlockOfBlocks_FormatsCorrectly()
    {
        var code = @"
            make-table: do %lib/prettytable.r
            data: [
                [""Name"" ""Age"" ""City""]
                [""Alice"" 30 ""New York""]
                [""Bob"" 25 ""Los Angeles""]
                [""Charlie"" 35 ""Chicago""]
            ]
            make-table data
        ";

        var (result, _) = Run(code);
        var textResult = Assert.IsType<Text>(result);

        var expected = "+---------+-----+-------------+\n" +
                       "| Name    | Age | City        |\n" +
                       "| Alice   |  30 | New York    |\n" +
                       "| Bob     |  25 | Los Angeles |\n" +
                       "| Charlie |  35 | Chicago     |\n" +
                       "+---------+-----+-------------+\n";

        Assert.Equal(expected.Replace("\r\n", "\n"), textResult.Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void PrettyTable_BlockOfRecords_FormatsCorrectly()
    {
        var code = @"
            make-table: do %lib/prettytable.r
            data: funcmap :to-record [
                [""Name"" ""Alice"" ""Age"" 30 ""City"" ""New York""]
                [""Name"" ""Bob"" ""Age"" 25 ""City"" ""Los Angeles""]
                [""Name"" ""Charlie"" ""Age"" 35 ""City"" ""Chicago""]
            ]
            make-table data
        ";

        var (result, _) = Run(code);
        var textResult = Assert.IsType<Text>(result);

        var expected = "+---------+-----+-------------+\n" +
                       "| Name    | Age | City        |\n" +
                       "+---------+-----+-------------+\n" +
                       "| Alice   |  30 | New York    |\n" +
                       "| Bob     |  25 | Los Angeles |\n" +
                       "| Charlie |  35 | Chicago     |\n" +
                       "+---------+-----+-------------+\n";

        Assert.Equal(expected.Replace("\r\n", "\n"), textResult.Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void PrettyTable_EmptyData_ReturnsEmptyString()
    {
        var code = @"
            make-table: do %lib/prettytable.r
            make-table []
        ";

        var (result, _) = Run(code);
        var textResult = Assert.IsType<Text>(result);
        Assert.Equal("", textResult.Content);
    }
}
