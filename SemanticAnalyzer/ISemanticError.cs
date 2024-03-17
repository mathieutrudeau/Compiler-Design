
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
    MainNotFound,
    MainReturnType,
    MainParameter,
    ArraySizeZero,
    ClassAlreadyDeclared,
    ClassNotFound,
    DuplicateDeclaration,
    MultipleDefinition,
    
}