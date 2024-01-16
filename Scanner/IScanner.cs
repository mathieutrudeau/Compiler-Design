namespace Scanner;

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
}