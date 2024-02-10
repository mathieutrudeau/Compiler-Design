namespace SyntacticAnalyzer;

public interface IParser
{
    public bool Parse();

    public bool Match();
}