using System;
using System.Linq;
using Xunit;
using Ragnar.Natives;

namespace Ragnar.Tests;

public class GuiTests : TestBase
{
    [Fact]
    public void Test_Parse_Simple_Widgets()
    {
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        var code = @"
            [
                title ""My App""
                heading ""System Status""
                lbl: text ""Status ok""
            ]
        ";

        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var layoutBlock = (Block)new Loader().Load(tokens).Children.First();

        var root = GuiFunctions.ParseLayout(layoutBlock, ctx, interpreter);

        // Verify root container (default column)
        Assert.Equal("column", root.Type);
        Assert.Equal("My App", root.Text);

        // Verify children
        Assert.Equal(2, root.Children.Count);

        var heading = root.Children[0];
        Assert.Equal("heading", heading.Type);
        Assert.Equal("System Status", heading.Text);

        var text = root.Children[1];
        Assert.Equal("text", text.Type);
        Assert.Equal("Status ok", text.Text);
        Assert.Equal("lbl", text.Id);

        // Verify the variable 'lbl' is bound in the context to the widget
        Assert.True(ctx.TryGet("lbl", out var val));
        Assert.Same(text, val);
    }

    [Fact]
    public void Test_Parse_Nesting_And_Button()
    {
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        var code = @"
            [
                row [
                    btn: button ""Click"" [ print ""Clicked"" ]
                ]
            ]
        ";

        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var layoutBlock = (Block)new Loader().Load(tokens).Children.First();

        var root = GuiFunctions.ParseLayout(layoutBlock, ctx, interpreter);

        Assert.Single(root.Children);
        var row = root.Children[0];
        Assert.Equal("row", row.Type);

        Assert.Single(row.Children);
        var button = row.Children[0];
        Assert.Equal("button", button.Type);
        Assert.Equal("Click", button.Text);
        Assert.Equal("btn", button.Id);
        Assert.NotNull(button.Action);
    }

    [Fact]
    public void Test_Parse_Paren_Evaluation()
    {
        var ctx = Runtime.CreateGlobalContext();
        ctx.Set("num", new Integer(42));
        var interpreter = new Interpreter();

        // Load Mezzanine (needed for rejoin)
        var mezzTokens = new Lexer(Mezzanine.SOURCE).Tokenize();
        var mezzRoot = new Loader().Load(mezzTokens);
        interpreter.Evaluate(mezzRoot, ctx);

        var code = @"
            [
                text (rejoin [""The number is "" num])
            ]
        ";

        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var layoutBlock = (Block)new Loader().Load(tokens).Children.First();

        var root = GuiFunctions.ParseLayout(layoutBlock, ctx, interpreter);

        Assert.Single(root.Children);
        var text = root.Children[0];
        Assert.Equal("text", text.Type);
        Assert.Equal("The number is 42", text.Text);
    }

    [Fact]
    public void Test_Html_Generation()
    {
        var widgetText = new GuiWidget("txt1", "text", "Glow text", new Word("none"));
        var htmlText = GuiFunctions.RenderWidgetHtml(widgetText);
        Assert.Contains("class=\"retro-text\"", htmlText);
        Assert.Contains("id=\"txt1\"", htmlText);

        var widgetBtn = new GuiWidget("btn1", "button", "Click me", new Word("none"));
        var htmlBtn = GuiFunctions.RenderWidgetHtml(widgetBtn);
        Assert.Contains("class=\"retro-btn\"", htmlBtn);
        Assert.Contains("onclick=\"triggerClick('btn1')\"", htmlBtn);

        var widgetField = new GuiWidget("f1", "field", "", new Text("init text"));
        var htmlField = GuiFunctions.RenderWidgetHtml(widgetField);
        Assert.Contains("class=\"retro-field\"", htmlField);
        Assert.Contains("value=\"init text\"", htmlField);
    }

    [Fact]
    public void Test_SetFace_And_GetFace()
    {
        var ctx = Runtime.CreateGlobalContext();
        var widget = new GuiWidget("lbl", "text", "Initial", new Text("Initial"));
        ctx.Set("lbl", widget);

        var interpreter = new Interpreter();
        
        var code = @"
            set-face lbl ""Updated""
            get-face lbl
        ";
        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var root = new Loader().Load(tokens);
        var result = interpreter.Evaluate(root, ctx);

        Assert.Equal("Updated", result.ToUserString());
        Assert.Equal("Updated", widget.Text);
        Assert.Equal("Updated", widget.CurrentValue.ToUserString());
    }
}
