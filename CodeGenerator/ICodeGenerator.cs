
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
    /// The temporary variables in use.
    /// </summary>
    /// <value>The temporary variables.</value>
    /// <remarks>
    /// This stack will contain the temporary variables that are currently in use.
    /// </remarks>
    public Stack<ISymbolTableEntry> TempVarsInUse { get; set; }

    /// <summary>
    /// The temporary variables.
    /// </summary>
    /// <value>The temporary variables.</value>
    /// <remarks>
    /// This stack will contain the temporary variables that are available for use.
    /// </remarks>
    public Stack<ISymbolTableEntry> TempVars { get; set; }


    public Stack<List<string>> FrameEscapes { get; set; }


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

    /// <summary>
    /// Gets the next available temporary variable number.
    /// </summary>
    /// <returns>The next available temporary variable number.</returns>
    /// <remarks>
    /// This method will return the next available temporary variable number.
    /// </remarks>
    public string GetTempVarNumber();

    public void DeclareVariable(ISymbolTableEntry variableEntry);

    public void LoadVariable(string type,ISymbolTableEntry variableEntry, ISymbolTable table);

    public void LoadInteger(string value);

    public void LoadFloat(string value);

    public void NotExpr();

    public void AddExpr(string operation, ISymbolTable currentTable, bool isFloat);

    public void MultExpr(string operation, ISymbolTable currentTable, bool isFloat);

    public void RelExpr(string operation, ISymbolTable currentTable, bool isFloat);

    public void AssignFloat();
    public void AssignInteger();

    public void If(ref int ifCount);
    public void Else(ref int ifCount);
    public void EndIf(ref int ifCount);

    public void While(ref int whileCount);

    public void WhileCond(ref int whileCount);
    public void EndWhile(ref int whileCount);


    /// <summary>
    /// Outputs the code to the console.
    /// </summary>
    /// <param name="currentTable">The current symbol table.</param>
    /// <remarks>
    /// This method will output the code to the console.
    /// </remarks>
    public void WriteInteger(ISymbolTable currentTable);

    /// <summary>
    /// Reads input from the console.
    /// </summary>
    /// <param name="currentTable">The current symbol table.</param>
    /// <remarks>
    /// This method will read input from the console.
    /// </remarks>
    public void ReadInteger(ISymbolTable currentTable);


    public void WriteFloat(ISymbolTable currentTable);

    public void ReadFloat(ISymbolTable currentTable);


    public void FunctionDeclaration(ISymbolTable currentTable);

    public void FunctionDeclarationEnd(ISymbolTable currentTable);

    public void Return(ISymbolTable currentTable);
    public void CallFunction(ISymbolTable currentTable, ISymbolTable functionTable);



    public void ClassDeclaration(ISymbolTable currentTable);
    public void ClassDeclarationEnd(ISymbolTable currentTable);


    public void ClassVariable(ISymbolTable currentTable,  ISymbolTableEntry entry);
    public void ClassVariableEnd(ISymbolTable currentTable, ISymbolTable classTable);
}