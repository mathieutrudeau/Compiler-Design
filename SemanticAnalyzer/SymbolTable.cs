using System.Security.Cryptography.X509Certificates;
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


    /// <summary>
    /// The size of the scope.
    /// </summary>
    public int ScopeSize { get; set; } = 0;

    #region Public Methods

    public int GetScopeSize()
    {
        int size = 0;
        foreach (var entry in Entries)
        {
            if (entry.Link != null)
                size += entry.Link!.GetScopeSize();
            else
                size += entry.Size;
        }

        return size;
    }

    public void SetOffset(int offset)
    {
        foreach (var entry in Entries)
        {
            if (entry.Link != null)
            {
                entry.Link!.SetOffset(offset);
                entry.Link!.ScopeSize = entry.Link!.GetScopeSize();
                entry.Size = entry.Link!.ScopeSize;
                offset -= entry.Link!.ScopeSize;
            }
            else
            {
                entry.Offset = offset;
                offset -= entry.Size;
            }
        }
    }

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
    public bool IsAccessibleWithinScope(string identifier, int identifierLocation, ISymbolTable callScope, List<ISemanticWarning> warnings, List<ISemanticError> errors , string[] arguments, bool asArguments = true, SymbolEntryKind? kind = null, string? type = null)
    {
        // Copy the reference to the current symbol table
        ISymbolTable? currentTable = this;

        // Recursively check if the identifier is accessible within the current scope
        return IsAccessibleWithinScope(identifier, identifierLocation, currentTable, callScope, warnings, errors, arguments,asArguments, kind,type);
    }

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
    public ISymbolTableEntry? Lookup(string name, string[] parameters, SymbolEntryKind? kind)
    {
        // Look for the entry in the current symbol table
        ISymbolTableEntry? entry = Entries.FirstOrDefault(e => e.Name == name && MatchFunctionParameters(e.Parameters, parameters) && (kind == null || e.Kind == kind));

        // If the entry is found, return it
        if (entry != null)
            return entry;

        // If the entry is not found, look for it in any inherited symbol tables
        foreach (var inheritEntry in Entries.Where(e => e.Kind == SymbolEntryKind.Inherit))
        {
            if (inheritEntry.Link != null)
            {
                ISymbolTableEntry? inheritedEntry = inheritEntry.Link.Lookup(name, parameters, kind);
                if (inheritedEntry != null)
                    return inheritedEntry;
            }
        }

        // If the entry is not found in the inherited symbol tables, look for it in the parent symbol table
        if (Parent != null)
            return Parent.Lookup(name, parameters, kind);

        // If the entry is not found in the current symbol table or any of its ancestors, return null
        return null;
    }

    /// <summary>
    /// Looks up the symbol table entry with the given name. This method will search the current symbol table and all of its ancestors and inherited tables.
    /// </summary>
    /// <param name="name">The name of the entry to look up.</param>
    /// <returns> The symbol table entry with the given name, or null if no such entry exists.</returns>
    public ISymbolTableEntry? Lookup(string name)
    {
        // Look for the entry in the current symbol table
        ISymbolTableEntry? entry = Entries.FirstOrDefault(e => e.Name == name);

        // If the entry is found, return it
        if (entry != null)
            return entry;

        // If the entry is not found, look for it in any inherited symbol tables
        foreach (var inheritEntry in Entries.Where(e => e.Kind == SymbolEntryKind.Inherit))
        {
            if (inheritEntry.Link != null)
            {
                ISymbolTableEntry? inheritedEntry = inheritEntry.Link.Lookup(name);
                if (inheritedEntry != null)
                    return inheritedEntry;
            }
        }

        // If the entry is not found in the inherited symbol tables, look for it in the parent symbol table
        if (Parent != null)
            return Parent.Lookup(name);

        // If the entry is not found in the current symbol table or any of its ancestors, return null
        return null;
    }

    /// <summary>
    /// Adds an entry to the symbol table.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void AddEntry(ISymbolTableEntry entry)
    {
        // Set the size of the entry based on its kind
        switch(entry.Kind)
        {
            case SymbolEntryKind.Data:
            case SymbolEntryKind.Variable:
                var dims = entry.Type.Split('[');   
                if(dims[0]=="float")
                    entry.Size = 8;
                else
                    entry.Size = 4;
            
                if(dims.Length > 1)
                    for(int i = 1; i < dims.Length; i++)
                        entry.Size *= int.Parse(dims[i].Split(']')[0]);

                if(entry.Kind == SymbolEntryKind.Data)
                    Parent!.Entries.Where(e => e.Name == Name).First().Size += entry.Size;
            
                break;

            case SymbolEntryKind.Parameter:
                entry.Size = 4;
            
                if(entry.Type.Contains("float"))
                    entry.Size = 8;
                break;
        }

        Entries.AddLast(entry);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Checks if the given identifier is accessible within the current scope.
    /// </summary>
    /// <param name="identifier">The identifier to check.</param>
    /// <param name="identifierLocation">The line number where the identifier is located.</param>
    /// <param name="currentTable">The current symbol table.</param>
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
    private bool IsAccessibleWithinScope(string identifier, int identifierLocation,  ISymbolTable currentTable ,ISymbolTable callScope, List<ISemanticWarning> warnings, List<ISemanticError> errors, string[] arguments, bool asArguments, SymbolEntryKind? kind = null, string? type = null)
    {
        // Look for the entry in the current symbol table
        ISymbolTableEntry? entry = currentTable.Entries.FirstOrDefault(e => e.Name == identifier && (kind == null || e.Kind == kind) && (asArguments==false || MatchFunctionParameters(e.Parameters, arguments)) && (type == null || e.Type == type));

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
                    else if (currentTable != callScope && currentTable != callScope.Parent)
                    {
                        errors.Add(new SemanticError(SemanticErrorType.UndeclaredMember, identifierLocation, $"Member '{identifier}' is visible only within the class it is declared in."));
                        return false;
                    }
                    // If the data/method is private and being accessed from the class it is declared in, return true
                    else
                        return currentTable == callScope || currentTable == callScope.Parent;
                default:
                    if(entry.Visibility == VisibilityType.Public)
                        return true;
                    else if (currentTable != callScope && currentTable != callScope.Parent)
                    {
                        errors.Add(new SemanticError(SemanticErrorType.UndeclaredMember, identifierLocation, $"Member '{identifier}' is visible only within the class it is declared in."));
                        return false;
                    }
                    // If the data/method is private and being accessed from the class it is declared in, return true
                    else
                        return currentTable == callScope || currentTable == callScope.Parent;
            }
        }

        // If the entry is not found in the current symbol table, check if its declared in any inherited symbol tables
        foreach (var inheritEntry in currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.Inherit))
        {
            if (inheritEntry.Link != null)
            {
                // Check if the entry is accessible from the inherited symbol table
                if (IsAccessibleWithinScope(identifier, identifierLocation, inheritEntry.Link, callScope, warnings, errors, arguments, asArguments, kind, type))
                {
                    // If the entry is accessible from the inherited symbol table, return true and add a warning
                    if (kind == SymbolEntryKind.Method || kind == SymbolEntryKind.MethodDeclaration)
                    {
                        string parameterString = string.Join(", ", arguments);
                        warnings.Add(new SemanticWarning(SemanticWarningType.ShadowedInheritedMember, identifierLocation, $"Member method '{identifier}({parameterString})' is inherited from class '{inheritEntry.Link.Name}'."));
                    }
                    else if(kind == SymbolEntryKind.Data)
                        warnings.Add(new SemanticWarning(SemanticWarningType.ShadowedInheritedMember, identifierLocation, $"Member data '{identifier}' is inherited from class '{inheritEntry.Link.Name}'."));
                    return true;
                }
            }
        }

        // If the entry as yet not been found, check if it is declared in the parent symbol table
        if (currentTable.Parent != null)
            return IsAccessibleWithinScope(identifier, identifierLocation, currentTable.Parent, callScope, warnings, errors, arguments, asArguments, kind, type);

        
        // If the entry is not found in the current symbol table or any of its ancestors, return false
        return false;
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
        + "| Symbol Table: " + table.Name + "    | Parent: " + (table.Parent == null ? "None" : table.Parent.Name + "    | Scope Size: " + table.ScopeSize);

        string tableContents = "";

        if (table.Entries.Count != 0)
        {
            int maxNameLength = table.Entries.Max(e => e.Name.Length) + 8;
            int maxKindLength = table.Entries.Max(e => e.Kind.ToString().Length) + 9;
            int maxTypeLength = table.Entries.Max(e => e.Type.Length) + 9;
            int maxVisibilityLength = table.Entries.Max(e => e.Visibility.ToString().Length) + 9;
            int maxLinkLength = table.Entries.Max(e => e.Link == null ? 4 : e.Link.Name.Length) + 9;
            int maxReferencesCount = table.Entries.Max(e => e.ReferencesCount.ToString().Length) + 9;
            int maxSizeLength = table.Entries.Max(e => e.Size.ToString().Length) + 9;
            int maxOffsetLength = table.Entries.Max(e => e.Offset.ToString().Length) + 9;
            if (maxLinkLength < 13)
                maxLinkLength = 13;

            foreach (var entry in table.Entries)
            {
                tableContents += start_str + string.Format("| Name: {0}", entry.Name).PadRight(maxNameLength);
                tableContents += string.Format(" | Kind: {0}", entry.Kind).PadRight(maxKindLength);
                tableContents += string.Format(" | Type: {0}", entry.Type).PadRight(maxTypeLength);
                tableContents += string.Format(" | Visibility: {0}", entry.Visibility).PadRight(maxVisibilityLength);
                tableContents += string.Format(" | Link: {0}", entry.Link == null ? "None" : entry.Link.Name).PadRight(maxLinkLength);
                tableContents += string.Format(" | References: {0}", entry.ReferencesCount).PadRight(maxReferencesCount);
                tableContents += string.Format(" | Size: {0}", entry.Size).PadRight(maxSizeLength);
                tableContents += string.Format(" | Offset: {0}", entry.Offset).PadRight(maxOffsetLength);
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
            if (arguments[i] == parameters[i] || arguments[i].Replace("integer","float") == parameters[i])
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
    public int ReferencesCount { get; set; } = 0;
    public int Offset { get; set; } = 0;
    public int Size { get; set; } = 0;
}


public enum VisibilityType
{
    Public,
    Private
}

public enum SymbolEntryKind
{
    Variable,

    /// <summary>
    /// Represents a temporary variable. These are variables that are created during the compilation process and are not part of the source code.
    /// </summary>
    TempVar,

    /// <summary>
    /// Represents the address from which the function/method was called.
    /// </summary>
    JumpAddress,

    /// <summary>
    /// Represents the return value of a function/method.
    /// </summary>
    ReturnVal,

    Function,
    Parameter,
    ClassDeclaration,
    Class,
    Data,
    MethodDeclaration,
    Method,
    Inherit
}