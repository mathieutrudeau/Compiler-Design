
using AbstractSyntaxTreeGeneration;

namespace SemanticAnalyzer;

public class SemanticAnalyzer : ISemanticAnalyzer
{

    private IASTNode _astRoot;
    private ISymbolTable _globalSymbolTable;


    public SemanticAnalyzer(IASTNode aSTNode)
    {
        _astRoot = aSTNode;
        _globalSymbolTable = new SymbolTable("Global", null);


    }

    public bool Analyze()
    {
        // Generate the Symbol Table by visiting each node in the AST
        _astRoot.Visit(_globalSymbolTable);

        // Perform the semantic analysis once the symbol table has been built
        

        return true;
    }

    public ISymbolTable GetGlobalSymbolTable()
    {
        return _globalSymbolTable;
    }
}