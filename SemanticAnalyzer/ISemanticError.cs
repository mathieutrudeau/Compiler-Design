namespace SemanticAnalyzer;

/// <summary>
/// Interface for a semantic error.
/// </summary>
public interface ISemanticError
{
    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The type of the error.
    /// </summary>
    public SemanticErrorType Type { get; }
    
    /// <summary>
    /// The line number where the error occurred.
    /// </summary>
    public int Line { get; }
}

/// <summary>
/// Represents a semantic error.
/// </summary>
public enum SemanticErrorType
{
    /// <summary>
    /// The Identifier is declared multiple times.
    /// </summary>
    MultipleDeclaration,
        
    UndefinedMethod,
    MethodNotImplemented,
    UndeclaredMember,
    UndeclaredMethod,
    UndeclaredClass,
    MainNotFound,
    MainReturnType,
    MainParameter,
    ArraySizeOutOfRange,
    UndeclaredArraySize,
    IllegalChaining,
    ClassAlreadyDeclared,
    ClassNotFound,
    ClassNotImplemented,
    InheritedClassNotFound,
    MultipleDefinition,
    InvalidType,
    UndeclaredType,
    InvalidIndex,
    ReturnOnVoid,
    NotAllPathsReturn,
}