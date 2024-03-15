


using AbstractSyntaxTreeGeneration;

namespace SemanticAnalyzer;

public interface ISymbolTable
{
    public string Name { get; }
    public List<ISymbolTableEntry> Entries { get; }
    public ISymbolTable? Parent { get; }

    public bool GenerateSymbolTable(IASTNode root);

    public void AddEntry(ISymbolTableEntry entry);
}

/// <summary>
/// Represents an entry in the symbol table.
/// </summary>
public interface ISymbolTableEntry
{
    /// <summary>
    /// The name of the entry.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The kind of the entry.
    /// </summary>
    public SymbolEntryKind Kind { get; }
    
    /// <summary>
    /// The type of the entry.
    /// </summary>
    public string Type { get; }
    
    /// <summary>
    /// The link to the symbol table for the entry.
    /// </summary>
    public ISymbolTable? Link { get; }


}

public interface IVisitor
{
    public void Visit(ISymbolTable currentTable);
}