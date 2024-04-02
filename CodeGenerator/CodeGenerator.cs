using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using AbstractSyntaxTreeGeneration;
using SemanticAnalyzer;
using static System.Console;

namespace CodeGenerator;

public class CodeGenerator : ICodeGenerator
{

    private string SourceFileName { get; set; }

    private IASTNode Root { get; set; }

    private ISymbolTable SymbolTable { get; set; }

    private IMoonCodeGenerator MoonCodeGenerator { get; set; }

    public CodeGenerator(IASTNode root, ISymbolTable symbolTable, string sourceFileName)
    {
        this.Root = root;
        this.SymbolTable = symbolTable;
        SourceFileName = sourceFileName;
        MoonCodeGenerator = new MoonCodeGenerator();
    }



    public void GenerateCode()
    {
        Root.GenerateCode(SymbolTable, MoonCodeGenerator);

        string outputFileName = SourceFileName.Replace(".src", ".m");

        // Add the entry point to the code and the halt instruction
        MoonCodeGenerator.Code.Insert(0, "entry\n");
        MoonCodeGenerator.Code.Insert(0, "\n% Execution Code\n");
        MoonCodeGenerator.Code.AppendLine("hlt");

        MoonCodeGenerator.Data.Insert(0, "\n% Data Section\n");

        File.WriteAllText(outputFileName, MoonCodeGenerator.Code.ToString());
        File.AppendAllText(outputFileName, MoonCodeGenerator.Data.ToString());
    }
}


public class MoonCodeGenerator : IMoonCodeGenerator
{
    public Stack<string> RegisterPool { get; set; } = FillRegisterPool();

    public Stack<string> RegistersInUse { get; set; } = new();

    public StringBuilder Code { get; set; } = new StringBuilder();

    public StringBuilder Data { get; set; } = new StringBuilder();

    private int tempVarNumber = 0;

    public string GetTempVarNumber()
    {
        return $"t{tempVarNumber++}";
    }

    private static Stack<string> FillRegisterPool()
    {
        Stack<string> registerPool = new();
        for (int i = 1; i <= 12; i++)
        {
            registerPool.Push($"r{i}");
        }
        return registerPool;
    }


    public void FreeRegister(string register)
    {
        RegisterPool.Push(register);
    }

    public string GetRegister()
    {
        string register = RegisterPool.Pop();
        RegistersInUse.Push(register);
        return register;
    }

    private string BuildVariableName(ISymbolTableEntry variableEntry)
    {
        return $"{variableEntry.Name}_{variableEntry.Line}";
    }

    public string DeclareVariable(ISymbolTableEntry variableEntry)
    {
        // Get the size of the variable
        int size = variableEntry.Size;

        // Assign memory to the variable
        Data.AppendLine($"{BuildVariableName(variableEntry)}\t\tres {size}  \t\t% Declaring {variableEntry.Name} : {variableEntry.Type}");

        var defaultSize = variableEntry.Type.Split('[')[0] == "float" ? 8 : 4;

        // Check if the variable is an array
        if (variableEntry.Type.Contains(']'))
        {
            var arrayDimensions = new List<int>();

            foreach (var dimension in variableEntry.Type.Split('[').Skip(1))
            {
                arrayDimensions.Add(int.Parse(dimension.Split(']')[0]));
            }

            var arrayDims = arrayDimensions.Aggregate((a, b) => a * b);

            for (int i = 0; i < arrayDims; i++)
            {
                //Code.AppendLine($"sw {BuildVariableName(variableEntry)}({i*defaultSize}),r0");
            }
        }
        else
        {
            // Initialize the variable to 0
            //Code.AppendLine($"sw {BuildVariableName(variableEntry)}(r0),r0 \t\t% Initializing {variableEntry.Name} to 0 (Default Value)");
        }

        return "";
    }

    public void LoadVariable(ISymbolTableEntry variableEntry)
    {
        // Get the register to load the variable into
        string register = GetRegister();

        // Load the variable into the register
        Code.AppendLine($"\t\tlw {register},{BuildVariableName(variableEntry)}(r0) \t\t% Loading {variableEntry.Name} : {variableEntry.Type}");
    }

    public string LoadInteger(string value)
    {
        string register = GetRegister();

        // Load the value into the register
        Code.AppendLine($"\t\taddi {register},r0,{value}\t\t% Loading {value} into {register}");

        return register;
    }

    public void NotExpr()
    {
        // Get the operand from the stack
        string operand = RegistersInUse.Pop();

        // Store the result in a register
        string resultRegister = GetRegister();

        // Negate the operand by creating a subroutine
        // Check how many times the endnot subroutine has been declared
        int endNotSubroutineCount = Code.ToString().Split("endnot").Length / 2 + 1;
        string endNotSubroutine = $"endnot{endNotSubroutineCount}";
        string zeroSubroutine = $"zeronot{endNotSubroutineCount}";

        Code.AppendLine($"\t\tbnz {operand},{zeroSubroutine}\t\t% Check if {operand} is zero");
        Code.AppendLine($"\t\taddi {resultRegister},r0,1\t\t% {operand} = 0, so {resultRegister} = 1");
        Code.AppendLine($"\t\tj {endNotSubroutine}\t\t% Jump to the end of the not subroutine");
        Code.AppendLine($"{zeroSubroutine}\t\t addi {resultRegister},r0,0\t\t% {operand} = 1, so {resultRegister} = 0");
        Code.AppendLine($"{endNotSubroutine}\t\t nop\t\t% End of the not subroutine");

        // Free the operand
        FreeRegister(operand);
    }

    public void AddExpr(string operation)
    {
        string op = operation switch
        {
            "+" => "add",
            "-" => "sub",
            "|" => "or",
            _ => "add"
        };

        // Get the two operands from the stack
        string operand2 = RegistersInUse.Pop();
        string operand1 = RegistersInUse.Pop();

        // Store the result in a register
        string resultRegister = GetRegister();

        if (op == "or")
        {
            // Check how many times the zero subroutine has been declared
            int nonZeroSubroutineCount = Code.ToString().Split("endOr").Length / 2 + 1;
            string nonZeroSubroutine = $"nonzero{nonZeroSubroutineCount}";
            string endOrSubroutine = $"endOr{nonZeroSubroutineCount}";

            Code.AppendLine($"\t\tbnz {operand1},{nonZeroSubroutine}\t\t% Check if {operand1} is not zero");
            Code.AppendLine($"\t\tbnz {operand2},{nonZeroSubroutine}\t\t% Check if {operand2} is not zero");
            Code.AppendLine($"\t\taddi {resultRegister},r0,1\t\t% {operand1} {operation} {operand2} = {resultRegister}");
            Code.AppendLine($"\t\tj {endOrSubroutine}\t\t% Jump to the end of the {operation} subroutine");
            Code.AppendLine($"{nonZeroSubroutine}\t\t addi {resultRegister},r0,1\t\t% Set the result to 1");
            Code.AppendLine($"{endOrSubroutine}\t\t nop\t\t% End of the {operation} subroutine");
        }
        else
        {
            // Add the two operands
            Code.AppendLine($"\t\t{op} {resultRegister}, {operand1}, {operand2}\t\t% {operand1} {operation} {operand2} = {resultRegister}");
        }

        // Free the operands
        FreeRegister(operand1);
        FreeRegister(operand2);
    }

    public void MultExpr(string operation)
    {
        string op = operation switch
        {
            "*" => "mul",
            "/" => "div",
            "&" => "and",
            _ => "mul"
        };

        // Get the two operands from the stack
        string operand2 = RegistersInUse.Pop();
        string operand1 = RegistersInUse.Pop();

        // Store the result in a register
        string resultRegister = GetRegister();

        if (op == "and")
        {
            // Check how many times the zero subroutine has been declared
            int zeroSubroutineCount = Code.ToString().Split("endAnd").Length / 2 + 1;
            string zeroSubroutine = $"zero{zeroSubroutineCount}";
            string endAndSubroutine = $"endAnd{zeroSubroutineCount}";

            Code.AppendLine($"\t\tbz {operand1},{zeroSubroutine}\t\t% Check if {operand1} is zero");
            Code.AppendLine($"\t\tbz {operand2},{zeroSubroutine}\t\t% Check if {operand2} is zero");
            Code.AppendLine($"\t\taddi {resultRegister},r0,1\t\t% {operand1} {operation} {operand2} = {resultRegister}");
            Code.AppendLine($"\t\tj {endAndSubroutine}\t\t% Jump to the end of the {operation} subroutine");
            Code.AppendLine($"{zeroSubroutine}\t\t addi {resultRegister},r0,0\t\t% Set the result to 0");
            Code.AppendLine($"{endAndSubroutine}\t\t nop\t\t% End of the {operation} subroutine");
        }
        else
        {
            // Multiply the two operands
            Code.AppendLine($"\t\t{op} {resultRegister}, {operand1}, {operand2}\t\t% {operand1} {operation} {operand2} = {resultRegister}");
        }

        // Free the operands
        FreeRegister(operand1);
        FreeRegister(operand2);
    }

    public void RelExpr(string operation)
    {
        string op = operation switch
        {
            "<" => "clt",
            "<=" => "cle",
            ">" => "cgt",
            ">=" => "cge",
            "==" => "ceq",
            "!=" => "cne",
            _ => "blt"
        };

        // Get the two operands from the stack
        string operand2 = RegistersInUse.Pop();
        string operand1 = RegistersInUse.Pop();

        // Store the result in a register
        string resultRegister = GetRegister();

        // Compare the two operands
        Code.AppendLine($"\t\t{op} {resultRegister}, {operand1}, {operand2}\t\t% {operand1} {operation} {operand2} = {resultRegister}");

        // Free the operands
        FreeRegister(operand1);
        FreeRegister(operand2);
    }


    public void Assign()
    {
        // Get the value to assign
        string value = RegistersInUse.Pop();

        // Get the variable to assign to
        string variable = RegistersInUse.Pop();

        // Assign the value to the variable
        Code.AppendLine($"\t\tadd {variable},r0,{value}\t\t% Assigning {value} to {variable}");

        // Free the value and the variable registers
        FreeRegister(value);
        FreeRegister(variable);
    }

    public void If(ref int ifCount)
    {
        // Get the condition
        string condition = RegistersInUse.Pop();

        ifCount = Code.ToString().Split("ifthen").Length;

        string ifLabel = $"ifthen{ifCount}";
        string elseLabel = $"else{ifCount}";

        Code.AppendLine($"{ifLabel}\t\tbz {condition},{elseLabel}\t\t% If {condition} is false, jump to {elseLabel}");

        // Free the condition
        FreeRegister(condition);
    }

    public void Else(ref int ifCount)
    {
        string endIfLabel = $"endif{ifCount}";
        Code.AppendLine($"\t\tj {endIfLabel}\t\t% Jump to {endIfLabel}");

        string elseLabel = $"else{ifCount}";

        Code.AppendLine($"{elseLabel}\t\t nop\t\t% Start of the else block");
    }

    public void EndIf(ref int ifCount)
    {
        string endIfLabel = $"endif{ifCount}";

        Code.AppendLine($"{endIfLabel}\t\t nop\t\t% End of the if block");
    }


    public void WhileCond(ref int whileCount)
    {
        whileCount = Code.ToString().Split("gowhile").Length;
        string goWhileLabel = $"gowhile{whileCount}";

        Code.AppendLine($"{goWhileLabel}\t\t nop\t\t% Start of the while condition block");
    }
    
    public void While(ref int whileCount)
    {
        string endWhileLabel = $"endwhile{whileCount}";

        // Get the condition
        string condition = RegistersInUse.Pop();

        Code.AppendLine($"\t\tbz {condition},{endWhileLabel}\t\t% If {condition} is false, jump to {endWhileLabel}");

        // Free the condition
        FreeRegister(condition);
    }

    public void EndWhile(ref int whileCount)
    {
        string goWhileLabel = $"gowhile{whileCount}";
        string endWhileLabel = $"endwhile{whileCount}";

        Code.AppendLine($"\t\tj {goWhileLabel}\t\t% Jump to {goWhileLabel}");
        Code.AppendLine($"{endWhileLabel}\t\t nop\t\t% End of the while block");
    }



    public void Write()
    {
        // Get the value to write
        string value = RegistersInUse.Pop();

        // Write the value
        
        // Load the top address of the stack
        Code.AppendLine($"\t\taddi r14,r0, topaddr\t\t% Load the top address of the stack");

        // Write the value to the stack
        Code.AppendLine($"\t\tsw -8(r14),{value}");
        
        // Put the address on the buffer stack
        Code.AppendLine($"\t\taddi {value},r0,buf\t\t% Put the address on the buffer stack");

        // Write the value to the stack
        Code.AppendLine($"\t\tsw -12(r14),{value}");

        // Call the int to string subroutine
        Code.AppendLine($"\t\tjl r15, intstr\t\t% Call the int to string subroutine");

        // Copy the result to the stack
        Code.AppendLine($"\t\tsw -8(r14),r13\t\t% Copy the result to the stack");

        // Call the print string subroutine
        Code.AppendLine($"\t\tjl r15, putstr\t\t% Call the print string subroutine");



        // Add a buffer of 20 bytes
        Data.AppendLine("buf\t\tres 20");

        // Free the value
        FreeRegister(value);
    }

}
