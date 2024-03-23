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

    /// <summary>
    /// The Identifier is not declared.
    /// </summary>
    UndeclaredVariable,

    /// <summary>
    /// The Identifier is not declared.
    /// </summary>
    UndeclaredFunction,

    /// <summary>
    /// The method is not implemented.
    /// </summary>
    MethodNotImplemented,

    /// <summary>
    /// The Member is not declared.
    /// </summary>
    UndeclaredMember,

    /// <summary>
    /// The Method is not declared.
    /// </summary>
    UndeclaredMethod,

    /// <summary>
    /// The Class is not declared.
    /// </summary>
    UndeclaredClass,
    
    /// <summary>
    /// The program does not have a main function.
    /// </summary>
    MainNotFound,

    /// <summary>
    /// The main function has an invalid return type.
    /// </summary>
    MainReturnType,

    /// <summary>
    /// The main function has an invalid parameter.
    /// </summary>
    MainParameter,

    /// <summary>
    /// The array size is out of range. (i.e. negative)
    /// </summary>
    ArraySizeOutOfRange,

    /// <summary>
    /// The array size is not declared.
    /// </summary>
    UndeclaredArraySize,

    /// <summary>
    /// Chaining is done on a type that does not support chaining.
    /// </summary>
    IllegalChaining,

    /// <summary>
    /// The class is not fully implemented.
    /// </summary>
    ClassNotImplemented,

    /// <summary>
    /// The inherited class is not declared.
    /// </summary>
    InheritedClassNotFound,

    /// <summary>
    /// Multiple definitions for the same type.
    /// </summary>
    MultipleDefinition,

    /// <summary>
    /// The type is not valid in the context.
    /// </summary>
    InvalidType,

    /// <summary>
    /// The type is not declared.
    /// </summary>
    UndeclaredType,

    /// <summary>
    /// The index is invalid. (i.e. not an integer)
    InvalidIndex,

    /// <summary>
    /// A return statement is found in a void function.
    /// </summary>
    ReturnOnVoid,

    /// <summary>
    /// A return statement is missing in a non-void function.
    /// </summary>
    NotAllPathsReturn,
}