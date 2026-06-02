using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ragnar.Natives;

public static class GuiFunctions
{
    private static HttpListener? _listener;
    private static ManualResetEvent? _exitEvent;
    private static StreamWriter? _sseWriter;
    private static readonly object _sseLock = new();
    private static int _connectionCount = 0;
    private static readonly object _connLock = new();
    private static Timer? _disconnectTimer;
    private static GuiWidget? _rootWidget;
    private static string _currentTheme = "retro-terminal";

    public static void Add(Context ctx)
    {
        // view [layout]
        ctx.Set("view", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not Block layoutBlock)
                throw new Exception("view expects a layout block.");

            // Parse layout block recursively
            _rootWidget = ParseLayout(layoutBlock, context, interpreter);

            // Find free port
            int port = GetFreePort();
            string url = $"http://localhost:{port}/";

            // Initialize exit event
            _exitEvent = new ManualResetEvent(false);
            _connectionCount = 0;
            _disconnectTimer = null;
            _sseWriter = null;

            // Start HttpListener
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();

            // Run listener loop in background task
            Task.Run(() => ListenLoop(context, interpreter));

            // Open browser
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ctx.Output.WriteLine($"Failed to open browser: {ex.Message}");
            }

            ctx.Output.WriteLine($"GUI Server running at {url}. Waiting for browser...");

            // Block script execution until window is closed
            _exitEvent.WaitOne();

            // Stop server and clean up
            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch {}

            lock (_sseLock)
            {
                _sseWriter?.Close();
                _sseWriter = null;
            }

            _disconnectTimer?.Dispose();

            ctx.Output.WriteLine("GUI Session ended.");
            return new Word("none");
        }, 1).WithTitle("Launches a Visual Dialect application in the default browser."));

        // set-face widget value
        ctx.Set("set-face", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not GuiWidget widget)
                throw new Exception("set-face expects a gui-widget.");

            Value val = args[1];
            widget.CurrentValue = val;

            // If it is a text-based or visual widget, also update its text
            if (widget.Type == "text" || widget.Type == "heading" || widget.Type == "image")
            {
                widget.Text = val.ToUserString();
            }

            string sseVal = val.ToUserString();
            if (widget.Type == "image")
            {
                sseVal = GetImageSource(sseVal);
            }

            // Push update to browser via SSE
            SendSseUpdate(widget.Id, sseVal);

            return val;
        }, 2).WithTitle("Updates a GUI widget's value and refreshes it in the browser."));

        // get-face widget
        ctx.Set("get-face", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not GuiWidget widget)
                throw new Exception("get-face expects a gui-widget.");

            return widget.CurrentValue;
        }, 1).WithTitle("Returns the current value of a GUI widget."));

        // set-theme theme
        ctx.Set("set-theme", new Native((args, refs, context, interpreter, isTail) =>
        {
            string themeName = args[0] switch
            {
                Word w => w.Name,
                LitWord lw => lw.Name,
                Text t => t.Content,
                _ => throw new Exception("set-theme expects a theme name (word or text).")
            };

            themeName = themeName.ToLower();
            if (themeName != "retro-terminal" && themeName != "classic-rebol" && themeName != "modern-slate" && themeName != "kawaii-blossom")
            {
                throw new Exception($"Unknown theme: '{themeName}'. Supported themes: 'retro-terminal', 'classic-rebol', 'modern-slate', 'kawaii-blossom'.");
            }

            _currentTheme = themeName;
            return new Word(themeName);
        }, 1).WithTitle("Sets the visual theme for view GUI applications."));
    }

    private static int GetFreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    internal static GuiWidget ParseLayout(Block layoutBlock, Context context, Interpreter interpreter)
    {
        var root = new GuiWidget("root", "column", "", new Word("none"));
        ParseBlock(layoutBlock.Children.Skip(layoutBlock.Index).ToList(), root, context, interpreter);
        return root;
    }

    private static void ParseBlock(List<Value> items, GuiWidget container, Context context, Interpreter interpreter)
    {
        int i = 0;
        int widgetCounter = 0;
        string? pendingName = null;

        while (i < items.Count)
        {
            var item = items[i];

            if (item is SetWord sw)
            {
                pendingName = sw.Name;
                i++;
                continue;
            }

            if (item is Word w)
            {
                string type = w.Name.ToLower();
                if (type == "title")
                {
                    i++;
                    if (i < items.Count)
                    {
                        var val = EvaluateOrGetLiteral(items[i], context, interpreter);
                        container.Text = val.ToUserString();
                    }
                    i++;
                }
                else if (type == "heading" || type == "text" || type == "field" || type == "button" || type == "check" || type == "slider" || type == "image" || type == "choice")
                {
                    i++;
                    string text = "";
                    Value initialValue = new Word("none");
                    Block? action = null;
                    List<string>? options = null;

                    if (i < items.Count && (items[i] is Text || items[i] is Paren))
                    {
                        var evaluated = EvaluateOrGetLiteral(items[i], context, interpreter);
                        text = evaluated.ToUserString();
                        if (type == "field") initialValue = evaluated;
                        i++;
                    }

                    string? imgWidth = null;
                    string? imgHeight = null;
                    if (type == "image")
                    {
                        if (i < items.Count && items[i] is Integer wInt)
                        {
                            imgWidth = wInt.Number.ToString();
                            i++;
                            if (i < items.Count && items[i] is Integer hInt)
                            {
                                imgHeight = hInt.Number.ToString();
                                i++;
                            }
                        }
                        else if (i < items.Count && (items[i] is Word || items[i] is Text))
                        {
                            string sizeStr = items[i].ToUserString();
                            var parts = sizeStr.Split('x');
                            if (parts.Length == 2)
                            {
                                imgWidth = parts[0];
                                imgHeight = parts[1];
                                i++;
                            }
                            else if (parts.Length == 1 && int.TryParse(parts[0], out _))
                            {
                                imgWidth = parts[0];
                                i++;
                            }
                        }
                    }

                    if (type == "check")
                    {
                        if (i < items.Count && (items[i] is Logic || items[i] is Paren))
                        {
                            var evaluated = EvaluateOrGetLiteral(items[i], context, interpreter);
                            initialValue = evaluated;
                            i++;
                        }
                        if (initialValue is Word) initialValue = new Logic(false);
                    }

                    if (type == "slider")
                    {
                        if (i < items.Count && (items[i] is Integer || items[i] is Paren))
                        {
                            var evaluated = EvaluateOrGetLiteral(items[i], context, interpreter);
                            initialValue = evaluated;
                            i++;
                        }
                        if (initialValue is Word) initialValue = new Integer(0);
                    }

                    if (type == "choice")
                    {
                        if (i < items.Count && items[i] is Block optionsBlock)
                        {
                            var optionsList = optionsBlock.Children.Skip(optionsBlock.Index).Select(v => v.ToUserString()).ToList();
                            options = optionsList;
                            initialValue = new Text(optionsList.FirstOrDefault() ?? "");
                            i++;
                        }
                        if (initialValue is Word) initialValue = new Text("");
                    }

                    if (type == "field" && initialValue is Word)
                    {
                        initialValue = new Text("");
                    }

                    if (i < items.Count && items[i] is Block actionBlock)
                    {
                        action = actionBlock;
                        i++;
                    }

                    string id = pendingName ?? $"{type}_{++widgetCounter}";
                    var widget = new GuiWidget(id, type, text, initialValue, action, options);
                    if (type == "image")
                    {
                        widget.Width = imgWidth;
                        widget.Height = imgHeight;
                    }
                    container.Children.Add(widget);

                    if (pendingName != null)
                    {
                        context.Set(pendingName, widget);
                        pendingName = null;
                    }
                }
                else if (type == "row" || type == "column")
                {
                    i++;
                    if (i < items.Count && items[i] is Block subBlock)
                    {
                        string id = pendingName ?? $"{type}_{++widgetCounter}";
                        var subContainer = new GuiWidget(id, type, "", new Word("none"));
                        ParseBlock(subBlock.Children.Skip(subBlock.Index).ToList(), subContainer, context, interpreter);
                        container.Children.Add(subContainer);

                        if (pendingName != null)
                        {
                            context.Set(pendingName, subContainer);
                            pendingName = null;
                        }
                        i++;
                    }
                    else
                    {
                        throw new Exception($"{type} expects a layout block.");
                    }
                }
                else
                {
                    i++;
                }
            }
            else
            {
                pendingName = null;
                i++;
            }
        }
    }

    private static Value EvaluateOrGetLiteral(Value val, Context context, Interpreter interpreter)
    {
        if (val is Paren p)
        {
            return interpreter.Evaluate(new Block(p.Children, p.Index), context);
        }
        return val;
    }

    private static void ListenLoop(Context context, Interpreter interpreter)
    {
        if (_listener == null) return;

        while (_listener.IsListening)
        {
            try
            {
                var contextTask = _listener.GetContextAsync();
                contextTask.Wait();
                var ctx = contextTask.Result;

                _ = Task.Run(() => HandleRequest(ctx, context, interpreter));
            }
            catch
            {
                break;
            }
        }
    }

    private static void HandleRequest(HttpListenerContext ctx, Context context, Interpreter interpreter)
    {
        var request = ctx.Request;
        var response = ctx.Response;

        try
        {
            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/")
            {
                string html = BuildHtml();
                byte[] buffer = Encoding.UTF8.GetBytes(html);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/events")
            {
                response.ContentType = "text/event-stream";
                response.Headers.Add("Cache-Control", "no-cache");
                response.Headers.Add("Connection", "keep-alive");
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                var writer = new StreamWriter(response.OutputStream);
                lock (_sseLock)
                {
                    if (_sseWriter != null)
                    {
                        try { _sseWriter.Close(); } catch {}
                    }
                    _sseWriter = writer;
                }

                lock (_connLock)
                {
                    _connectionCount++;
                    if (_disconnectTimer != null)
                    {
                        _disconnectTimer.Dispose();
                        _disconnectTimer = null;
                    }
                }

                // Keep SSE connection open with heartbeat
                while (_listener?.IsListening == true)
                {
                    Thread.Sleep(2000);
                    try
                    {
                        lock (_sseLock)
                        {
                            writer.Write(":\n\n");
                            writer.Flush();
                        }
                    }
                    catch
                    {
                        break;
                    }
                }

                lock (_connLock)
                {
                    _connectionCount--;
                    if (_connectionCount <= 0)
                    {
                        _disconnectTimer = new Timer(_ =>
                        {
                            lock (_connLock)
                            {
                                if (_connectionCount <= 0)
                                {
                                    _exitEvent?.Set();
                                }
                            }
                        }, null, 2000, Timeout.Infinite);
                    }
                }

                try { writer.Close(); } catch {}
            }
            else if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/click")
            {
                string? id = request.QueryString["id"];
                if (id != null)
                {
                    using var reader = new StreamReader(request.InputStream);
                    string body = reader.ReadToEnd();

                    // Parse request body containing values: {"values": {"cmd-field": "value"}}
                    var values = ParseJsonValues(body);

                    // Sync client values back to C# GuiWidget instances
                    if (_rootWidget != null)
                    {
                        SyncWidgetValues(_rootWidget, values);
                    }

                    // Run the associated action block in the interpreter
                    var widget = FindWidget(_rootWidget, id);
                    if (widget?.Action != null)
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                interpreter.Evaluate(widget.Action, context);
                            }
                            catch (Exception ex)
                            {
                                context.Output.WriteLine($"GUI action error: {ex.Message}");
                            }
                        });
                    }
                }

                response.StatusCode = 200;
                response.OutputStream.Close();
            }
            else
            {
                response.StatusCode = 404;
                response.OutputStream.Close();
            }
        }
        catch (Exception)
        {
            try
            {
                response.StatusCode = 500;
                response.OutputStream.Close();
            }
            catch {}
        }
    }

    internal static Dictionary<string, string> ParseJsonValues(string json)
    {
        var dict = new Dictionary<string, string>();
        
        // Very basic JSON parser to extract values without external library
        int valuesIdx = json.IndexOf("\"values\"");
        if (valuesIdx < 0) return dict;

        int startBrace = json.IndexOf("{", valuesIdx);
        int endBrace = json.IndexOf("}", startBrace);
        if (startBrace < 0 || endBrace < 0 || endBrace <= startBrace) return dict;

        string sub = json.Substring(startBrace + 1, endBrace - startBrace - 1);
        var tokens = sub.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var t in tokens)
        {
            var parts = t.Split(new[] { ':' }, 2);
            if (parts.Length == 2)
            {
                string key = parts[0].Trim().Trim('"');
                string val = parts[1].Trim().Trim('"');
                dict[key] = val;
            }
        }

        return dict;
    }

    private static void SyncWidgetValues(GuiWidget widget, Dictionary<string, string> values)
    {
        if (values.TryGetValue(widget.Id, out string? valStr))
        {
            if (widget.Type == "field" || widget.Type == "choice")
            {
                widget.CurrentValue = new Text(valStr);
            }
            else if (widget.Type == "check")
            {
                widget.CurrentValue = new Logic(valStr.ToLower() == "true");
            }
            else if (widget.Type == "slider")
            {
                if (int.TryParse(valStr, out int parsedVal))
                    widget.CurrentValue = new Integer(parsedVal);
            }
        }

        foreach (var child in widget.Children)
        {
            SyncWidgetValues(child, values);
        }
    }

    private static GuiWidget? FindWidget(GuiWidget? widget, string id)
    {
        if (widget == null) return null;
        if (widget.Id == id) return widget;

        foreach (var child in widget.Children)
        {
            var found = FindWidget(child, id);
            if (found != null) return found;
        }

        return null;
    }

    private static void SendSseUpdate(string id, string val)
    {
        lock (_sseLock)
        {
            if (_sseWriter != null)
            {
                try
                {
                    string safeVal = val.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
                    var json = $"{{\"action\":\"update\",\"id\":\"{id}\",\"value\":\"{safeVal}\"}}";
                    _sseWriter.Write($"data: {json}\n\n");
                    _sseWriter.Flush();
                }
                catch {}
            }
        }
    }

    private static string BuildHtml()
    {
        string title = _rootWidget?.Text ?? "Ragnar GUI";
        string content = "";

        if (_rootWidget != null)
        {
            foreach (var child in _rootWidget.Children)
            {
                content += RenderWidgetHtml(child);
            }
        }

        string cssContent = LoadThemeCss(_currentTheme);

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>{title}</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        {cssContent}
    </style>
</head>
<body>
    <div class=""gui-container"">
        <div class=""column"">
            {content}
        </div>
    </div>

    <script>
        const clientValues = {{}};

        // Cache initial values
        function syncInputs() {{
            document.querySelectorAll('input, select').forEach(el => {{
                if (el.type === 'checkbox') {{
                    clientValues[el.id] = el.checked;
                }} else {{
                    clientValues[el.id] = el.value;
                }}
            }});
        }}
        syncInputs();

        function updateValue(id, val) {{
            clientValues[id] = val;
        }}

        function triggerAction(id) {{
            fetch('/click?id=' + id, {{
                method: 'POST',
                headers: {{
                    'Content-Type': 'application/json'
                }},
                body: JSON.stringify({{ values: clientValues }})
            }}).catch(err => console.error(err));
        }}

        function triggerClick(id) {{
            triggerAction(id);
        }}

        // Server-Sent Events
        const eventSource = new EventSource('/events');
        eventSource.onmessage = function(event) {{
            const data = JSON.parse(event.data);
            const el = document.getElementById(data.id);
            if (!el) return;

            if (data.action === ""update"") {{
                if (el.tagName === 'INPUT' || el.tagName === 'SELECT') {{
                    if (el.type === 'checkbox') {{
                        el.checked = data.value.toLowerCase() === 'true';
                        clientValues[data.id] = el.checked;
                    }} else {{
                        el.value = data.value;
                        clientValues[data.id] = el.value;
                    }}
                }} else if (el.tagName === 'IMG') {{
                    el.src = data.value;
                }} else {{
                    el.textContent = data.value;
                }}
            }}
        }};

        eventSource.onerror = function() {{
            console.log(""SSE connection dropped"");
        }};
    </script>
</body>
</html>";
    }

    internal static string RenderWidgetHtml(GuiWidget widget)
    {
        var sb = new StringBuilder();

        switch (widget.Type)
        {
            case "row":
                sb.Append($"<div id=\"{widget.Id}\" class=\"row\">");
                foreach (var child in widget.Children) sb.Append(RenderWidgetHtml(child));
                sb.Append("</div>");
                break;

            case "column":
                sb.Append($"<div id=\"{widget.Id}\" class=\"column\">");
                foreach (var child in widget.Children) sb.Append(RenderWidgetHtml(child));
                sb.Append("</div>");
                break;

            case "heading":
                sb.Append($"<h1 id=\"{widget.Id}\" class=\"gui-heading\">{widget.Text}</h1>");
                break;

            case "text":
                sb.Append($"<span id=\"{widget.Id}\" class=\"gui-text\">{widget.Text}</span>");
                break;

            case "button":
                sb.Append($"<button id=\"{widget.Id}\" onclick=\"triggerClick('{widget.Id}')\" class=\"gui-btn\">{widget.Text}</button>");
                break;

            case "field":
                sb.Append($"<input id=\"{widget.Id}\" type=\"text\" class=\"gui-field\" value=\"{widget.CurrentValue.ToUserString()}\" oninput=\"updateValue('{widget.Id}', this.value)\"{(widget.Action != null ? " onchange=\"triggerAction('" + widget.Id + "')\"" : "")}/>");
                break;

            case "choice":
                sb.Append($"<select id=\"{widget.Id}\" class=\"gui-choice\" onchange=\"updateValue('{widget.Id}', this.value){(widget.Action != null ? "; triggerAction('" + widget.Id + "')" : "")}\">");
                foreach (var opt in widget.Options)
                {
                    bool isSelected = opt == widget.CurrentValue.ToUserString();
                    sb.Append($"<option value=\"{opt}\"{(isSelected ? " selected=\"selected\"" : "")}>{opt}</option>");
                }
                sb.Append("</select>");
                break;

            case "check":
                bool isChecked = widget.CurrentValue is Logic l && l.Condition;
                sb.Append($"<label class=\"gui-checkbox-label\"><input id=\"{widget.Id}\" type=\"checkbox\" class=\"gui-checkbox\" {(isChecked ? "checked" : "")} onchange=\"updateValue('{widget.Id}', this.checked){(widget.Action != null ? "; triggerAction('" + widget.Id + "')" : "")}\"/> <span class=\"gui-checkbox-custom\"></span>{widget.Text}</label>");
                break;

            case "slider":
                int val = widget.CurrentValue is Integer integer ? (int)integer.Number : 0;
                sb.Append($"<div class=\"gui-slider-container\"><span class=\"gui-slider-label\">{widget.Text}</span><input id=\"{widget.Id}\" type=\"range\" min=\"0\" max=\"100\" value=\"{val}\" class=\"gui-slider\" oninput=\"updateValue('{widget.Id}', this.value)\"{(widget.Action != null ? " onchange=\"triggerAction('" + widget.Id + "')\"" : "")}/></div>");
                break;

            case "image":
                string widthAttr = !string.IsNullOrEmpty(widget.Width) ? $" width=\"{widget.Width}\"" : "";
                string heightAttr = !string.IsNullOrEmpty(widget.Height) ? $" height=\"{widget.Height}\"" : "";
                sb.Append($"<img id=\"{widget.Id}\" src=\"{GetImageSource(widget.Text)}\" class=\"gui-img\" alt=\"{widget.Id}\"{widthAttr}{heightAttr}/>");
                break;
        }

        return sb.ToString();
    }

    private static string GetImageSource(string pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl)) return "";

        if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            pathOrUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return pathOrUrl;
        }

        try
        {
            if (System.IO.File.Exists(pathOrUrl))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(pathOrUrl);
                string ext = System.IO.Path.GetExtension(pathOrUrl).ToLowerInvariant();
                string mimeType = ext switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".svg" => "image/svg+xml",
                    ".webp" => "image/webp",
                    _ => "image/png"
                };
                return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
            }
        }
        catch {}

        return pathOrUrl;
    }

    private static string LoadThemeCss(string themeName)
    {
        var assembly = typeof(GuiFunctions).Assembly;
        string resourceName = $"Ragnar.Themes.{themeName}.css";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new Exception($"Embedded theme resource '{resourceName}' not found.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
