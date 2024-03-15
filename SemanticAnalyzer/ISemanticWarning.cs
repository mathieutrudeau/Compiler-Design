

namespace SemanticAnalyzer;

public interface ISemanticWarning
{
    public string Message { get; }

    public SemanticWarningType Type { get; }

    public int Line { get; }
}

public enum SemanticWarningType
{
    ShadowedInheritedMember,
    OverloadedMethodOrFunction,
    ArraySizeNotSpecified
}