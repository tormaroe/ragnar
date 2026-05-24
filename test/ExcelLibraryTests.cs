using Xunit;
using Ragnar;
using System;
using System.IO;

namespace Ragnar.Tests;

public class ExcelLibraryTests : TestBase
{
    [Fact]
    public void CoerceType_EnumResolution_Works()
    {
        // XLDataType is a real enum in ClosedXML
        var enumType = typeof(ClosedXML.Excel.XLDataType);

        // Test with string
        var res1 = Interop.CoerceType("Text", enumType);
        Assert.IsType<ClosedXML.Excel.XLDataType>(res1);
        Assert.Equal(ClosedXML.Excel.XLDataType.Text, res1);

        // Test with Ragnar Word
        var word = new Word("Number");
        var res2 = Interop.CoerceType(word, enumType);
        Assert.IsType<ClosedXML.Excel.XLDataType>(res2);
        Assert.Equal(ClosedXML.Excel.XLDataType.Number, res2);

        // Test with Ragnar LitWord
        var litWord = new LitWord("Boolean");
        var res3 = Interop.CoerceType(litWord, enumType);
        Assert.IsType<ClosedXML.Excel.XLDataType>(res3);
        Assert.Equal(ClosedXML.Excel.XLDataType.Boolean, res3);
    }

    [Fact]
    public void CoerceType_StaticPropertyResolution_Works()
    {
        // XLTableTheme is a class with static properties in ClosedXML
        var themeType = typeof(ClosedXML.Excel.XLTableTheme);

        // Test with string
        var res1 = Interop.CoerceType("TableStyleMedium2", themeType);
        Assert.IsType<ClosedXML.Excel.XLTableTheme>(res1);
        Assert.Equal(ClosedXML.Excel.XLTableTheme.TableStyleMedium2, res1);

        // Test with Ragnar Word
        var word = new Word("TableStyleMedium5");
        var res2 = Interop.CoerceType(word, themeType);
        Assert.IsType<ClosedXML.Excel.XLTableTheme>(res2);
        Assert.Equal(ClosedXML.Excel.XLTableTheme.TableStyleMedium5, res2);

        // Test with Ragnar LitWord
        var litWord = new LitWord("TableStyleDark1");
        var res3 = Interop.CoerceType(litWord, themeType);
        Assert.IsType<ClosedXML.Excel.XLTableTheme>(res3);
        Assert.Equal(ClosedXML.Excel.XLTableTheme.TableStyleDark1, res3);
    }

    [Fact]
    public void CoerceType_ImplicitConversion_Works()
    {
        // XLCellValue uses implicit operators from standard types
        var targetType = typeof(ClosedXML.Excel.XLCellValue);

        // Implicit operator from string
        var val1 = Interop.CoerceType("hello", targetType);
        Assert.NotNull(val1);
        Assert.IsType<ClosedXML.Excel.XLCellValue>(val1);
        Assert.Equal("hello", ((ClosedXML.Excel.XLCellValue)val1).GetText());

        // Implicit operator from double
        var val2 = Interop.CoerceType(123.45, targetType);
        Assert.NotNull(val2);
        Assert.IsType<ClosedXML.Excel.XLCellValue>(val2);
        Assert.Equal(123.45, ((ClosedXML.Excel.XLCellValue)val2).GetNumber());

        // Implicit operator from bool
        var val3 = Interop.CoerceType(true, targetType);
        Assert.NotNull(val3);
        Assert.IsType<ClosedXML.Excel.XLCellValue>(val3);
        Assert.True(((ClosedXML.Excel.XLCellValue)val3).GetBoolean());
    }

    [Fact]
    public void ExcelLibrary_BasicWorkbookOperations_Works()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        try
        {
            var code = $@"
                excel: do %lib/excel.r
                wb: excel/new
                ws: wb/worksheets/add ""Sheet1""
                
                c1: ws/cell ""A1""
                c1/set-value ""Hello Excel""
                
                c2: ws/cell ""B1""
                c2/set-value 42
                
                c3: ws/cell ""C1""
                c3/set-value true

                wb/save-as ""{tempFile.Replace("\\", "/")}""
                
                ; Load it back
                wb2: excel/load ""{tempFile.Replace("\\", "/")}""
                ws2: wb2/worksheets/get ""Sheet1""
                
                cell-a1: ws2/cell ""A1""
                val1: cell-a1/get-value
                
                cell-b1: ws2/cell ""B1""
                val2: cell-b1/get-value
                
                cell-c1: ws2/cell ""C1""
                val3: cell-c1/get-value

                reduce [val1 val2 val3]
            ";

            var (result, _) = Run(code);
            var b = Assert.IsType<Block>(result);
            Assert.Equal(3, b.Children.Count);
            Assert.Equal("Hello Excel", ((Text)b.Children[0]).Content);
            Assert.Equal(42.0, ((Decimal)b.Children[1]).Number);
            Assert.True(((Logic)b.Children[2]).Condition);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                System.IO.File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ExcelLibrary_BulkDataInsertAndGetData_Works()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test_bulk_{Guid.NewGuid():N}.xlsx");
        try
        {
            var code = $@"
                excel: do %lib/excel.r
                wb: excel/new
                ws: wb/worksheets/add ""DataSheet""
                
                cell: ws/cell ""A1""
                
                ; Insert table data and style it
                tbl: cell/insert-data reduce [
                    [""Name"" ""Age"" ""Active""]
                    reduce [""Alice"" 30 true]
                    reduce [""Bob"" 25 false]
                ]
                
                tbl/set-theme 'TableStyleMedium9

                wb/save-as ""{tempFile.Replace("\\", "/")}""
                
                ; Load it back and get data range
                wb2: excel/load ""{tempFile.Replace("\\", "/")}""
                ws2: wb2/worksheets/get ""DataSheet""
                
                ws2/get-data ""A1:C3""
            ";

            var (result, _) = Run(code);
            var b = Assert.IsType<Block>(result);
            Assert.Equal(3, b.Children.Count);

            var row1 = Assert.IsType<Block>(b.Children[0]);
            var row2 = Assert.IsType<Block>(b.Children[1]);
            var row3 = Assert.IsType<Block>(b.Children[2]);

            Assert.Equal("Name", ((Text)row1.Children[0]).Content);
            Assert.Equal("Age", ((Text)row1.Children[1]).Content);
            Assert.Equal("Active", ((Text)row1.Children[2]).Content);

            Assert.Equal("Alice", ((Text)row2.Children[0]).Content);
            Assert.Equal(30.0, ((Decimal)row2.Children[1]).Number);
            Assert.True(((Logic)row2.Children[2]).Condition);

            Assert.Equal("Bob", ((Text)row3.Children[0]).Content);
            Assert.Equal(25.0, ((Decimal)row3.Children[1]).Number);
            Assert.False(((Logic)row3.Children[2]).Condition);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                System.IO.File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Excel_DoesNotPolluteGlobalScope_Works()
    {
        var code = @"
            excel: do %lib/excel.r
            reduce [
                attempt [get 'get-cell-val]
                attempt [get 'wrap-table]
                attempt [get 'wrap-range]
                attempt [get 'wrap-cell]
                attempt [get 'wrap-worksheet]
                attempt [get 'wrap-workbook]
                attempt [get 'net-new]
            ]
        ";
        var result = (Block)Run(code).Result;
        foreach (var item in result.Children)
        {
            Assert.Equal("none", item.ToUserString());
        }
    }
}
