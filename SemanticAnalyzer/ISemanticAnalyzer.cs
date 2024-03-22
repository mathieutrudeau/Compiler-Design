namespace SemanticAnalyzer;


/// <summary>
/// Represents a semantic analyzer. 
/// </summary>
public interface ISemanticAnalyzer
{
    /// <summary>
    /// Analyzes the AST and generates the symbol table.
    /// </summary>
    /// <returns>True if the analysis was successful, false otherwise.</returns>
    public bool Analyze();

    /// <summary>
    /// Gets the global symbol table.
    /// </summary>
    /// <returns>The global symbol table.</returns>
    /// <remarks>
    /// The global symbol table contains all the symbols that are available in the entire program.
    /// It must be generated before calling this method. 
    /// The generation of the global symbol table is done by calling the Analyze method.
    /// </remarks>
    public ISymbolTable GetGlobalSymbolTable();   
}
