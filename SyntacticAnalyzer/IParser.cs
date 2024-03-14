using AbstractSyntaxTreeGeneration;
using LexicalAnalyzer;

namespace SyntacticAnalyzer;

/// <summary>
/// Represents a parser.
/// </summary>
public interface IParser
{
    /// <summary>
    /// Parses the source file.
    /// </summary>
    /// <returns>True if the parse was successful, false otherwise.</returns>
    public bool Parse();

    /// <summary>
    /// Gets the abstract syntax tree root.
    /// </summary>
    /// <returns>The abstract syntax tree root.</returns>
    public IASTNode GetAST_Root();
}