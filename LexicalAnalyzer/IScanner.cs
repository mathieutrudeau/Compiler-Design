namespace LexicalAnalyzer;

/// <summary>
/// Represents a scanner that reads source code and returns tokens.
/// </summary>
public interface IScanner
{
    /// <summary>
    /// Returns the next token in the source code.
    /// </summary>
    /// <returns>The next token in the source code.</returns>
    public Token NextToken();

    /// <summary>
    /// Shows the contents of the buffer.
    /// </summary>
    public void PrintBuffer();

    /// <summary>
    /// True if there are more tokens in the buffer. False otherwise.
    /// </summary>
    public bool HasTokenLeft();
}