
using AbstractSyntaxTreeGeneration;
using static System.Console;

namespace SemanticAnalyzer;

public class SymbolTable : ISymbolTable
{
    public string Name { get; set; } = "";
    public LinkedList<ISymbolTableEntry> Entries { get; set; } = new LinkedList<ISymbolTableEntry>();
    public ISymbolTable? Parent { get; set; } = null;

    public bool GenerateSymbolTable(IASTNode root)
    {
        throw new NotImplementedException();
    }

    public ISymbolTableEntry? Lookup(string name)
    {
        // Look for the entry in the current symbol table
        ISymbolTableEntry? entry = Entries.FirstOrDefault(e => e.Name == name);
        
        // If the entry is found, return it
        if (entry != null)
            return entry;
        
        // If the entry is not found, look for it in the parent symbol table
        if (Parent != null)
            return Parent.Lookup(name);

        // If the entry is not found in the current symbol table or any of its ancestors, return null
        return null;
    }

    public override string ToString()
    {
        return PrintSymbolTable(this, 0);
    }

    private string AlignString(string str)
    {
        int maxLineLength = 0;

        foreach (var line in str.Split("\n"))
        {
            if (line.Length > maxLineLength)
                maxLineLength = line.Length;
        }

        return str.PadRight(maxLineLength);
    }

    private string PrintSymbolTable(ISymbolTable table, int depth)
    {
        
        int depth_magnitude = 4;
        int indent = depth_magnitude * depth;

        string start_str = "";
        for (int i = 0; i < depth; i++)
            start_str += "|" + new string(' ', depth_magnitude);
        

        if (indent == 0)
            indent = -1;

        string tableName = start_str
        + "| Symbol Table: " + table.Name + "    | Parent: " + (table.Parent == null ? "None" : table.Parent.Name);
        
        string tableContents = "";

        if (table.Entries.Count != 0)
        {
            int maxNameLength = table.Entries.Max(e => e.Name.Length) + 8;
            int maxKindLength = table.Entries.Max(e => e.Kind.ToString().Length) + 9;
            int maxTypeLength = table.Entries.Max(e => e.Type.Length) + 9;
            int maxLinkLength = table.Entries.Max(e => e.Link == null ? 4 : e.Link.Name.Length) + 9;
            if (maxLinkLength < 13)
                maxLinkLength = 13;

            foreach (var entry in table.Entries)
            {
                tableContents += start_str + string.Format("| Name: {0}", entry.Name).PadRight(maxNameLength);
                tableContents += string.Format(" | Kind: {0}", entry.Kind).PadRight(maxKindLength);
                tableContents += string.Format(" | Type: {0}", entry.Type).PadRight(maxTypeLength);
                tableContents += string.Format(" | Link: {0}", entry.Link == null ? "None" : entry.Link.Name).PadRight(maxLinkLength);
                tableContents += "\n";


                if (entry.Link != null)
                    tableContents += PrintSymbolTable(entry.Link, depth + 1);
            }

            tableContents = tableContents.TrimEnd('\n');
        }
        
        // Find the line with the most characters
        int maxLineLength = tableName.Length;

        foreach (var line in tableContents.Split("\n"))
        {
            if (line.Length > maxLineLength)
                maxLineLength = line.Length;
        }

        
        tableName = tableName.PadRight(maxLineLength+1) + "|" + "\n";
        //tableName =
        tableName = start_str + new string('=', maxLineLength+1-indent) + "\n" + tableName + start_str + new string('=', maxLineLength+1-indent) + "\n";

        // Append spaces to each line to make them the same length and add the | at the end
        tableContents = string.Join("\n", tableContents.Split("\n").Select(s=>s.PadRight(maxLineLength+1) + "|").Where(s=>s.Length > 1))+ "\n";

        tableContents += start_str + new string('=', maxLineLength-indent) + "|\n";

        string str = tableName + tableContents;

        return str;
    }

    public bool IsAlreadyDeclared(string name)
    {
        // Check if the name is already declared in the current symbol table
        if (Entries.Any(e => e.Name == name))
            return true;

        // If the name is not declared in the current symbol table, check if it is declared in the parent symbol table
        if (Parent != null)
            return Parent.IsAlreadyDeclared(name);

        // If the name is not declared in the current symbol table or any of its ancestors, return false
        return false;
    }

    public bool IsAlreadyDeclared(string name, string[] parameters, SymbolEntryKind? kind = null)
    {
        // Check if the name is already declared in the current symbol table
        if ((kind ==null && Entries.Any(e => e.Name == name && e.Parameters.SequenceEqual(parameters)))
        || (kind != null && Entries.Any(e => e.Name == name && e.Parameters.SequenceEqual(parameters) && e.Kind == kind)))
            return true;

        // If the name is not declared in the current symbol table, check if it is declared in the parent symbol table
        if (Parent != null)
            return Parent.IsAlreadyDeclared(name, parameters, kind);

        // If the name is not declared in the current symbol table or any of its ancestors, return false
        return false;
    }

    public void AddEntry(ISymbolTableEntry entry)
    {
        Entries.AddLast(entry);
    }

}

/// <summary>
/// Represents an entry in the symbol table.
/// </summary>
public class SymbolTableEntry : ISymbolTableEntry
{
    public string Name { get; set; } = "";
    public SymbolEntryKind Kind { get; set; }
    public string Type { get; set; } = "";
    public ISymbolTable? Link { get; set; }
    public int Line { get; set; }
    public VisibilityType Visibility { get; set; }
    public string[] Parameters { get; set; } = Array.Empty<string>();
    public int ReferencesCount { get; set; }
}


public enum VisibilityType
{
    Public,
    Private
}

public enum SymbolEntryKind
{
    Variable,
    Function,
    Parameter,
    ClassDeclaration,
    Class,
    Data,
    MethodDeclaration,
    Method,
    Inherit
}