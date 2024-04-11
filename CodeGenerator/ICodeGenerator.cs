
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using SemanticAnalyzer;

namespace CodeGenerator;


public interface ICodeGenerator
{
    public void GenerateCode();
}

public interface IMoonCodeGenerator
{
    /// <summary>
    /// The register pool.
    /// </summary>
    /// <value>The register pool.</value>
    /// <remarks>
    /// This stack will contain the registers that are available for use.
    /// </remarks>
    public Stack<string> RegisterPool { get; set; }

    /// <summary>
    /// The registers in use.
    /// </summary>
    /// <value>The registers in use.</value>
    /// <remarks>
    /// This stack will contain the registers that are currently in use.
    /// </remarks>
    public Stack<string> RegistersInUse { get; set; }

    


    /// <summary>
    /// Gets the next available register.
    /// </summary>
    /// <returns>The next available register.</returns>
    /// <remarks>
    /// This method will return the next available register from the register pool.
    /// </remarks>
    public string GetRegister();

    /// <summary>
    /// Frees the last used register.
    /// </summary>
    /// <param name="register">The register to free.</param>
    /// <remarks>
    /// This method will free the last used register and add it back to the register pool.
    /// </remarks>
    public void FreeRegister(string register);
    
    /// <summary>
    /// The code.
    /// </summary>
    /// <value>The code.</value>
    /// <remarks>
    /// This string builder will contain the code that is generated.
    /// </remarks>
    public StringBuilder Code { get; set; }

    /// <summary>
    /// The data.
    /// </summary>
    /// <value>The data.</value>
    /// <remarks>
    /// This string builder will contain the data that is generated.
    /// </remarks>
    public StringBuilder Data { get; set; }


    public void FunctionDeclaration(ISymbolTable currentTable);

    public void FunctionDeclarationEnd(ISymbolTable currentTable);





    
    public void AddFramePointer(ISymbolTable currentTable);
    public void RemoveFramePointer(ISymbolTable currentTable);

    /// <summary>
    /// Declares a variable.
    /// </summary>

    public void VarDeclaration(ISymbolTable currentTable, ISymbolTableEntry entry);

    public void LoadDataMember(ISymbolTable currentTable, ISymbolTableEntry entry, ref bool isArray);

    public void LoadClassIndex(ISymbolTable currentTable, ISymbolTableEntry? entry, ref bool isArray);

    public void UnloadDataMember(ISymbolTable currentTable, int offset);

    public void LoadVariableFromDataMember(ISymbolTable currentTable, ISymbolTableEntry entry, ref bool isArray);

    public void AssignDataMember(ISymbolTable currentTable, ISymbolTableEntry? entry, string type ,bool isArray=false);

    public void LoadIntegerValue(string value);

    public void LoadFloatValue(string value);

    public void FunctionCall(ISymbolTable currentTable, ISymbolTable functionTable, ref bool isArray, int? offset=null);

    public void Return(ISymbolTable currentTable, string type);

    public void Write(ISymbolTable currentTable, string type);

    public void Read(ISymbolTable currentTable, string type);

    public void AddExpression(ISymbolTable currentTable, string operation, string type);

    public void MultExpression(ISymbolTable currentTable, string operation, string type);

    public void RelExpression(ISymbolTable currentTable, string operation, string type);

    public void NotExpression(ISymbolTable currentTable, string type);

    public void NegExpression(ISymbolTable currentTable, string type);

    public void While(ISymbolTable currentTable,ref int whileCount);

    public void WhileCond(ISymbolTable currentTable,ref int whileCount);
    public void EndWhile(ISymbolTable currentTable,ref int whileCount);

    public void If(ISymbolTable currentTable,ref int ifCount);
    public void Else(ISymbolTable currentTable,ref int ifCount);
    public void EndIf(ISymbolTable currentTable,ref int ifCount);

    public void Subroutines();
}