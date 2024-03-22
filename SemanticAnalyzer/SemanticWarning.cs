namespace SemanticAnalyzer;

/// <summary>
/// Represents a semantic warning.
/// </summary>
public class SemanticWarning : ISemanticWarning
{
    /// <summary>
    /// The warning message.
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// The type of the warning.
    /// </summary>
    public SemanticWarningType Type { get; set; }

    /// <summary>
    /// The line number where the warning occurred.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Creates a new semantic warning.
    /// </summary>
    /// <param name="type">The type of the warning.</param>
    /// <param name="line">The line number where the warning occurred.</param>
    /// <param name="message">The warning message.</param>
    public SemanticWarning(SemanticWarningType type, int line, string message)
    {
        Type = type;
        Line = line;
        Message = message;
    }

    /// <summary>
    /// Returns a string representation of the warning.
    /// </summary>
    public override string ToString()
    {
        return $"Warning: {Type} at line {Line}: {Message}";
    }
}