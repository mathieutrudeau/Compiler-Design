using AbstractSyntaxTreeGeneration;

namespace SemanticAnalyzer;

/// <summary>
/// Represents a Symbol Table.
/// </summary>
public interface ISymbolTable
{
    /// <summary>
    /// The name of the symbol table.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// The list of entries in the symbol table.
    /// </summary>
    public LinkedList<ISymbolTableEntry> Entries { get; set; }
    
    /// <summary>
    /// The parent symbol table. This will be null for the global symbol table.
    /// </summary>
    public ISymbolTable? Parent { get; set; }


    public void AddEntry(ISymbolTableEntry entry);

    /// <summary>
    /// Looks up the symbol table entry with the given name. This method will search the current symbol table and all of its ancestors.
    /// </summary>
    /// <param name="name">The name of the entry to look up.</param>
    /// <returns> The symbol table entry with the given name, or null if no such entry exists.</returns>
    public ISymbolTableEntry? Lookup(string name);

    /// <summary>
    /// Checks if the given name is already declared in the symbol table for the current scope or any of its ancestors.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name is already declared, false otherwise.</returns>
    public bool IsAlreadyDeclared(string name);

    /// <summary>
    /// Checks if the given name is already declared in the symbol table for the current scope or any of its ancestors.
    /// Entries with the same name but different parameters are considered different entries.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="parameters">The parameters of the function to check.</param>
    /// <returns>True if the name is already declared, false otherwise.</returns>
    public bool IsAlreadyDeclared(string name, string[] parameters, SymbolEntryKind? kind);

    public bool IsInheritedMethod(string name, string[] parameters, string type);

    
    public IASTNode? IsValidReference(string name);

}

/// <summary>
/// Represents an entry in the symbol table.
/// </summary>
public interface ISymbolTableEntry
{
    /// <summary>
    /// The name of the entry.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The kind of the entry. Like variable, function, etc.
    /// </summary>
    public SymbolEntryKind Kind { get; set; }
    
    /// <summary>
    /// The type of the entry. Like int, float, etc. Or the return type of a function.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// The parameters of the entry. Only applicable to functions.
    /// </summary>
    public string[] Parameters { get; set; }
    
    /// <summary>
    /// The link to the symbol table for the entry.
    /// </summary>
    public ISymbolTable? Link { get; set; }

    /// <summary>
    /// The line number where the entry was declared.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// The number of references to the entry.
    /// </summary>
    public int ReferencesCount { get; set; }

    /// <summary>
    /// The visibility of the entry.
    /// </summary>
    public VisibilityType Visibility { get; set; }
}


/// <summary>
///  Interface that allows a node to be visited.
/// </summary>
public interface IVisitor
{

    /// <summary>
    /// Visits the node and performs the necessary operations to build the symbol table.
    /// </summary>
    /// <param name="currentTable">The symbol table for the current scope.</param>
    /// <param name="warnings">The list of warnings to add to.</param>
    /// <param name="errors">The list of errors to add to.</param>
    public void Visit(ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors);

    /// <summary>
    /// Performs the semantic check on the node. This method will check the node for any semantic errors and warnings.
    /// </summary>
    /// <param name="currentTable">The symbol table for the current scope.</param>
    /// <param name="warnings">The list of warnings to add to.</param>
    /// <param name="errors">The list of errors to add to.</param>
    public void SemanticCheck(ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors);

}

