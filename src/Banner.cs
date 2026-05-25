namespace Ragnar;

public class Banner(
    string text, 
    ConsoleColor foreground,
    int leftPadding = 2
)
{
    private readonly string _text = text;
    private readonly Dictionary<int, string> _trailingText = [];

    public ConsoleColor Foreground { get; } = foreground;

    public Banner AddTrailingText(int line, string text)
    {
        _trailingText[line] = text;
        return this;
    }

    public void Print()
    {
        Console.WriteLine(); // Extra line before banner

        var bannerChars = _text.Select(c => new BannerCharacter(c, this)).ToArray();

        for (int i = 0; i < 7; i++)
        {
            PrintLeftPadding();

            foreach (var bc in bannerChars)
            {
                bc.PrintLine(i);
            }

            if (_trailingText.TryGetValue(i, out var trailingText))
            {
                Console.Write(trailingText);
            }

            Console.WriteLine();
        }
        Console.WriteLine();
    }

    private void PrintLeftPadding()
    {
        for (int i = 0; i < leftPadding; i++)
        {
            Console.Write(' ');
        }
    }
}

public class BannerCharacter(char character, Banner banner)
{
    private const char BLOCK = '█';
    private readonly char c = character;

    private static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
    {
        ['r'] = ["████ ", "█    ", "█    ", "█    ", "█    "],
        ['a'] = ["████ ", "   █ ", "████ ", "█  █ ", "████ "],
        ['g'] = ["████ ", "█  █ ", "█  █ ", "█  █ ", "████ ", "   █ ", "████ "],
        ['n'] = ["███  ", "█  █ ", "█  █ ", "█  █ ", "█  █ "],
    };

    public void PrintLine(int line)
    {
        if (Font.TryGetValue(c, out var fontLines))
        {
            if (line >= fontLines.Length)
            {
                Console.Write("     ");
                return;
            }
            foreach (char pixel in fontLines[line])
            {
                PrintPixel(pixel);
            }
        }
        else
        {
            throw new ArgumentException($"Character '{c}' not supported in banner font.");
        }
    }

    private void PrintPixel(char pixel)
    {
        Console.ForegroundColor = banner.Foreground;
        Console.Write(pixel);
        Console.ResetColor();
    }
}