using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Ragnar;

public class Repl
{
    internal readonly List<string> _history = new();
    private int _historyIndex = -1;
    private string _savedInput = "";
    private int _lastMaxTextLength = 0;

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

    public string ReadLine(string prompt)
    {
        WritePrompt(prompt);

        var line = new StringBuilder();
        int pos = 0;
        _historyIndex = _history.Count;
        _savedInput = "";
        _lastMaxTextLength = 0;

        int startTop = Console.CursorTop;
        int startLeft = Console.CursorLeft;

        while (true)
        {
            var keyInfo = Console.ReadKey(true);
            var key = keyInfo.Key;

            if (key == ConsoleKey.Enter)
            {
                MoveToEnd(prompt, line.Length, startTop, startLeft);
                Console.WriteLine();
                Console.ResetColor();
                return line.ToString();
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

    public void AddHistory(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        
        string sanitized = string.Join(" ", line.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(l => l.Trim()));
        
        if (string.IsNullOrWhiteSpace(sanitized)) return;
        if (_history.Count > 0 && _history[^1] == sanitized) return;
        
        _history.Add(sanitized);
    }

    private void Redraw(string prompt, StringBuilder line, int pos, int startTop, int startLeft)
    {
        int promptStartLeft = startLeft - prompt.Length;
        Console.SetCursorPosition(promptStartLeft, startTop);
        
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
        int width = Console.BufferWidth;
        int left = totalPos % width;
        int top = startTop + (totalPos / width);
        
        top = Math.Min(top, Console.BufferHeight - 1);
        Console.SetCursorPosition(left, top);
    }

    private void MoveToEnd(string prompt, int length, int startTop, int startLeft)
    {
        SetCursor(prompt, length, startTop, startLeft);
    }
}
