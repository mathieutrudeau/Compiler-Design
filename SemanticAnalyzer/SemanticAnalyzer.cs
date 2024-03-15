
using AbstractSyntaxTreeGeneration;

namespace SemanticAnalyzer;

public class SemanticAnalyzer : ISemanticAnalyzer
{

    private IASTNode _astRoot;
    private ISymbolTable _globalSymbolTable;

    private List<ISemanticWarning> _warnings;
    private List<ISemanticError> _errors;


    public SemanticAnalyzer(IASTNode aSTNode)
    {
        _astRoot = aSTNode;
        _globalSymbolTable = new SymbolTable("Global", null);
        _warnings = new List<ISemanticWarning>();
        _errors = new List<ISemanticError>();

    }

    public bool Analyze()
    {
        // Generate the Symbol Table by visiting each node in the AST
        _astRoot.Visit(_globalSymbolTable, _warnings, _errors);

        if(_errors.Count == 0)
            // Perform the semantic analysis once the symbol table has been built
            _astRoot.SemanticCheck(_globalSymbolTable, _warnings, _errors);


        if (_warnings.Count > 0)
        {
            // If there are any warnings, print them
            foreach (var warning in _warnings)
            {
                Console.WriteLine(warning);
            }
        }        

        if (_errors.Count > 0)
        {
            // If there are any errors, print them and return false
            foreach (var error in _errors)
            {
                Console.WriteLine(error);
            }
            return false;   
        }

        return true;
    }

    public ISymbolTable GetGlobalSymbolTable()
    {
        return _globalSymbolTable;
    }
}