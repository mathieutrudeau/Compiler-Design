namespace SemanticAnalyzer;

public class SemanticWarning : ISemanticWarning
{
    public string Message { get; set; } = "";
    
    public SemanticWarningType Type { get; set; }

    public int Line { get; set; }

    public SemanticWarning(SemanticWarningType type, int line, string message)
    {
        Type = type;
        Line = line;
        Message = message;
    }

    public override string ToString()
    {
        return $"Warning: {Type} at line {Line}: {Message}";
    }
}