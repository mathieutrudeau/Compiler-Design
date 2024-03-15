
namespace SemanticAnalyzer;


public interface ISemanticAnalyzer
{
    public bool Analyze();

    public ISymbolTable GetGlobalSymbolTable();   
}
