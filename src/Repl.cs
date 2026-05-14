using System;
using System.Collections.Generic;
using System.Text;

namespace Ragnar;

public class Repl
{
    internal readonly List<string> _history = new();
    private int _historyIndex = -1;
    private string _savedInput = "";

    private int _currentRowOffset = 0;
    private int _maxRowsUsed = 0;
    private StringBuilder _currentLine = new();

    public string ReadLine(string prompt)
    {
        _currentRowOffset = 0;
        _maxRowsUsed = 0;
        
        _currentLine.Clear();
        int pos = 0;
        _historyIndex = _history.Count;
        _savedInput = "";

        UpdateLine(prompt, pos);

        while (true)
        {
            var keyInfo = Console.ReadKey(true);
            var key = keyInfo.Key;

            if (key == ConsoleKey.Enter)
            {
                UpdateCursor(prompt, _currentLine.Length);
                Console.WriteLine();
                var result = _currentLine.ToString();
                return result;
            }
            else if (key == ConsoleKey.Backspace)
            {
                if (pos > 0)
                {
                    pos--;
                    _currentLine.Remove(pos, 1);
                    UpdateLine(prompt, pos);
                }
            }
            else if (key == ConsoleKey.Delete)
            {
                if (pos < _currentLine.Length)
                {
                    _currentLine.Remove(pos, 1);
                    UpdateLine(prompt, pos);
                }
            }
            else if (key == ConsoleKey.LeftArrow)
            {
                if (pos > 0)
                {
                    pos--;
                    UpdateCursor(prompt, pos);
                }
            }
            else if (key == ConsoleKey.RightArrow)
            {
                if (pos < _currentLine.Length)
                {
                    pos++;
                    UpdateCursor(prompt, pos);
                }
            }
            else if (key == ConsoleKey.UpArrow)
            {
                if (_historyIndex > 0)
                {
                    if (_historyIndex == _history.Count)
                    {
                        _savedInput = _currentLine.ToString();
                    }
                    _historyIndex--;
                    _currentLine.Clear();
                    _currentLine.Append(_history[_historyIndex]);
                    pos = _currentLine.Length;
                    UpdateLine(prompt, pos);
                }
            }
            else if (key == ConsoleKey.DownArrow)
            {
                if (_historyIndex < _history.Count)
                {
                    _historyIndex++;
                    _currentLine.Clear();
                    if (_historyIndex == _history.Count)
                    {
                        _currentLine.Append(_savedInput);
                    }
                    else
                    {
                        _currentLine.Append(_history[_historyIndex]);
                    }
                    pos = _currentLine.Length;
                    UpdateLine(prompt, pos);
                }
            }
            else if (key == ConsoleKey.Home)
            {
                pos = 0;
                UpdateCursor(prompt, pos);
            }
            else if (key == ConsoleKey.End)
            {
                pos = _currentLine.Length;
                UpdateCursor(prompt, pos);
            }
            else if (keyInfo.KeyChar != '\0' && !char.IsControl(keyInfo.KeyChar))
            {
                _currentLine.Insert(pos, keyInfo.KeyChar);
                pos++;
                UpdateLine(prompt, pos);
            }
        }
    }

    public void AddHistory(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        if (_history.Count > 0 && _history[^1] == line) return;
        _history.Add(line);
    }

    private void UpdateLine(string prompt, int pos)
    {
        string fullText = prompt + _currentLine.ToString();
        int width = Console.BufferWidth;

        // 1. Move to the original start row
        int startTop = Console.CursorTop - _currentRowOffset;
        if (startTop < 0) startTop = 0; // Safety
        
        Console.SetCursorPosition(0, startTop);

        // 2. Clear all rows previously used
        for (int i = 0; i < _maxRowsUsed; i++)
        {
            int row = startTop + i;
            if (row >= Console.BufferHeight) break;
            Console.SetCursorPosition(0, row);
            Console.Write(new string(' ', width));
        }

        // 3. Write the new text
        Console.SetCursorPosition(0, startTop);
        Console.Write(fullText);

        // 4. Update state based on what was actually written
        int endTop = Console.CursorTop;
        int endLeft = Console.CursorLeft;
        
        // If we ended exactly at the right edge, the cursor might wrap on next write
        // but Console.CursorTop is already advanced.
        
        _currentRowOffset = endTop - startTop;
        _maxRowsUsed = Math.Max(_maxRowsUsed, _currentRowOffset + 1);

        UpdateCursor(prompt, pos);
    }

    private void UpdateCursor(string prompt, int pos)
    {
        string textToPos = prompt + _currentLine.ToString().Substring(0, pos);
        int width = Console.BufferWidth;

        int left = 0;
        int rows = 0;

        foreach (char c in textToPos)
        {
            if (c == '\n')
            {
                left = 0;
                rows++;
            }
            else
            {
                left++;
                if (left >= width)
                {
                    left = 0;
                    rows++;
                }
            }
        }

        int startTop = Console.CursorTop - _currentRowOffset;
        int targetTop = startTop + rows;

        // Final safety check for buffer boundaries
        targetTop = Math.Clamp(targetTop, 0, Console.BufferHeight - 1);
        left = Math.Clamp(left, 0, width - 1);

        Console.SetCursorPosition(left, targetTop);
    }
}
