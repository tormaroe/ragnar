namespace Ragnar;

public class BreakException : Exception { }
public class ContinueException : Exception { }
public class ReturnException(Value value) : Exception 
{
    public Value Value { get; } = value;
}

public class IncompleteInputException(string message) : Exception(message) { }

public class ThrowException(Value thrownValue) : Exception
{
    public Value ThrownValue { get; } = thrownValue;
}

public class RagnarRuntimeException : Exception
{
    public Value ErrorToken { get; }
    public Block ErrorBlock { get; }
    public int ErrorIndex { get; }

    public RagnarRuntimeException(string message, Value errorToken, Block errorBlock, int errorIndex)
        : base(FormatMessage(message, errorToken, errorBlock, errorIndex))
    {
        ErrorToken = errorToken;
        ErrorBlock = errorBlock;
        ErrorIndex = errorIndex;
    }

    public RagnarRuntimeException(string message, Exception innerException, Value errorToken, Block errorBlock, int errorIndex)
        : base(FormatMessage(message, errorToken, errorBlock, errorIndex), innerException)
    {
        ErrorToken = errorToken;
        ErrorBlock = errorBlock;
        ErrorIndex = errorIndex;
    }

    private static string FormatMessage(string message, Value errorToken, Block errorBlock, int errorIndex)
    {
        var near = GetNearString(errorToken, errorBlock, errorIndex);
        return $"{message}\nNear: {near}";
    }

    private static string GetNearString(Value errorToken, Block errorBlock, int errorIndex)
    {
        var sb = new System.Text.StringBuilder();
        if (errorIndex > 0 && errorIndex - 1 < errorBlock.Children.Count)
        {
            sb.Append(errorBlock.Children[errorIndex - 1].ToString());
            sb.Append(" ");
        }
        sb.Append("** ");
        sb.Append(errorToken.ToString());
        sb.Append(" **");
        if (errorIndex + 1 < errorBlock.Children.Count)
        {
            sb.Append(" ");
            sb.Append(errorBlock.Children[errorIndex + 1].ToString());
        }
        return sb.ToString();
    }
}
