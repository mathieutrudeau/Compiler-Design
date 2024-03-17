
using AbstractSyntaxTreeGeneration;

namespace SemanticAnalyzer;

public class SemanticAnalyzer : ISemanticAnalyzer
{

    private IASTNode _astRoot;
    private ISymbolTable _globalSymbolTable;

    private List<ISemanticWarning> _warnings;
    private List<ISemanticError> _errors;


    /// <summary>
    /// Constructs a new SemanticAnalyzer with the given AST root.
    /// </summary>
    /// <param name="aSTNode">The root of the AST to analyze.</param>
    public SemanticAnalyzer(IASTNode aSTNode)
    {
        // Initialize the SemanticAnalyzer with the given AST root
        _astRoot = aSTNode;
        
        // Initialize the global symbol table
        _globalSymbolTable = new SymbolTable()
        {
            Name = "Global",
            Entries = new LinkedList<ISymbolTableEntry>(),
            Parent = null
        };

        // Initialize the lists of warnings and errors
        _warnings = new List<ISemanticWarning>();
        _errors = new List<ISemanticError>();

    }

    /// <summary>
    /// Analyzes the AST and builds the symbol table.
    /// </summary>
    /// <returns>True if the analysis was successful, false otherwise.</returns>
    /// <remarks>
    /// If the analysis was successful, the global symbol table can be retrieved using the GetGlobalSymbolTable method.
    /// </remarks>
    public bool Analyze()
    {
        // Generate the Symbol Table by visiting each node in the AST
        _astRoot.Visit(_globalSymbolTable, _warnings, _errors);

        if(_errors.Count == 0)
            //Perform the semantic analysis once the symbol table has been built
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