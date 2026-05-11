
namespace Ragnar;

public enum TokenType { OpenBracket, CloseBracket, Value, OpenParen, CloseParen }

public record Token(TokenType Type, Value? Value = null);