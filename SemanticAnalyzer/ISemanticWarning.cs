namespace SemanticAnalyzer;

/// <summary>
/// Interface for a semantic warning.
/// </summary>
public interface ISemanticWarning
{
    /// <summary>
    /// The warning message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The type of the warning.
    /// </summary>
    public SemanticWarningType Type { get; }

    /// <summary>
    /// The line number where the warning occurred.
    /// </summary>
    public int Line { get; }
}

/// <summary>
/// Represents a semantic warning.
/// </summary>
public enum SemanticWarningType
{
    /// <summary>
    /// The Identifier overwrites an inherited member.
    /// </summary>
    ShadowedInheritedMember,


    OverloadedMethod,
    OverloadedFunction,
    
    /// <summary>
    /// An array is declared with an unknown size.
    /// </summary>
    UndeclaredArraySize,
}