namespace SyntacticAnalyzer;

public interface IParseList
{
    public void Add(string Rule);
    public void Print();
    public string GetDerivation();
}