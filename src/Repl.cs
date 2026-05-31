using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Ragnar;

public class Repl
{
    private static ConsoleKeyInfo DefaultReadKey() => Console.ReadKey(true);
    public Func<ConsoleKeyInfo> ReadKeyFunc { get; set; } = DefaultReadKey;
    internal readonly List<string> _history = new();
    private int _historyIndex = -1;
    private string _savedInput = "";
    private int _lastMaxTextLength = 0;

    private bool IsSeparator(char c)
    {
        return char.IsWhiteSpace(c) || c == '[' || c == ']' || c == '(' || c == ')' || c == '{' || c == '}' || c == '"' || c == ':' || c == ';' || c == '/';
    }

    public static void WritePrompt(string text, bool newline = false) => Write(text, ReplConfig.PromptColor, newline);
    public static void WriteInput(string text, bool newline = false) => Write(text, ReplConfig.InputColor, newline);
    public static void WritePrint(string text, bool newline = false) => Write(text, ReplConfig.PrinterColor, newline);
    public static void WriteResult(string text, bool newline = true) => Write(text, ReplConfig.ResultColor, newline);
    public static void WriteError(string text, bool newline = true) => Write(text, ReplConfig.ErrorColor, newline);

    public static void Write(string text, ConsoleColor color, bool newline = false)
    {
        Console.ForegroundColor = color;
        if (newline) Console.WriteLine(text);
        else Console.Write(text);
        Console.ResetColor();
    }

    public static void PrintColors()
    {
        var colors = Enum.GetValues<ConsoleColor>();
        foreach (var fg in colors)
        {
            Console.ForegroundColor = fg;
            Console.Write($" ██ {fg,-15}");
            Console.WriteLine();
        }
    }

    public string ReadLine(string prompt, Context? context = null)
    {
        if (Console.IsInputRedirected && ReadKeyFunc == DefaultReadKey)
        {
            WritePrompt(prompt);
            return Console.ReadLine() ?? "";
        }

        WritePrompt(prompt);

        var line = new StringBuilder();
        int pos = 0;
        _historyIndex = _history.Count;
        _savedInput = "";
        _lastMaxTextLength = 0;

        int startTop = SafeGetCursorTop();
        int startLeft = SafeGetCursorLeft();

        while (true)
        {
            var keyInfo = ReadKeyFunc();
            var key = keyInfo.Key;

            if (key == ConsoleKey.Enter)
            {
                MoveToEnd(prompt, line.Length, startTop, startLeft);
                Console.WriteLine();
                Console.ResetColor();
                return line.ToString();
            }
            else if (key == ConsoleKey.Tab && context != null)
            {
                // Find start of the word at the cursor position
                int wordStart = pos;
                while (wordStart > 0 && !IsSeparator(line[wordStart - 1]))
                {
                    wordStart--;
                }

                string prefix = line.ToString(wordStart, pos - wordStart);
                if (prefix.Length > 0)
                {
                    // Find matches
                    var words = context.GetAllBindings().Keys;
                    var matches = words
                        .Where(w => w.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(w => w)
                        .ToList();

                    if (matches.Count == 1)
                    {
                        // Single match - replace inline
                        line.Remove(wordStart, pos - wordStart);
                        line.Insert(wordStart, matches[0]);
                        pos = wordStart + matches[0].Length;
                        Redraw(prompt, line, pos, startTop, startLeft);
                    }
                    else if (matches.Count > 1)
                    {
                        // Multiple matches - enter inline cycling mode!
                        int cycleIndex = 0;
                        string originalLineText = line.ToString();
                        int originalPos = pos;

                        // Initial completion
                        line.Remove(wordStart, pos - wordStart);
                        line.Insert(wordStart, matches[cycleIndex]);
                        pos = wordStart + matches[cycleIndex].Length;
                        Redraw(prompt, line, pos, startTop, startLeft);

                        bool selectionDone = false;
                        while (!selectionDone)
                        {
                            var nextKeyInfo = ReadKeyFunc();
                            var nextKey = nextKeyInfo.Key;
                            
                            // Check if Shift+Tab was pressed (Shift-Tab has Key = Tab and Shift modifier active)
                            bool isShiftTab = nextKey == ConsoleKey.Tab && (nextKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0;

                            if (nextKey == ConsoleKey.Tab || nextKey == ConsoleKey.RightArrow || nextKey == ConsoleKey.LeftArrow)
                            {
                                if (isShiftTab || nextKey == ConsoleKey.LeftArrow)
                                {
                                    cycleIndex = (cycleIndex - 1 + matches.Count) % matches.Count;
                                }
                                else // Tab or RightArrow
                                {
                                    cycleIndex = (cycleIndex + 1) % matches.Count;
                                }

                                line.Clear();
                                line.Append(originalLineText.Substring(0, wordStart));
                                line.Append(matches[cycleIndex]);
                                line.Append(originalLineText.Substring(originalPos));
                                pos = wordStart + matches[cycleIndex].Length;
                                Redraw(prompt, line, pos, startTop, startLeft);
                            }
                            else if (nextKey == ConsoleKey.Escape)
                            {
                                // Cancel completion - restore original state
                                line.Clear();
                                line.Append(originalLineText);
                                pos = originalPos;
                                Redraw(prompt, line, pos, startTop, startLeft);
                                selectionDone = true;
                            }
                            else if (nextKey == ConsoleKey.Enter)
                            {
                                // Accept and return line immediately
                                MoveToEnd(prompt, line.Length, startTop, startLeft);
                                Console.WriteLine();
                                Console.ResetColor();
                                return line.ToString();
                            }
                            else
                            {
                                // Any other key: accept the currently selected option,
                                // and exit the sub-loop. The outer loop will process this key next.
                                selectionDone = true;

                                // Process the key immediately
                                if (nextKey == ConsoleKey.Backspace)
                                {
                                    if (pos > 0)
                                    {
                                        pos--;
                                        line.Remove(pos, 1);
                                        Redraw(prompt, line, pos, startTop, startLeft);
                                    }
                                }
                                else if (nextKey == ConsoleKey.Delete)
                                {
                                    if (pos < line.Length)
                                    {
                                        line.Remove(pos, 1);
                                        Redraw(prompt, line, pos, startTop, startLeft);
                                    }
                                }
                                else if (nextKey == ConsoleKey.LeftArrow)
                                {
                                    if (pos > 0)
                                    {
                                        pos--;
                                        SetCursor(prompt, pos, startTop, startLeft);
                                    }
                                }
                                else if (nextKey == ConsoleKey.RightArrow)
                                {
                                    if (pos < line.Length)
                                    {
                                        pos++;
                                        SetCursor(prompt, pos, startTop, startLeft);
                                    }
                                }
                                else if (nextKey == ConsoleKey.Home)
                                {
                                    pos = 0;
                                    SetCursor(prompt, pos, startTop, startLeft);
                                }
                                else if (nextKey == ConsoleKey.End)
                                {
                                    pos = line.Length;
                                    SetCursor(prompt, pos, startTop, startLeft);
                                }
                                else if (nextKeyInfo.KeyChar != '\0' && !char.IsControl(nextKeyInfo.KeyChar))
                                {
                                    line.Insert(pos, nextKeyInfo.KeyChar);
                                    pos++;
                                    Redraw(prompt, line, pos, startTop, startLeft);
                                }
                            }
                        }
                    }
                }
            }
            else if (key == ConsoleKey.Backspace)
            {
                if (pos > 0)
                {
                    pos--;
                    line.Remove(pos, 1);
                    Redraw(prompt, line, pos, startTop, startLeft);
                }
            }
            else if (key == ConsoleKey.Delete)
            {
                if (pos < line.Length)
                {
                    line.Remove(pos, 1);
                    Redraw(prompt, line, pos, startTop, startLeft);
                }
            }
            else if (key == ConsoleKey.LeftArrow)
            {
                if (pos > 0)
                {
                    pos--;
                    SetCursor(prompt, pos, startTop, startLeft);
                }
            }
            else if (key == ConsoleKey.RightArrow)
            {
                if (pos < line.Length)
                {
                    pos++;
                    SetCursor(prompt, pos, startTop, startLeft);
                }
            }
            else if (key == ConsoleKey.UpArrow)
            {
                if (_historyIndex > 0)
                {
                    if (_historyIndex == _history.Count) _savedInput = line.ToString();
                    _historyIndex--;
                    line.Clear();
                    line.Append(_history[_historyIndex]);
                    pos = line.Length;
                    Redraw(prompt, line, pos, startTop, startLeft);
                }
            }
            else if (key == ConsoleKey.DownArrow)
            {
                if (_historyIndex < _history.Count)
                {
                    _historyIndex++;
                    line.Clear();
                    if (_historyIndex == _history.Count) line.Append(_savedInput);
                    else line.Append(_history[_historyIndex]);
                    pos = line.Length;
                    Redraw(prompt, line, pos, startTop, startLeft);
                }
            }
            else if (key == ConsoleKey.Home)
            {
                pos = 0;
                SetCursor(prompt, pos, startTop, startLeft);
            }
            else if (key == ConsoleKey.End)
            {
                pos = line.Length;
                SetCursor(prompt, pos, startTop, startLeft);
            }
            else if (keyInfo.KeyChar != '\0' && !char.IsControl(keyInfo.KeyChar))
            {
                line.Insert(pos, keyInfo.KeyChar);
                pos++;
                Redraw(prompt, line, pos, startTop, startLeft);
            }
        }
    }

    public bool AddHistory(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        
        string sanitized = string.Join(" ", line.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(l => l.Trim()));
        
        if (string.IsNullOrWhiteSpace(sanitized)) return false;
        if (_history.Count > 0 && _history[^1] == sanitized) return false;
        
        _history.Add(sanitized);
        return true;
    }

    private void Redraw(string prompt, StringBuilder line, int pos, int startTop, int startLeft)
    {
        int promptStartLeft = startLeft - prompt.Length;
        SafeSetCursorPosition(promptStartLeft, startTop);
        
        WritePrompt(prompt);
        
        string text = line.ToString();
        Console.Write(text);
        
        int currentTotalLength = prompt.Length + text.Length;
        if (currentTotalLength < _lastMaxTextLength)
        {
            Console.Write(new string(' ', _lastMaxTextLength - currentTotalLength));
        }
        
        _lastMaxTextLength = Math.Max(_lastMaxTextLength, currentTotalLength);
        
        SetCursor(prompt, pos, startTop, startLeft);
    }

    private void SetCursor(string prompt, int pos, int startTop, int startLeft)
    {
        int promptStartLeft = startLeft - prompt.Length;
        int totalPos = promptStartLeft + prompt.Length + pos;
        int width = SafeGetBufferWidth();
        int left = totalPos % width;
        int top = startTop + (totalPos / width);
        
        top = Math.Min(top, SafeGetBufferHeight() - 1);
        SafeSetCursorPosition(left, top);
    }

    private void MoveToEnd(string prompt, int length, int startTop, int startLeft)
    {
        SetCursor(prompt, length, startTop, startLeft);
    }

    private int SafeGetCursorTop()
    {
        try { return Console.CursorTop; }
        catch { return 0; }
    }

    private int SafeGetCursorLeft()
    {
        try { return Console.CursorLeft; }
        catch { return 0; }
    }

    private int SafeGetBufferWidth()
    {
        try
        {
            int w = Console.BufferWidth;
            return w > 0 ? w : 80;
        }
        catch { return 80; }
    }

    private int SafeGetBufferHeight()
    {
        try
        {
            int h = Console.BufferHeight;
            return h > 0 ? h : 24;
        }
        catch { return 24; }
    }

    private void SafeSetCursorPosition(int left, int top)
    {
        try
        {
            Console.SetCursorPosition(left, top);
        }
        catch {}
    }
}
