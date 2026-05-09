
namespace rebelly;

public enum TokenType { OpenBracket, CloseBracket, Value }

public record Token(TokenType Type, Value? Value = null);