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
        Assert.Contains("class=\"gui-text\"", htmlText);
        Assert.Contains("id=\"txt1\"", htmlText);

        var widgetBtn = new GuiWidget("btn1", "button", "Click me", new Word("none"));
        var htmlBtn = GuiFunctions.RenderWidgetHtml(widgetBtn);
        Assert.Contains("class=\"gui-btn\"", htmlBtn);
        Assert.Contains("onclick=\"triggerClick('btn1')\"", htmlBtn);

        var widgetField = new GuiWidget("f1", "field", "", new Text("init text"));
        var htmlField = GuiFunctions.RenderWidgetHtml(widgetField);
        Assert.Contains("class=\"gui-field\"", htmlField);
        Assert.Contains("value=\"init text\"", htmlField);
    }

    [Fact]
    public void Test_SetTheme()
    {
        var (result1, _) = Run("set-theme 'classic-rebol");
        Assert.Equal("classic-rebol", result1.ToUserString());

        var (result2, _) = Run("set-theme \"retro-terminal\"");
        Assert.Equal("retro-terminal", result2.ToUserString());

        var (result3, _) = Run("set-theme 'modern-slate");
        Assert.Equal("modern-slate", result3.ToUserString());

        var (result4, _) = Run("set-theme 'kawaii-blossom");
        Assert.Equal("kawaii-blossom", result4.ToUserString());

        Assert.ThrowsAny<Exception>(() => Run("set-theme 'invalid-theme"));
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

    [Fact]
    public void Test_Choice_Parsing_And_Rendering()
    {
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        var code = @"
            [
                sel: choice [""Option A"" ""Option B"" ""Option C""] [ print ""Changed"" ]
            ]
        ";

        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var layoutBlock = (Block)new Loader().Load(tokens).Children.First();

        var root = GuiFunctions.ParseLayout(layoutBlock, ctx, interpreter);

        Assert.Single(root.Children);
        var choice = root.Children[0];
        Assert.Equal("choice", choice.Type);
        Assert.Equal("Option A", choice.CurrentValue.ToUserString());
        Assert.Equal(3, choice.Options.Count);
        Assert.Equal("Option A", choice.Options[0]);
        Assert.Equal("Option B", choice.Options[1]);
        Assert.Equal("Option C", choice.Options[2]);
        Assert.NotNull(choice.Action);

        // Check HTML generation
        var html = GuiFunctions.RenderWidgetHtml(choice);
        Assert.Contains("<select id=\"sel\" class=\"gui-choice\"", html);
        Assert.Contains("<option value=\"Option A\" selected=\"selected\">Option A</option>", html);
        Assert.Contains("<option value=\"Option B\">Option B</option>", html);
    }

    [Fact]
    public void Test_Image_Encoding()
    {
        var tempFile = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "temp_test_image.png");
        try
        {
            System.IO.File.WriteAllBytes(tempFile, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }); // dummy png header
            var widget = new GuiWidget("img1", "image", tempFile, new Word("none"));

            var html = GuiFunctions.RenderWidgetHtml(widget);
            Assert.Contains("data:image/png;base64,iVBORw0KGgo=", html);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public void Test_Image_Sizing()
    {
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        var code = @"
            [
                image ""logo.png"" 150 50
                image ""logo2.png"" 120x60
            ]
        ";

        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var layoutBlock = (Block)new Loader().Load(tokens).Children.First();

        var root = GuiFunctions.ParseLayout(layoutBlock, ctx, interpreter);

        Assert.Equal(2, root.Children.Count);

        var img1 = root.Children[0];
        Assert.Equal("image", img1.Type);
        Assert.Equal("logo.png", img1.Text);
        Assert.Equal("150", img1.Width);
        Assert.Equal("50", img1.Height);

        var img2 = root.Children[1];
        Assert.Equal("image", img2.Type);
        Assert.Equal("logo2.png", img2.Text);
        Assert.Equal("120", img2.Width);
        Assert.Equal("60", img2.Height);

        var html1 = GuiFunctions.RenderWidgetHtml(img1);
        Assert.Contains("width=\"150\"", html1);
        Assert.Contains("height=\"50\"", html1);

        var html2 = GuiFunctions.RenderWidgetHtml(img2);
        Assert.Contains("width=\"120\"", html2);
        Assert.Contains("height=\"60\"", html2);
    }

    [Fact]
    public void Test_Json_Parsing_Values()
    {
        var json = "{\"values\":{\"check-override\":true,\"cmd-field\":\"ACTIVATE\"}}";
        var dict = GuiFunctions.ParseJsonValues(json);

        Assert.True(dict.TryGetValue("check-override", out var overrideVal));
        Assert.Equal("true", overrideVal);

        Assert.True(dict.TryGetValue("cmd-field", out var cmdVal));
        Assert.Equal("ACTIVATE", cmdVal);
    }
}
