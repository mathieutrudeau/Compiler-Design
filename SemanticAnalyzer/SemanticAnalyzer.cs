using System.ComponentModel;
using AbstractSyntaxTreeGeneration;
using static System.Console;

namespace SemanticAnalyzer;

/// <summary>
/// Represents the implementation of a semantic analyzer.
/// </summary>
public class SemanticAnalyzer : ISemanticAnalyzer
{
    /// <summary>
    /// The root of the AST to analyze.
    /// </summary>
    private readonly IASTNode ASTRoot;

    /// <summary>
    /// The global symbol table.
    /// </summary>
    private readonly ISymbolTable GlobalSymbolTable;

    /// <summary>
    /// The list of warnings generated during the analysis.
    /// </summary>
    private readonly List<ISemanticWarning> Warnings;

    /// <summary>
    /// The list of errors generated during the analysis.
    /// </summary>
    private readonly List<ISemanticError> Errors;

    /// <summary>
    /// The extension for the file containing the analysis warnings and errors.
    /// </summary>
    private const string ANALYSIS_ERRORS_EXTENSION = ".outsemanticerrors";

    /// <summary>
    /// The extension for the file containing the symbol tables generated during the analysis.
    /// </summary>
    private const string SEMANTIC_SYMBOL_TABLE_EXTENSION = ".outsymboltables";

    /// <summary>
    /// The name of the source file being analyzed.
    /// </summary>
    private string SourceName { get; set; } = "";

    /// <summary>
    /// Constructs a new SemanticAnalyzer with the given AST root.
    /// </summary>
    /// <param name="astNode">The root of the AST to analyze.</param>
    public SemanticAnalyzer(IASTNode astNode, string sourceName)
    {
        // Initialize the SemanticAnalyzer with the given AST root
        ASTRoot = astNode;

        // Set the name of the source file being analyzed
        SourceName = sourceName.Replace(".src", "");

        // Initialize the global symbol table
        GlobalSymbolTable = new SymbolTable()
        {
            Name = "Global",
            Entries = new LinkedList<ISymbolTableEntry>(),
            Parent = null
        };

        // Initialize the lists of warnings and errors
        Warnings = new List<ISemanticWarning>();
        Errors = new List<ISemanticError>();

        // Remove any existing files containing the analysis warnings and errors
        if (File.Exists(SourceName + ANALYSIS_ERRORS_EXTENSION))
            File.Delete(SourceName + ANALYSIS_ERRORS_EXTENSION);
        // Remove any existing files containing the symbol tables generated during the analysis
        if (File.Exists(SourceName + SEMANTIC_SYMBOL_TABLE_EXTENSION))
            File.Delete(SourceName + SEMANTIC_SYMBOL_TABLE_EXTENSION);
    }

    /// <summary>
    /// Shows all the errors generated during the analysis. 
    /// </summary>
    /// <param name="showConsole">True if the errors should be printed to the console, false otherwise.</param>
    /// <remarks>
    /// If the showConsole parameter is set to false, the errors will only be written to the file.
    /// </remarks>
    private void ShowErrors(bool showConsole=true)
    {
        if (Errors.Count > 0)
        {
            ForegroundColor = ConsoleColor.Red;
            
            // Open the file to write the errors
            using var file = new StreamWriter(SourceName + ANALYSIS_ERRORS_EXTENSION, append: true);
            
            // If there are any errors, print them to the console and the file in the order in they are located in the source code
            foreach (var error in Errors.DistinctBy(e=>e.ToString()).OrderBy(e => e.Line))
            {
                if (showConsole)
                    WriteLine(error);
                file.WriteLine(error);
            }

            // Close the file
            file.Close();

            ResetColor();
        }
    }

    /// <summary>
    /// Shows all the warnings generated during the analysis.
    /// </summary>
    /// <param name="showConsole">True if the warnings should be printed to the console, false otherwise.</param>
    /// <remarks>
    /// If the showConsole parameter is set to false, the warnings will only be written to the file.
    /// </remarks>
    private void ShowWarnings(bool showConsole=true)
    {
        if (Warnings.Count > 0)
        {
            ForegroundColor = ConsoleColor.Yellow;
            
            // Open the file to write the warnings
            using var file = new StreamWriter(SourceName + ANALYSIS_ERRORS_EXTENSION, append: true);
            
            // If there are any warnings, print them
            foreach (var warning in Warnings.DistinctBy(w=>w.ToString()).OrderBy(w => w.Line))
            {
                if (showConsole)
                    WriteLine(warning);

                file.WriteLine(warning);
            }

            // Close the file
            file.Close();

            ResetColor();
        }
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
        ASTRoot.Visit(GlobalSymbolTable, Warnings, Errors);

        //Perform the semantic analysis once the symbol table has been built
        ASTRoot.SemanticCheck(GlobalSymbolTable, Warnings, Errors);

        // Show the warnings and errors generated during the analysis
        ShowWarnings();
        ShowErrors();

        // Print the symbol tables generated during the analysis to the file
        using var file = new StreamWriter(SourceName + SEMANTIC_SYMBOL_TABLE_EXTENSION);
        file.WriteLine(GlobalSymbolTable);
        file.Close();

        return Errors.Count == 0;
    }

    /// <summary>
    /// Gets the global symbol table.
    /// </summary>
    /// <returns>The global symbol table.</returns>
    /// <remarks>
    /// This method should only be called after a successful call to the Analyze method.
    /// </remarks>
    public ISymbolTable GetGlobalSymbolTable()
    {
        return GlobalSymbolTable;
    }
}