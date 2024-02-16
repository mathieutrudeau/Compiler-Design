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
}