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
