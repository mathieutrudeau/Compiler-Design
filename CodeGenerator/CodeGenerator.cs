using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using AbstractSyntaxTreeGeneration;
using SemanticAnalyzer;
using static System.Console;
using System;
using System.Numerics;

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
        bool isArray = false;
        Root.GenerateCode(SymbolTable, MoonCodeGenerator, ref isArray);

        string outputFileName = SourceFileName.Replace(".src", ".m");

        // Add the entry point to the code and the halt instruction
        MoonCodeGenerator.Code.Insert(0, "\n% Execution Code\n");

        MoonCodeGenerator.Data.Insert(0, "buf\t\tres 100\n");
        MoonCodeGenerator.Data.Insert(0, "entfloat\t\tdb \"Enter a float: \", 0\n");
        MoonCodeGenerator.Data.Insert(0, "entint\t\tdb \"Enter an integer: \", 0\n");
        MoonCodeGenerator.Data.Insert(0, "dot\t\tdb \".\", 0\n");
        MoonCodeGenerator.Data.Insert(0, "nl\t\tdb 13, 10, 0\n");
        MoonCodeGenerator.Data.Insert(0, "\n% Data Section\n");


        File.WriteAllText(outputFileName, MoonCodeGenerator.Code.ToString());
        File.AppendAllText(outputFileName, MoonCodeGenerator.Data.ToString());
    }
}


public class MoonCodeGenerator : IMoonCodeGenerator
{
    public Stack<string> RegisterPool { get; set; } = FillRegisterPool();

    public Stack<string> RegistersInUse { get; set; } = new();

    public Stack<ISymbolTableEntry> TempVarsInUse { get; set; } = new();

    public Stack<ISymbolTableEntry> TempVars { get; set; } = new();

    public Stack<List<string>> FrameEscapes { get; set; } = new();

    public StringBuilder Code { get; set; } = new StringBuilder();

    public StringBuilder Data { get; set; } = new StringBuilder();

    private int tempVarNumber = 0;


    public MoonCodeGenerator()
    {
        FloatWriteSubroutine();
        ReadFloatSubroutine();
        FloatOpSubroutines();
    }

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

    private int GetDimSize(string varType)
    {
        int dimSize = -1;

        if (varType.Contains(']'))
        {
            List<int> arrayDims = new();

            foreach (string dimension in varType.Split('[').Skip(1))
                arrayDims.Add(int.Parse(dimension.Split(']')[0]));

            dimSize = arrayDims.Aggregate((a, b) => a * b);
        }

        return dimSize;
    }

    private int GetDimCount(string varType)
    {
        int dimSize = -1;

        if (varType.Contains(']'))
        {
            List<int> arrayDims = new();

            foreach (string dimension in varType.Split('[').Skip(1))
                arrayDims.Add(int.Parse(dimension.Split(']')[0]));

            return arrayDims.Count;
        }

        return dimSize;
    }

    private void GetDims(string varType, int[] dims)
    {
        if (varType.Contains(']'))
        {
            List<int> arrayDims = new();

            foreach (string dimension in varType.Split('[').Skip(1))
                arrayDims.Add(int.Parse(dimension.Split(']')[0]));

            for (int i = 0; i < arrayDims.Count; i++)
                dims[i] = arrayDims[i];
        }
    }

    public void DeclareVariable(ISymbolTableEntry variableEntry)
    {
        // Get the offset of the variable
        int offset = variableEntry.Offset;

        // Var dimensions 
        int arrayDims = GetDimSize(variableEntry.Type);

        int defaultSize = variableEntry.Size / (arrayDims < 0 ? 1 : arrayDims);

        for (int i = 0; i < arrayDims; i++)
            Code.AppendLine($"\t\tsw {offset - i * defaultSize}(r14),r0 \t\t% Initializing {variableEntry.Name}(pos {i}) to 0 (Default Value)");

        if (arrayDims < 0)
            Code.AppendLine($"\t\tsw {offset}(r14),r0 \t\t% Initializing {variableEntry.Name} to 0 (Default Value)");


    }

    private void LoadArrayIndex(ISymbolTableEntry variableEntry, ISymbolTable table)
    {
        string varType = variableEntry.Type;

        // Get all dimensions
        if (varType.Contains(']'))
        {
            List<int> arrayDims = new();

            foreach (string dimension in varType.Split('[').Skip(1))
                arrayDims.Add(int.Parse(dimension.Split(']')[0]));

            int elSize = variableEntry.Size / arrayDims.Aggregate((a, b) => a * b);

            int size = 1;

            string tReg = GetRegister();
            RegistersInUse.Pop();

            Code.AppendLine($"\t\taddi {tReg},r0,0");

            foreach (int d in arrayDims.Reverse<int>())
            {
                string r = RegistersInUse.Pop();

                Code.AppendLine($"\t\tmuli {r},{r},{size}");
                Code.AppendLine($"\t\tadd {tReg},{tReg},{r}");

                size *= d;

                FreeRegister(r);
            }
            Code.AppendLine($"\t\tmuli {tReg},{tReg},-{elSize}");
            Code.AppendLine($"\t\taddi {tReg},{tReg},{variableEntry.Offset}");
            Code.AppendLine($"\t\tadd {tReg},{tReg},r14");


            RegistersInUse.Push(tReg);
        }


    }

    public void LoadVariable(string type, ISymbolTableEntry variableEntry, ISymbolTable table)
    {
        // Get the offset of the variable
        int offset = variableEntry.Offset;

        string locReg = GetRegister();
        locReg = RegistersInUse.Pop();

        Code.AppendLine($"\n\t\taddi {locReg},r0,0");

        if (type != variableEntry.Type)
        {
            Code.AppendLine($"\t\t% Load array index frame");

            int dimCount = GetDimCount(variableEntry.Type);
            int dimSize = GetDimSize(variableEntry.Type);
            int[] dims = new int[dimCount];
            GetDims(variableEntry.Type, dims);

            int index = dimSize;

            for (int i = dimCount - 1; i >= 0; i--)
            {
                // Get the index register
                string indexReg = RegistersInUse.Pop();
                index /= dims[i];

                Code.AppendLine($"\t\tmuli {indexReg},{indexReg},-{index}");
                Code.AppendLine($"\t\tadd {locReg},{locReg},{indexReg}");

                // Free the index register
                FreeRegister(indexReg);
            }

            // Multiply the index by the size of the element
            Code.AppendLine($"\t\tmuli {locReg},{locReg},{variableEntry.Size / dimSize}");

            // Add the offset to the location register
            Code.AppendLine($"\t\taddi {locReg},{locReg},{offset}");
            Code.AppendLine($"\t\tadd {locReg},{locReg},r14");
        }
        else
        {
            Code.AppendLine($"\t\taddi {locReg},{locReg},{offset}");
            Code.AppendLine($"\t\tadd {locReg},{locReg},r14");
        }


        //WriteLine("Loading Variable: " + variableEntry.Type);

        if (type == "integer")
        {
            // Load the variable into a register
            string register = GetRegister();

            // Load the variable from the stack
            Code.AppendLine($"\t\tlw {register},0({locReg}) \t\t% Loading {variableEntry.Name} into {register}");

        }
        else if (type == "float")
        {
            // Load the variable into a register
            string register = GetRegister();

            // Load the variable point position from the stack and store it in a register
            string pointReg = GetRegister();

            Code.AppendLine($"\t\tlw {register},0(r14) \t\t% Loading {variableEntry.Name} into {register}");
            Code.AppendLine($"\t\tlw {pointReg},{offset - 4}(r14) \t\t% Loading the point position of {variableEntry.Name} into {pointReg}");
        }

        if (type == variableEntry.Type)
            // Free the location register
            FreeRegister(locReg);
        else
        {
            RegistersInUse.Push(locReg);
        }
    }

    public void LoadInteger(string value)
    {

        Code.AppendLine($"\n\t\t% Loading Integer Value: {value}");

        string register = GetRegister();

        // Convert the value to an integer
        bool isValid = int.TryParse(value, out int intValue);

        if (!isValid)
        {
            WriteLine("Invalid Integer Value: " + value);
            intValue = 0;
        }

        // Get the number of bits needed to represent the integer
        int minBits = (int)Math.Ceiling(Math.Log(intValue + 1, 2));

        // Convert the integer to binary
        string binary = Convert.ToString(intValue, 2);

        // Pad the binary string with zeros to make it 32 bits
        if (binary.Length < 32)
            binary = binary.PadLeft(32, '0');

        // Load the integer into the register
        // If the integer is larger than 8 bits, load it in 8-bit chunks and shift the register
        if (minBits > 8)
            for (int i = 0; i < binary.Length; i += 8)
            {
                int val = Convert.ToInt32(binary.Substring(i, 8), 2);

                if (i == 0)
                    Code.AppendLine($"\t\taddi {register},r0,{val}");
                else
                    Code.AppendLine($"\t\taddi {register},{register},{val}");

                if (i != 24)
                    Code.AppendLine($"\t\tsl {register},8");
            }
        else
            Code.AppendLine($"\t\taddi {register},r0,{intValue}");
    }


    public void LoadFloat(string value)
    {
        Code.AppendLine($"\n\t\t% Loading Float Value: {value}");

        // Transform the float value into an integer and while keeping the position of the decimal point (relative to the right)
        // For example, 3.14 will be transformed into 314 with a decimal point at the 2nd position from the right
        int pointPosition = value.Length - (value.IndexOf('.') + 1);
        value = value.Replace(".", "");

        // Load the integer value
        LoadInteger(value);

        // Load the point position
        string pointReg = GetRegister();

        Code.AppendLine($"\t\taddi {pointReg},r0,{pointPosition}\t\t% Load the point position of the float value");
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

    public void AddExpr(string operation, ISymbolTable currentTable, bool isFloat = false)
    {
        string op = operation switch
        {
            "+" => "add",
            "-" => "sub",
            "|" => "or",
            _ => "add"
        };

        if (isFloat)
        {
            FloatOp(op, currentTable);
            return;
        }

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

            Code.AppendLine($"\n\t\tbnz {operand1},{nonZeroSubroutine}\t\t% Check if {operand1} is not zero");
            Code.AppendLine($"\t\tbnz {operand2},{nonZeroSubroutine}\t\t% Check if {operand2} is not zero");
            Code.AppendLine($"\t\taddi {resultRegister},r0,1\t\t% {operand1} {operation} {operand2} = {resultRegister}");
            Code.AppendLine($"\t\tj {endOrSubroutine}\t\t% Jump to the end of the {operation} subroutine");
            Code.AppendLine($"{nonZeroSubroutine}\t\t addi {resultRegister},r0,1\t\t% Set the result to 1");
            Code.AppendLine($"{endOrSubroutine}\t\t nop\t\t% End of the {operation} subroutine");
        }
        else
        {
            // Add the two operands
            Code.AppendLine($"\n\t\t{op} {resultRegister}, {operand1}, {operand2}\t\t% {operand1} {operation} {operand2} = {resultRegister}");
        }

        // Free the operands
        FreeRegister(operand1);
        FreeRegister(operand2);
    }



    private void FloatOp(string op, ISymbolTable currentTable)
    {
        // Get the two operands from the stack
        string pointReg2 = RegistersInUse.Pop();
        string valueReg2 = RegistersInUse.Pop();

        string pointReg1 = RegistersInUse.Pop();
        string valueReg1 = RegistersInUse.Pop();

        // Store the result in a register
        string resultReg = GetRegister();
        string pointReg = GetRegister();

        if (op == "or" || op == "and")
        {

        }
        else
        {
            // Go to the next stack frame
            Code.AppendLine($"\n\t\t%----------------- {op} Float -----------------");
            Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

            // Store the float values
            Code.AppendLine($"\t\tsw 0(r14),{valueReg1}\t\t% Store the first float value");
            Code.AppendLine($"\t\tsw -4(r14),{pointReg1}\t\t% Store the point position of the first float value");
            Code.AppendLine($"\t\tsw -8(r14),{valueReg2}\t\t% Store the second float value");
            Code.AppendLine($"\t\tsw -12(r14),{pointReg2}\t\t% Store the point position of the second float value");

            // Call the add/sub float subroutine
            Code.AppendLine($"\t\tjl r15,{op}float\t\t% Call the {op} float subroutine");

            // Load the result
            Code.AppendLine($"\t\tlw {resultReg},0(r14)\t\t% Load the result of the {op} operation");
            Code.AppendLine($"\t\tlw {pointReg},-4(r14)\t\t% Load the point position of the result");

            // Go back to the current stack frame
            Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Move back to the current stack frame");
        }

        // Free the registers
        FreeRegister(pointReg2);
        FreeRegister(valueReg2);

        FreeRegister(pointReg1);
        FreeRegister(valueReg1);
    }


    private void FloatOpSubroutines()
    {
        string[] addOps = new string[] { "add", "mul", "div", "sub", "clt", "cle", "cgt", "cge", "ceq", "cne" };

        // Add/Sub Float Subroutine

        foreach (string op in addOps)
        {
            Code.AppendLine($"\n\t\t%----------------- {op} Float Subroutine -----------------");

            Code.AppendLine($"{op}float\t\t nop\t\t% Start of the {op} float subroutine");

            // Save the contents of r1, r2, r3, r4 and return address
            Code.AppendLine($"\t\tsw -16(r14),r1\t\t% Save contents of r1");
            Code.AppendLine($"\t\tsw -20(r14),r2\t\t% Save contents of r2");
            Code.AppendLine($"\t\tsw -24(r14),r3\t\t% Save contents of r3");
            Code.AppendLine($"\t\tsw -28(r14),r4\t\t% Save contents of r4");
            Code.AppendLine($"\t\tsw -32(r14),r15\t\t% Save the return address");

            // Load the float values
            Code.AppendLine($"\t\tlw r1,0(r14)\t\t% Load the first float value");
            Code.AppendLine($"\t\tlw r2,-4(r14)\t\t% Load the point position of the first float value");
            Code.AppendLine($"\t\tlw r3,-8(r14)\t\t% Load the second float value");
            Code.AppendLine($"\t\tlw r4,-12(r14)\t\t% Load the point position of the second float value");

            Code.AppendLine($"{op}float1\t\tceq r15,r2,r4\t\t% Check if the point positions are equal");
            Code.AppendLine($"\t\tbnz r15,{op}float2\t\t% If the point positions are equal, jump to {op}float2");
            Code.AppendLine($"\t\tclt r15,r2,r4\t\t% Check if the first point position is less than the second point position");
            Code.AppendLine($"\t\tbnz r15,{op}float3\t\t% If the first point position is less than the second point position, jump to {op}float3");
            Code.AppendLine($"\t\tj {op}float4\t\t% Jump to {op}float4");

            Code.AppendLine($"{op}float3\t\taddi r2,r2,1\t\t% Increment the first point position");
            Code.AppendLine($"\t\tmuli r1,r1,10\t\t% Multiply the first float value by 10");
            Code.AppendLine($"\t\tj {op}float1\t\t% Jump to {op}float1");

            Code.AppendLine($"{op}float4\t\taddi r4,r4,1\t\t% Increment the second point position");
            Code.AppendLine($"\t\tmuli r3,r3,10\t\t% Multiply the second float value by 10");
            Code.AppendLine($"\t\tj {op}float1\t\t% Jump to {op}float1");

            Code.AppendLine($"{op}float2\t\t{op} r15,r1,r3\t\t% Perform the {op} operation on the float values");
            Code.AppendLine($"\t\tsw 0(r14),r15\t\t% Store the result of the {op} operation");
            Code.AppendLine($"\t\tsw -4(r14),r2\t\t% Store the point position of the result");

            // Restore the contents of r1, r2, r3, r4 and return address
            Code.AppendLine($"\t\tlw r1,-16(r14)\t\t% Restore contents of r1");
            Code.AppendLine($"\t\tlw r2,-20(r14)\t\t% Restore contents of r2");
            Code.AppendLine($"\t\tlw r3,-24(r14)\t\t% Restore contents of r3");
            Code.AppendLine($"\t\tlw r4,-28(r14)\t\t% Restore contents of r4");
            Code.AppendLine($"\t\tlw r15,-32(r14)\t\t% Restore the return address");

            Code.AppendLine($"\t\tjr r15\t\t% Return from the {op} float subroutine");
        }


        // Add the multiplication float subroutine
        Code.AppendLine($"\n\t\t%----------------- Mult Float Subroutine -----------------");

        // Add the division float subroutine
        Code.AppendLine($"\n\t\t%----------------- Div Float Subroutine -----------------");


    }


    public void MultExpr(string operation, ISymbolTable currentTable, bool isFloat = false)
    {
        string op = operation switch
        {
            "*" => "mul",
            "/" => "div",
            "&" => "and",
            _ => "mul"
        };

        if (isFloat)
        {
            FloatOp(op, currentTable);
            return;
        }

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

    public void RelExpr(string operation, ISymbolTable currentTable, bool isFloat = false)
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

        if (isFloat)
        {
            FloatOp(op, currentTable);
            return;
        }

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


    public void AssignFloat(bool isArray = false)
    {
        // Get the point position
        string pointReg = RegistersInUse.Pop();

        // Get the value to assign
        string valueReg = RegistersInUse.Pop();

        // Get the location of the variable, by looking at the last reference to the register
        string variablePointReg = RegistersInUse.Pop();

        // Get the variable to assign to
        string variableReg = RegistersInUse.Pop();

        // Get the location of the variable, by looking at the last reference to the register
        int variableOffset = int.Parse(Code.ToString().Split('\n').Where(x => x.Contains(variableReg) && x.Contains("lw")).Last().Split(',')[1].Split('(')[0]);

        //WriteLine("Assigning Float: " + valueReg + " to " + variableReg + " with point position " + pointReg);

        Code.AppendLine($"\n\t\t% Assignment of Float Value");

        // Assign the value to the variable
        Code.AppendLine($"\t\tadd {variableReg},r0,{valueReg}\t\t% Assigning {valueReg} to {variableReg}");

        // Store the value in the variable
        Code.AppendLine($"\t\tsw {variableOffset}(r14),{variableReg}");

        // Store the point position in the variable
        Code.AppendLine($"\t\tsw {variableOffset - 4}(r14),{pointReg}");

        // Free the registers
        FreeRegister(pointReg);
        FreeRegister(valueReg);
        FreeRegister(variableReg);
        FreeRegister(variablePointReg);
    }

    public void AssignInteger(bool isArray = false)
    {

        // Get the value to assign
        string value = RegistersInUse.Pop();

        string locReg = isArray ? RegistersInUse.Pop() : "r14";

        // Get the variable to assign to
        string variable = RegistersInUse.Pop();

        if (isArray)
        {
            // Assign the value to the variable
            Code.AppendLine($"\t\tadd {variable},r0,{value}\t\t% Assigning {value} to {variable}");

            // Store the value in the variable
            Code.AppendLine($"\t\tsw 0({locReg}),{variable}");

            FreeRegister(locReg);
        }
        else
        {

            // Get the location of the variable, by looking at the last reference to the register
            string variableOffset = Code.ToString().Split('\n').Where(x => x.Contains(variable) && x.Contains("lw")).Last().Split(',')[1].Split('(')[0];

            // Assign the value to the variable
            Code.AppendLine($"\t\tadd {variable},r0,{value}\t\t% Assigning {value} to {variable}");

            // Store the value in the variable
            Code.AppendLine($"\t\tsw {variableOffset}(r14),{variable}");
        }

        // Free the value and the variable registers
        FreeRegister(value);
        FreeRegister(variable);

        //WriteLine("Freeing Registers: " + value + " " + variable);
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



    public void WriteInteger(ISymbolTable currentTable)
    {
        while(RegistersInUse.Count > 1)
            FreeRegister(RegistersInUse.Pop());

        // Store the value in a register
        string value = RegistersInUse.Pop();

        // Write the value
        Code.AppendLine($"\n\t\t%----------------- WRITE Integer -----------------");

        // Go to the next stack frame
        Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

        // Write the value to the console/screen
        Code.AppendLine($"\t\tsw -8(r14),{value}");
        Code.AppendLine($"\t\taddi {value},r0,buf");
        Code.AppendLine($"\t\tsw -12(r14),{value}");
        Code.AppendLine($"\t\tjl r15,intstr\t\t% Call the int -> string subroutine");
        Code.AppendLine($"\t\tsw -8(r14),r13");
        Code.AppendLine($"\t\tjl r15,putstr\t\t% Call the print subroutine");
        Code.AppendLine($"\t\taddi {value},r0,nl");
        Code.AppendLine($"\t\tsw -8(r14),{value}");
        Code.AppendLine($"\t\tjl r15,putstr\t\t% Print a newline");

        // Go back to the current stack frame
        Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Move back to the current stack frame\n");

        // Free the value register
        FreeRegister(value);
    }

    public void WriteFloat(ISymbolTable currentTable)
    {
        // Store the value in a register
        string pointReg = RegistersInUse.Pop();

        // Store the point position in a register
        string value = RegistersInUse.Pop();

        // Write the value
        Code.AppendLine($"\n\t\t%----------------- WRITE Float -----------------");

        // Go to the next stack frame
        Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

        Code.AppendLine($"\t\tsw -28(r14),{value}\t\t\t% Save contents of value");
        Code.AppendLine($"\t\tsw -32(r14),{pointReg}\t\t\t% Save contents of point position");

        // Call the float write subroutine
        Code.AppendLine($"\t\tjl r15,floatwrite\t\t% Call the float write subroutine");

        // Go back to the current stack frame
        Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t\t% Move back to the current stack frame\n");

        // Free both registers
        FreeRegister(value);
        FreeRegister(pointReg);
    }

    private void FloatWriteSubroutine()
    {
        Code.AppendLine($"\n\t\t%----------------- Write float subroutine -----------------");

        Code.AppendLine($"floatwrite\t\t nop\t\t% Start of the float write subroutine");

        // Save the contents of r1, r2, r3 and r4
        Code.AppendLine($"\t\tsw -36(r14),r1\t\t% Save contents of r1");
        Code.AppendLine($"\t\tsw -40(r14),r2\t\t% Save contents of r2");
        Code.AppendLine($"\t\tsw -44(r14),r3\t\t% Save contents of r3");
        Code.AppendLine($"\t\tsw -48(r14),r4\t\t% Save contents of r4");

        // Save the return address
        Code.AppendLine($"\t\tsw -52(r14),r15\t\t% Save the return address");

        // Load the float value
        Code.AppendLine($"\t\tlw r1,-28(r14)\t\t% Load the float value");
        Code.AppendLine($"\t\tlw r2,-32(r14)\t\t% Load the point position");

        // Calculate the modulus divisor for the point position
        Code.AppendLine($"\t\taddi r3,r0,1\t\t% Initialize the modulus divisor");

        Code.AppendLine($"whilemodulus\t\tceq r4,r2,r0\t\t% Check if the point position is 0");
        Code.AppendLine($"\t\tbnz r4,endwhilemodulus\t\t% If the point position is 0, exit the loop");
        Code.AppendLine($"\t\t\tmuli r3,r3,10\t\t% Multiply the modulus divisor by 10");
        Code.AppendLine($"\t\t\tsubi r2,r2,1\t\t% Decrement the point position");
        Code.AppendLine($"\t\t\tbz r2,endwhilemodulus\t\t% If the point position is 0, exit the loop");
        Code.AppendLine($"\t\t\tj whilemodulus\t\t% Jump back to the start of the loop");
        Code.AppendLine($"endwhilemodulus\t\t nop\t\t% End of the while loop");

        // Calculate the integer part of the float value
        Code.AppendLine($"\t\tmod r4,r1,r3\t\t% Calculate the integer part of the float value");
        Code.AppendLine($"\t\tsub r1,r1,r4\t\t% Remove the fractional part of the float value");
        Code.AppendLine($"\t\tdiv r1,r1,r3\t\t% Divide the integer part by the modulus divisor");

        // Keep the fractional part of the float value in r2
        Code.AppendLine($"\t\tadd r2,r0,r4\t\t% Save the fractional part of the float value");

        // Print the integer part of the float value
        Code.AppendLine($"\n\t\tsw -8(r14),r1\t\t% Store the integer part of the float value");
        Code.AppendLine($"\t\taddi r2,r0,buf\t\t% Load the buffer address");
        Code.AppendLine($"\t\tsw -12(r14),r2\t\t% Store the buffer address");
        Code.AppendLine($"\t\tjl r15,intstr\t\t% Call the int -> string subroutine");
        Code.AppendLine($"\t\tsw -8(r14),r13\t\t% Store the string address");
        Code.AppendLine($"\t\tjl r15,putstr\t\t% Call the print subroutine");

        // Print the decimal point
        Code.AppendLine($"\n\t\taddi r2,r0,dot\t\t% Load the decimal point");
        Code.AppendLine($"\t\tsw -8(r14),r2\t\t% Store the decimal point");
        Code.AppendLine($"\t\tjl r15,putstr\t\t% Call the print subroutine");

        // Print the fractional part of the float value
        Code.AppendLine($"\n\t\tsw -8(r14),r4\t\t% Store the fractional part of the float value");
        Code.AppendLine($"\t\taddi r2,r0,buf\t\t% Load the buffer address");
        Code.AppendLine($"\t\tsw -12(r14),r2\t\t% Store the buffer address");
        Code.AppendLine($"\t\tjl r15,intstr\t\t% Call the int -> string subroutine");
        Code.AppendLine($"\t\tsw -8(r14),r13\t\t% Store the string address");
        Code.AppendLine($"\t\tjl r15,putstr\t\t% Call the print subroutine");

        // Print a newline
        Code.AppendLine($"\n\t\taddi r4,r0,nl\t\t% Load the newline");
        Code.AppendLine($"\t\tsw -8(r14),r4\t\t% Store the newline");
        Code.AppendLine($"\t\tjl r15,putstr\t\t% Call the print subroutine");

        // Restore the contents of r1, r2 and r3
        Code.AppendLine($"\t\tlw r1,-36(r14)\t\t% Restore contents of r1");
        Code.AppendLine($"\t\tlw r2,-40(r14)\t\t% Restore contents of r2");
        Code.AppendLine($"\t\tlw r3,-44(r14)\t\t% Restore contents of r3");
        Code.AppendLine($"\t\tlw r4,-48(r14)\t\t% Restore contents of r4");

        // Restore the return address
        Code.AppendLine($"\t\tlw r15,-52(r14)\t\t% Restore the return address");

        // Return from the subroutine
        Code.AppendLine($"\t\tjr r15\t\t% Return from the float write subroutine");

    }

    private void ReadFloatSubroutine()
    {
        Code.AppendLine($"\n\t\t%----------------- Read float subroutine -----------------");

        Code.AppendLine($"getfloat\t\t nop\t\t% Start of the float read subroutine");

        // Save the contents of r1, r2, r3, r4 and r15 (return address)
        Code.AppendLine($"\t\tsw -36(r14),r1\t\t% Save contents of r1");
        Code.AppendLine($"\t\tsw -40(r14),r2\t\t% Save contents of r2");
        Code.AppendLine($"\t\tsw -44(r14),r3\t\t% Save contents of r3");
        Code.AppendLine($"\t\tsw -48(r14),r4\t\t% Save contents of r4");
        Code.AppendLine($"\t\tsw -52(r14),r15\t\t% Save the return address");

        // Initialize the point position to 0 (no decimal point) and load the buffer address
        Code.AppendLine($"\t\taddi r4,r0,0\t\t% Initialize the point position");
        Code.AppendLine($"\t\taddi r1,r0,buf\t\t% Load the buffer address");

        // Get the first part of the float value (before the decimal point)
        Code.AppendLine($"\ngetfloat1\t\tgetc r2\t\t% Get the next character");
        Code.AppendLine($"\t\tceqi r3,r2,46\t\t% Check if the character is a decimal point");
        Code.AppendLine($"\t\tbnz r3,getfloat2\t\t% If the character is not a decimal point, jump to getfloat2");
        Code.AppendLine($"\t\tceqi r3,r2,10\t\t% Check if the character is a newline");
        Code.AppendLine($"\t\tbnz r3,endgetfloat\t\t% If the character is a newline, jump to endgetfloat");
        Code.AppendLine($"\t\tsb 0(r1),r2\t\t% Store the character in the buffer");
        Code.AppendLine($"\t\taddi r1,r1,1\t\t% Increment the buffer address");
        Code.AppendLine($"\t\tj getfloat1\t\t% Get the next character");

        // Get the second part of the float value (after the decimal point)
        Code.AppendLine($"\ngetfloat2\t\tgetc r2\t\t% Get the next character");
        Code.AppendLine($"\t\tceqi r3,r2,10\t\t% Check if the character is a newline");
        Code.AppendLine($"\t\tbnz r3,endgetfloat\t\t% If the character is a newline, jump to endgetfloat");
        Code.AppendLine($"\t\taddi r4,r4,1\t\t% Increment the point position");
        Code.AppendLine($"\t\tsb 0(r1),r2\t\t% Store the character in the buffer");
        Code.AppendLine($"\t\taddi r1,r1,1\t\t% Increment the buffer address");
        Code.AppendLine($"\t\tj getfloat2\t\t% Get the next character");

        // End of the float read subroutine
        Code.AppendLine($"\nendgetfloat\t\t sb 0(r1),r0\t\t% Add a null terminator to the buffer");
        Code.AppendLine($"\t\tsw -56(r14),r4\t\t% Store the point position");
        Code.AppendLine($"\t\tjl r15,strint\t\t% Call the string -> int subroutine");
        Code.AppendLine($"\t\tsw -60(r14),r13\t\t% Store the integer part of the float value");

        // Restore the contents of r1, r2, r3, r4 and r15 (return address)
        Code.AppendLine($"\t\tlw r1,-36(r14)\t\t% Restore contents of r1");
        Code.AppendLine($"\t\tlw r2,-40(r14)\t\t% Restore contents of r2");
        Code.AppendLine($"\t\tlw r3,-44(r14)\t\t% Restore contents of r3");
        Code.AppendLine($"\t\tlw r4,-48(r14)\t\t% Restore contents of r4");
        Code.AppendLine($"\t\tlw r15,-52(r14)\t\t% Restore the return address");

        // Return from the subroutine
        Code.AppendLine($"\t\tjr r15\t\t% Return from the float read subroutine");
    }

    public void ReadInteger(ISymbolTable currentTable)
    {
        // Get a register to store the value
        string value = GetRegister();

        Code.AppendLine($"\n\t\t%----------------- READ -----------------");

        // Go to the next stack frame
        Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t\t% Go to the next stack frame");

        // Prompt the user for an integer
        Code.AppendLine($"\t\taddi {value},r0,entint\t\t\t% Prompt for an integer");
        Code.AppendLine($"\t\tsw -8(r14),{value}");
        Code.AppendLine($"\t\tjl r15,putstr");

        // Get the integer from the user
        Code.AppendLine($"\t\taddi {value},r0,buf");
        Code.AppendLine($"\t\tsw -8(r14),{value}");
        Code.AppendLine($"\t\tjl r15,getstr\t\t\t% Call the get string subroutine");
        Code.AppendLine($"\t\tjl r15,strint\t\t\t% Call the string -> int subroutine");
        Code.AppendLine($"\t\taddi {value},r13,0");

        // Restore the memory location
        Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t\t% Go back to the current stack frame\n");

        // Store the value in the variable
        AssignInteger();
    }


    public void ReadFloat(ISymbolTable currentTable)
    {
        // Get a register to store the value
        string value = GetRegister();

        // Get a register to store the point position
        string pointReg = GetRegister();

        Code.AppendLine($"\n\t\t%----------------- READ Float -----------------");

        // Go to the next stack frame
        Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t\t\t% Go to the next stack frame");

        // Prompt the user for a float
        Code.AppendLine($"\t\taddi {value},r0,entfloat\t\t\t% Prompt for a float");
        Code.AppendLine($"\t\tsw -8(r14),{value}");
        Code.AppendLine($"\t\tjl r15,putstr");

        // Store the buffer address
        Code.AppendLine($"\t\taddi {value},r0,buf");
        Code.AppendLine($"\t\tsw -8(r14),{value}");

        // Get the float from the user
        Code.AppendLine($"\t\tjl r15,getfloat\t\t\t% Call the float read subroutine");

        // Get the integer and point position from the stack
        Code.AppendLine($"\t\tlw {value},-60(r14)\t\t\t% Load the integer part of the float value");
        Code.AppendLine($"\t\tlw {pointReg},-56(r14)\t\t\t% Load the point position of the float value");

        // Restore the memory location
        Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t\t% Go back to the current stack frame\n");

        // Store the value in the variable
        AssignFloat();

    }




    public void FunctionDeclaration(ISymbolTable currentTable)
    {
        Code.AppendLine($"\n\t\t%==================== Function/Method: {currentTable.Name} ====================\n");

        // Add the temporary variables to the stack frame
        TempVars = new Stack<ISymbolTableEntry>(currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.TempVar).Reverse());
        TempVarsInUse = new Stack<ISymbolTableEntry>();

        //foreach (ISymbolTableEntry tempVar in TempVars)
        //    WriteLine("TempVar: " + tempVar.Name);


        if (currentTable.Name == "main")
        {
            Code.AppendLine("entry\t\t% Start of the program");
            Code.AppendLine($"\t\taddi r14,r0,topaddr\t\t% Set the top of the stack");
            return;
        }

        int jumpOffset = currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.JumpAddress).First().Offset;

        // Tag the function's call address
        Code.AppendLine($"{currentTable.Name}\t\tsw {jumpOffset}(r14),r15\t\t\t% Tag the function call address");
    }


    public void FunctionDeclarationEnd(ISymbolTable currentTable)
    {
        if (currentTable.Name == "main")
            Code.AppendLine("hlt\t\t% Halt the program\n");
        else
        {
            // Get the return address to jump back to
            int jumpOffset = currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.JumpAddress).First().Offset;

            // Jump back to the return address
            Code.AppendLine($"\t\tlw r15,{jumpOffset}(r14)\t\t\t% Jump back to the return address");
            Code.AppendLine($"\t\tjr r15\t\t\t\t\t% Jump back to the return address");
        }

        Code.AppendLine($"\n\t\t%==================== End of {currentTable.Name} ====================\n");
    }



    public void Return(ISymbolTable currentTable)
    {
        // Get the return value register
        string returnValReg = RegistersInUse.Pop();

        // Get the address where the return value should be stored in the stack
        int returnOffset = currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset;

        // Store the return
        Code.AppendLine($"\t\tsw {returnOffset}(r14),{returnValReg}\t\t\t% Storing the return value");

        // Free the return value register
        FreeRegister(returnValReg);
    }



    public void CallFunction(ISymbolTable currentTable, ISymbolTable functionTable)
    {

        //WriteLine("Calling function: " + functionTable.Name+" from "+currentTable.Name);

        List<ISymbolTableEntry> parameters = functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.Parameter).ToList();

        foreach (ISymbolTableEntry parameter in parameters)
        {
            //WriteLine("Parameter: " + parameter.Name);

            // Get the parameter value
            string parameterValue = RegistersInUse.Pop();

            // Get the parameter offset
            int parameterOffset = parameter.Offset - currentTable.ScopeSize;

            // Store the parameter value in the function's stack frame
            Code.AppendLine($"\t\tsw {parameterOffset}(r14),{parameterValue}\t\t\t\t% Storing the parameter {parameter.Name}");
        }


        // Increment the stack frame
        Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t\t% Increment the stack frame");

        // Jump to the function
        Code.AppendLine($"\t\tjl r15,{functionTable.Name}\t\t\t\t\t% Jump to the function {functionTable.Name}");

        // Decrement the stack frame
        Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t\t\t% Decrement the stack frame");

        // Get the return value register
        string returnValReg = GetRegister();

        // Get the address where the return value should be stored in the stack
        int returnOffset = functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset - currentTable.ScopeSize;

        // Load the return value
        Code.AppendLine($"\t\tlw {returnValReg},{returnOffset}(r14)\t\t\t\t% Loading the return value");
    }






    public void ClassDeclaration(ISymbolTable currentTable)
    {

        Code.AppendLine($"\n\t\t%************************* Class: {currentTable.Name} **********************************n");




        WriteLine("Class Declaration");
    }

    public void ClassDeclarationEnd(ISymbolTable currentTable)
    {
        WriteLine("Class Declaration End");


        Code.AppendLine($"\n\t\t%************************* End of {currentTable.Name} **********************************n");
    }

    public void ClassVariable(ISymbolTable currentTable, ISymbolTableEntry entry)
    {
        WriteLine("Class Variable");

        // Get the offset of the variable
        int offset = entry.Offset;

        WriteLine("Offset: " + offset);

        // Set the frame pointer to the start of the class
        Code.AppendLine($"\t\taddi r14,r14,{offset}\t\t% Set the frame pointer to the start of the class variable");

    }

    public void ClassVariableEnd(ISymbolTable currentTable, ISymbolTable classTable)
    {
        WriteLine("Class Variable End");
    }
}
