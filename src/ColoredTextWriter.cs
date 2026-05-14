using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ragnar;

public class ColoredTextWriter : TextWriter
{
    private readonly TextWriter _baseWriter;
    private readonly ConsoleColor _color;

    public ColoredTextWriter(TextWriter baseWriter, ConsoleColor color)
    {
        _baseWriter = baseWriter;
        _color = color;
    }

    public override Encoding Encoding => _baseWriter.Encoding;

    public override void Write(char value)
    {
        Console.ForegroundColor = _color;
        _baseWriter.Write(value);
        Console.ResetColor();
    }

    public override void Write(string? value)
    {
        Console.ForegroundColor = _color;
        _baseWriter.Write(value);
        Console.ResetColor();
    }

    public override void WriteLine(string? value)
    {
        Console.ForegroundColor = _color;
        _baseWriter.WriteLine(value);
        Console.ResetColor();
    }
}
