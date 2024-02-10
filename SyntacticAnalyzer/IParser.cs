using LexicalAnalyzer;

namespace SyntacticAnalyzer;

public interface IParser
{
    public bool Parse();

    public bool Match(TokenType tokenType);
}