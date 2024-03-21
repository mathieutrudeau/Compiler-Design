
namespace SemanticAnalyzer;

public interface ISemanticError
{
    public string Message { get; }

    public SemanticErrorType Type { get; }
    public int Line { get; }
}

public enum SemanticErrorType
{
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