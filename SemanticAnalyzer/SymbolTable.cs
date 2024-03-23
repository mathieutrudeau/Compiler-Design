using AbstractSyntaxTreeGeneration;
using static System.Console;

namespace SemanticAnalyzer;

/// <summary>
/// Represents a Symbol Table.
/// </summary>
public class SymbolTable : ISymbolTable
{
    /// <summary>
    /// The name of the symbol table.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The list of entries in the symbol table.
    /// </summary>
    public LinkedList<ISymbolTableEntry> Entries { get; set; } = new LinkedList<ISymbolTableEntry>();

    /// <summary>
    /// The parent symbol table. This will be null for the global symbol table.
    /// </summary>
    public ISymbolTable? Parent { get; set; } = null;

    #region Public Methods

    public bool IsAccessibleWithinScope(string identifier, int identifierLocation, ISymbolTable callScope, List<ISemanticWarning> warnings, List<ISemanticError> errors , string[] arguments, SymbolEntryKind? kind = null, string? type = null)
    {
        // Copy the reference to the current symbol table
        ISymbolTable? currentTable = this;

        // Recursively check if the identifier is accessible within the current scope
        return IsAccessibleWithinScope(identifier, identifierLocation, currentTable, callScope, warnings, errors, arguments, kind,type);
    }

    private bool IsAccessibleWithinScope(string identifier, int identifierLocation,  ISymbolTable currentTable ,ISymbolTable callScope, List<ISemanticWarning> warnings, List<ISemanticError> errors, string[] arguments, SymbolEntryKind? kind = null, string? type = null)
    {
        // Look for the entry in the current symbol table
        ISymbolTableEntry? entry = currentTable.Entries.FirstOrDefault(e => e.Name == identifier && (kind == null || e.Kind == kind) && MatchFunctionParameters(e.Parameters, arguments) && (type == null || e.Type == type));

        // If the entry is found, check if it is accessible from the call scope
        if (entry != null)
        {
            switch (entry.Kind)
            {
                case SymbolEntryKind.Variable:
                    // Variables have no restrictions on their visibility
                    return true;
                case SymbolEntryKind.Function:
                    // Free Functions can be accessed from any scope
                    return true;
                case SymbolEntryKind.Parameter:
                    // Parameters can be accessed from the function they are declared in
                    return currentTable == callScope;
                case SymbolEntryKind.Data:
                case SymbolEntryKind.Method:
                    // Data and Methods can be accessed from the class they are declared in
                    if (entry.Visibility == VisibilityType.Public)
                        return true;
                    // If the data/method is private, check if it is being accessed from the class it is declared in
                    else if (currentTable != callScope)
                    {
                        errors.Add(new SemanticError(SemanticErrorType.UndeclaredMember, identifierLocation, $"Member '{identifier}' is visible only within the class it is declared in."));
                        return false;
                    }
                    else
                        return currentTable == callScope;
                default:
                    if(entry.Visibility == VisibilityType.Public)
                        return true;
                    else if (currentTable != callScope)
                    {
                        errors.Add(new SemanticError(SemanticErrorType.UndeclaredMember, identifierLocation, $"Member '{identifier}' is visible only within the class it is declared in."));
                        return false;
                    }
                    else
                        return currentTable == callScope;
            }
        }

        // If the entry is not found in the current symbol table, check if its declared in any inherited symbol tables
        foreach (var inheritEntry in currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.Inherit))
        {
            if (inheritEntry.Link != null)
            {
                // Check if the entry is accessible from the inherited symbol table
                if (IsAccessibleWithinScope(identifier, identifierLocation, inheritEntry.Link, callScope, warnings, errors, arguments, kind,type))
                    return true;
            }
        }

        // If the entry as yet not been found, check if it is declared in the parent symbol table
        if (currentTable.Parent != null)
            return IsAccessibleWithinScope(identifier, identifierLocation, currentTable.Parent, callScope, warnings, errors, arguments, kind, type);

        
        // If the entry is not found in the current symbol table or any of its ancestors, return false
        return false;
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

    public bool IsInheritedMethod(string name, string[] parameters, string type)
    {
        // Check if the name is already declared in the current symbol table
        if (Entries.Any(e => e.Name == name && MatchFunctionParameters(e.Parameters, parameters) && e.Visibility == VisibilityType.Public && e.Type == type && e.Kind == SymbolEntryKind.Method || e.Kind == SymbolEntryKind.MethodDeclaration))
            return true;

        // If the name is not declared in the current symbol table, check if it is declared in one of the inherited symbol tables
        foreach (var entry in Entries.Where(e => e.Kind == SymbolEntryKind.Inherit))
            if (entry.Link != null)
                if (entry.Link.IsInheritedMethod(name, parameters, type))
                    return true;

        return false;
    }

    public IASTNode? IsValidReference(string name)
    {
        // Copy the reference to the current symbol table
        ISymbolTable? currentTable = this;

        // Look for the entry in the current symbol table
        ISymbolTableEntry? entry = null;

        while (entry == null && currentTable != null)
        {
            // Check whether the current symbol table is for a class or for a function
            if (currentTable.Parent!.Entries.First(e => e.Name == currentTable.Name).Kind == SymbolEntryKind.Class)
            {
                // Class
            }
            else
            {
                // Function or method
            }

            entry = currentTable.Entries.FirstOrDefault(e => e.Name == name);

            currentTable = currentTable.Parent!;
        }



        return null;
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
        if ((kind == null && Entries.Any(e => e.Name == name && MatchFunctionParameters(e.Parameters, parameters)))
        || (kind != null && Entries.Any(e => e.Name == name && MatchFunctionParameters(e.Parameters, parameters) && e.Kind == kind)))
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

    #endregion Public Methods

    #region Private Methods

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


        tableName = tableName.PadRight(maxLineLength + 1) + "|" + "\n";
        //tableName =
        tableName = start_str + new string('=', maxLineLength + 1 - indent) + "\n" + tableName + start_str + new string('=', maxLineLength + 1 - indent) + "\n";

        // Append spaces to each line to make them the same length and add the | at the end
        tableContents = string.Join("\n", tableContents.Split("\n").Select(s => s.PadRight(maxLineLength + 1) + "|").Where(s => s.Length > 1)) + "\n";

        tableContents += start_str + new string('=', maxLineLength - indent) + "|\n";

        string str = tableName + tableContents;

        return str;
    }

    #endregion Private Methods

    #region Static Methods

    /// <summary>
    /// Checks if the parameters of a function match the arguments.
    /// </summary>
    /// <param name="parameters">The parameters of the function.</param>
    /// <param name="arguments">The arguments to check.</param>
    /// <returns>True if the parameters match the arguments, false otherwise.</returns>
    private static bool MatchFunctionParameters(string[] parameters, string[] arguments)
    {
        // Check if the number of arguments and parameters match
        if (arguments.Length != parameters.Length)
            return false;

        // Check if the arguments match the parameters
        for (int i = 0; i < arguments.Length; i++)
        {
            // If the argument is the same as the parameter, continue
            if (arguments[i] == parameters[i])
                continue;

            // In case of arrays, check if the dimensions match
            string[] argDims = arguments[i].Split('[');
            string[] paramDims = parameters[i].Split('[');

            if (argDims.Length != paramDims.Length)
                return false;

            // If the dimensions match, make sure the sizes match
            for (int j = 0; j < argDims.Length; j++)
            {
                // If the parameter size is empty, then it will match any size
                if (paramDims[j].Split(']')[0] == "")
                    continue;
                // If the parameter size is not empty, then the sizes must match
                else if (argDims[j].Split(']')[0] != paramDims[j].Split(']')[0])
                    return false;
            }
        }

        return true;
    }

    #endregion Static Methods

    #region Overridden Methods

    public override string ToString()
    {
        return PrintSymbolTable(this, 0);
    }

    #endregion Overridden Methods

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