namespace SemanticAnalyzer;

/// <summary>
/// Represents a semantic error.
/// </summary>
public class SemanticError : ISemanticError
{
    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// The type of the error.
    /// </summary>
    public SemanticErrorType Type { get; set; }

    /// <summary>
    /// The line number where the error occurred.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Creates a new semantic error.
    /// </summary>
    /// <param name="type">The type of the error.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="message">The error message.</param>
    public SemanticError(SemanticErrorType type, int line, string message)
    {
        Type = type;
        Line = line;
        Message = message;
    }

    /// <summary>
    /// Returns a string representation of the error.
    /// </summary>
    public override string ToString()
    {
        return $"Error: {Type} at line {Line}: {Message}";
    }
}