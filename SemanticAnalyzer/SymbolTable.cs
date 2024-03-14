
using AbstractSyntaxTreeGeneration;

namespace SemanticAnalyzer;

public class SymbolTable : ISymbolTable
{
    public string Name { get; }
    public List<ISymbolTableEntry> Entries { get; }
    public ISymbolTable? Parent { get; }

    public SymbolTable(string name, ISymbolTable? parent)
    {
        Name = name;
        Parent = parent;
        Entries = new List<ISymbolTableEntry>();
    }

    public bool GenerateSymbolTable(IASTNode root)
    {
        throw new NotImplementedException();
    }
}

public class SymbolTableEntry : ISymbolTableEntry
{
    public string Name { get; }
    public SymbolEntryKind Kind { get; }
    public string Type { get; }
    public int Offset { get; }
    public ISymbolTable? Link { get; }

    public SymbolTableEntry(string name, SymbolEntryKind kind, string type, int offset, ISymbolTable? link)
    {
        Name = name;
        Kind = kind;
        Type = type;
        Offset = offset;
        Link = link;
    }
}






public enum SymbolEntryKind
{
    Variable,
    Function,
    Parameter
}