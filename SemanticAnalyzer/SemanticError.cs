namespace SemanticAnalyzer;

public class SemanticError : ISemanticError
{
    public string Message { get; set; } = "";
    
    public SemanticErrorType Type { get; set; }

    public int Line { get; set; }

    public SemanticError(SemanticErrorType type, int line, string message)
    {
        Type = type;
        Line = line;
        Message = message;
    }

    public override string ToString()
    {
        return $"Error: {Type} at line {Line}: {Message}";
    }
}