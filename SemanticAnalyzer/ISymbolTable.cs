using System.Text;
using AbstractSyntaxTreeGeneration;
using CodeGenerator;

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

    /// <summary>
    /// The size of the scope.
    /// </summary>
    public int ScopeSize { get; set; }

    /// <summary>
    /// Adds an entry to the symbol table.
    /// </summary>
    public void AddEntry(ISymbolTableEntry entry);

    /// <summary>
    /// Checks if the given identifier is accessible within the current scope.
    /// </summary>
    /// <param name="identifier">The identifier to check.</param>
    /// <param name="identifierLocation">The line number where the identifier is located.</param>
    /// <param name="callScope">The symbol table for the current scope.</param>
    /// <param name="warnings">The list of warnings to add to.</param>
    /// <param name="errors">The list of errors to add to.</param>
    /// <param name="arguments">The arguments to the function or method.</param>
    /// <param name="asArguments">Whether to check if the arguments match the parameters.</param>
    /// <param name="kind">The kind of the entry to check.</param>
    /// <param name="type">The type of the entry to check.</param>
    /// <returns>True if the identifier is accessible within the current scope, false otherwise.</returns>
    /// <remarks>
    /// This method will check if the identifier is accessible within the current scope. If the identifier is not found in the current scope, it will check the parent scope and any inherited tables. If the identifier is found, it will check if the identifier is accessible based on the visibility of the identifier and the visibility of the current scope. If the identifier is a function or method, it will also check if the arguments match the parameters.
    /// </remarks>
    public bool IsAccessibleWithinScope(string identifier, int identifierLocation, ISymbolTable callScope, List<ISemanticWarning> warnings, List<ISemanticError> errors, string[] arguments, bool asArguments = true, SymbolEntryKind? kind = null, string? type = null);
 
    /// <summary>
    /// Looks up the symbol table entry with the given name, parameters, and kind. This method will search the current symbol table and all of its ancestors and inherited tables.
    /// </summary>
    /// <param name="name">The name of the entry to look up.</param>
    /// <param name="parameters">The parameters of the entry to look up. Only applicable to functions.</param>
    /// <param name="kind">The kind of the entry to look up.</param>
    /// <returns> The symbol table entry with the given name, parameters, and kind, or null if no such entry exists.</returns>
    /// <remarks>
    /// This method will search the current symbol table and all of its ancestors and inherited tables for the entry with the given name, parameters, and kind. If the entry is found, it will return the entry. If the entry is not found, it will return null.
    /// </remarks>
    public ISymbolTableEntry? Lookup(string name, string[] parameters, SymbolEntryKind? kind);

    /// <summary>
    /// Looks up the symbol table entry with the given name. This method will search the current symbol table and all of its ancestors and inherited tables.
    /// </summary>
    /// <param name="name">The name of the entry to look up.</param>
    /// <returns> The symbol table entry with the given name, or null if no such entry exists.</returns>
    public ISymbolTableEntry? Lookup(string name);


    /// <summary>
    /// Sets the offset for the symbol table entries.
    /// </summary>
    /// <param name="currentOffset">The current offset to set.</param>
    /// <remarks>
    /// This method will set the offset for all the symbol table entries in the symbol table. The offset will be used to calculate the memory location of the entry.
    /// </remarks>
    public void SetOffset(int currentOffset);

    /// <summary>
    /// Gets the size of the scope.
    /// </summary>
    /// <returns>The size of the scope.</returns>
    /// <remarks>
    /// This method will return the size of the scope. The size of the scope is the total size of all the entries in the symbol table.
    /// </remarks>
    public int GetScopeSize();


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
    /// The offset of the entry.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// The size of the entry.
    /// </summary>
    public int Size { get; set; }

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

    /// <summary>
    /// Generates the code for the node.
    /// </summary>
    /// <param name="currentTable">The symbol table for the current scope.</param>
    /// <remarks>
    /// This method will generate the code for the node. The code will be added to the code generator.
    /// </remarks>
    public void GenerateCode(ISymbolTable currentTable, IMoonCodeGenerator moonCodeGenerator,ref bool isArray);

}

