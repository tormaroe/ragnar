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

    [Fact]
    public void Test_Textarea_And_Spinner()
    {
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        var code = @"
            [
                ta1: textarea ""default rows""
                ta2: textarea 8 ""custom rows""
                ta3: textarea ""custom rows after"" 12
                spin1: spinner false
                spin2: spinner
            ]
        ";

        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var layoutBlock = (Block)new Loader().Load(tokens).Children.First();

        var root = GuiFunctions.ParseLayout(layoutBlock, ctx, interpreter);

        Assert.Equal(5, root.Children.Count);

        var ta1 = root.Children[0];
        Assert.Equal("textarea", ta1.Type);
        Assert.Equal("default rows", ta1.Text);
        Assert.Null(ta1.Rows);

        var ta2 = root.Children[1];
        Assert.Equal("textarea", ta2.Type);
        Assert.Equal("custom rows", ta2.Text);
        Assert.Equal(8, ta2.Rows);

        var ta3 = root.Children[2];
        Assert.Equal("textarea", ta3.Type);
        Assert.Equal("custom rows after", ta3.Text);
        Assert.Equal(12, ta3.Rows);

        var spin1 = root.Children[3];
        Assert.Equal("spinner", spin1.Type);
        Assert.False(((Logic)spin1.CurrentValue).Condition);

        var spin2 = root.Children[4];
        Assert.Equal("spinner", spin2.Type);
        Assert.True(((Logic)spin2.CurrentValue).Condition);

        // Check HTML Generation
        var htmlTa2 = GuiFunctions.RenderWidgetHtml(ta2);
        Assert.Contains("<textarea id=\"ta2\" class=\"gui-textarea\" rows=\"8\"", htmlTa2);
        Assert.Contains("custom rows</textarea>", htmlTa2);

        var htmlSpin1 = GuiFunctions.RenderWidgetHtml(spin1);
        Assert.Contains("<div id=\"spin1\" class=\"gui-spinner\" style=\"display: none;\"></div>", htmlSpin1);

        var htmlSpin2 = GuiFunctions.RenderWidgetHtml(spin2);
        Assert.Contains("<div id=\"spin2\" class=\"gui-spinner\"></div>", htmlSpin2);
    }

    [Fact]
    public void Test_Nested_Widgets_Have_Unique_Ids()
    {
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        var code = @"
            [
                button ""1""
                row [
                    button ""2""
                    button ""3""
                ]
                button ""4""
            ]
        ";

        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var layoutBlock = (Block)new Loader().Load(tokens).Children.First();

        var root = GuiFunctions.ParseLayout(layoutBlock, ctx, interpreter);

        Assert.Equal(3, root.Children.Count);

        var btn1 = root.Children[0];
        var row = root.Children[1];
        var btn4 = root.Children[2];

        Assert.Equal("button_1", btn1.Id);
        Assert.Equal("row_2", row.Id);

        Assert.Equal(2, row.Children.Count);
        var btn2 = row.Children[0];
        var btn3 = row.Children[1];

        Assert.Equal("button_3", btn2.Id);
        Assert.Equal("button_4", btn3.Id);
        Assert.Equal("button_5", btn4.Id);

        // Verify that all IDs in the whole tree are unique
        var allIds = new HashSet<string>();
        void CollectIds(GuiWidget w)
        {
            if (w.Id != "root") allIds.Add(w.Id);
            foreach (var child in w.Children)
            {
                CollectIds(child);
            }
        }
        CollectIds(root);

        // There should be 5 unique ids: button_1, row_2, button_3, button_4, button_5
        Assert.Equal(5, allIds.Count);
        Assert.Contains("button_1", allIds);
        Assert.Contains("row_2", allIds);
        Assert.Contains("button_3", allIds);
        Assert.Contains("button_4", allIds);
        Assert.Contains("button_5", allIds);
    }
}
