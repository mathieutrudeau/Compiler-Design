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
        MoonCodeGenerator.Code.Insert(0, "\n% Execution Code\n");

        MoonCodeGenerator.Data.Insert(0, "buf\t\tres 20\n");
        MoonCodeGenerator.Data.Insert(0, "entint\t\tdb \"Enter an integer: \", 0\n");
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

    public void LoadVariable(ISymbolTableEntry variableEntry, ISymbolTable table)
    {
        // Get the offset of the variable
        int offset = variableEntry.Offset;

        // Dimensions 
        int dimCount = GetDimCount(variableEntry.Type);
        int dimSize = GetDimSize(variableEntry.Type);

        if (dimSize > -1)
        {
            LoadArrayIndex(variableEntry, table);

            string aReg = RegistersInUse.Pop();

            // Get the register to load the variable into
            string register = GetRegister();

            Code.AppendLine($"\t\tlw {register},0({aReg}) \t\t% Loading {variableEntry.Name} into {register}");

            // Frame offset
            //int tableOffset = table.Parent!.Entries.Where(e => e.Name == table.Name).First().Offset;

            // Move the pointer back to the start of the current scope
            //Code.AppendLine($"\t\taddi r14,r0,{tableOffset}");

            FreeRegister(aReg);
        }
        else
        {
            // Get the register to load the variable into
            string register = GetRegister();

            Code.AppendLine($"\t\tlw {register},{offset}(r14) \t\t% Loading {variableEntry.Name} into {register}");
        }
    }

    public void LoadInteger(string value)
    {
        string register = GetRegister();

        // Load the value into the register
        Code.AppendLine($"\t\taddi {register},r0,{value}\t\t% Loading {value} into {register}");
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



        //WriteLine(Code.ToString());
        //WriteLine("value: " + value);
        //WriteLine("Var: " + variable);

        // Get the location of the variable, by looking at the last reference to the register
        string variableOffset = Code.ToString().Split('\n').Where(x => x.Contains(variable) && x.Contains("lw")).Last().Split(',')[1].Split('(')[0];

        // Assign the value to the variable
        Code.AppendLine($"\t\tadd {variable},r0,{value}\t\t% Assigning {value} to {variable}");

        // Store the value in the variable
        Code.AppendLine($"\t\tsw {variableOffset}(r14),{variable}");

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



    public void Write(ISymbolTable currentTable)
    {
        // Store the value in a register
        string value = RegistersInUse.Pop();

        // Write the value
        Code.AppendLine($"\n\t\t%----------------- WRITE -----------------");

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


    public void Read(ISymbolTable currentTable)
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
        Assign();
    }




    public void FunctionDeclaration(ISymbolTable currentTable)
    {


        Code.AppendLine($"\n\t\t%==================== {currentTable.Name} ====================\n");

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

        WriteLine("Calling function: " + functionTable.Name+" from "+currentTable.Name);

        List<ISymbolTableEntry> parameters = functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.Parameter).ToList();

        foreach (ISymbolTableEntry parameter in parameters)
        {
            WriteLine("Parameter: " + parameter.Name);

            // Get the parameter value
            string parameterValue = RegistersInUse.Pop();

            // Get the parameter offset
            int parameterOffset = parameter.Offset-currentTable.ScopeSize;

            // Store the parameter value in the function's stack frame
            Code.AppendLine($"\t\tsw {parameterOffset}(r14),{parameterValue}\t\t\t\t% Storing the parameter {parameter.Name}");
        }

        
        // Increment the stack frame
        Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t\t% Increment the stack frame");
        
        // Jump to the function
        Code.AppendLine($"\t\tjl r15,{functionTable.Name}\t\t\t\t\t% Jump to the function {functionTable.Name}");

        // Decrement the stack frame
        Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t\t\t% Decrement the stack frame");

    }

}
