
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

    /// <summary>
    /// Gets the next available temporary variable number.
    /// </summary>
    /// <returns>The next available temporary variable number.</returns>
    /// <remarks>
    /// This method will return the next available temporary variable number.
    /// </remarks>
    public string GetTempVarNumber();


    public string DeclareVariable(ISymbolTableEntry variableEntry);

    public void LoadVariable(ISymbolTableEntry variableEntry);
    public string LoadInteger(string value);

    public void NotExpr();

    public void AddExpr(string operation);

    public void MultExpr(string operation);

    public void RelExpr(string operation);

    public void Assign();

    public void If(ref int ifCount);
    public void Else(ref int ifCount);
    public void EndIf(ref int ifCount);

}