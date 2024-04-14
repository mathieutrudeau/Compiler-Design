using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using AbstractSyntaxTreeGeneration;
using SemanticAnalyzer;
using static System.Console;
using System;
using System.Numerics;
using System.Linq.Expressions;
using System.Data.Common;

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
        ISymbolTable chainedTable = SymbolTable;
        int scopeSize = 0;
        Root.GenerateCode(SymbolTable, MoonCodeGenerator, ref scopeSize, ref chainedTable, ref isArray);

        string outputFileName = SourceFileName.Replace(".src", ".m");

        // Add the entry point to the code and the halt instruction
        MoonCodeGenerator.Code.Insert(0, "\n% Execution Code\n");

        MoonCodeGenerator.Data.Insert(0, "buf\t\tres 100\n");
        MoonCodeGenerator.Data.Insert(0, "entfloat\t\tdb \"Enter a float: \", 0\n");
        MoonCodeGenerator.Data.Insert(0, "entint\t\tdb \"Enter an integer: \", 0\n");
        MoonCodeGenerator.Data.Insert(0, "dot\t\tdb \".\", 0\n");
        MoonCodeGenerator.Data.Insert(0, "zero\t\tdb \"0\", 0\n");
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

    public MoonCodeGenerator()
    {
        FloatWriteSubroutine();
        IntegerWriteSubroutine();
        ReadFloatSubroutine();
        FloatOpSubroutines();
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

            if (op == "mul")
            {
                Code.AppendLine($"\t\tmul r15,r1,r3\t\t% Perform the {op} operation on the float values");
                Code.AppendLine($"\t\tadd r2,r2,r4\t\t% Add the point positions");
            }
            else
            {
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
            }

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

    private void IntegerWriteSubroutine()
    {
        Code.AppendLine($"\n\t\t%----------------- Write integer subroutine -----------------");

        Code.AppendLine($"\nintwrite\t\t nop\t\t% Start of the integer write subroutine");

        // Save the contents of r1, r2, r3 and r4
        Code.AppendLine($"\t\tsw -36(r14),r1\t\t% Save contents of r1");
        Code.AppendLine($"\t\tsw -40(r14),r2\t\t% Save contents of r2");
        Code.AppendLine($"\t\tsw -44(r14),r3\t\t% Save contents of r3");
        Code.AppendLine($"\t\tsw -48(r14),r4\t\t% Save contents of r4");

        // Save the return address
        Code.AppendLine($"\t\tsw -52(r14),r15\t\t% Save the return address");


        // Load the integer value
        Code.AppendLine($"\t\tlw r1,-28(r14)\t\t% Load the integer value");

        // Print the integer value
        Code.AppendLine($"\n\t\tsw -8(r14),r1\t\t% Store the integer value");
        Code.AppendLine($"\t\taddi r2,r0,buf\t\t% Load the buffer address");
        Code.AppendLine($"\t\tsw -12(r14),r2\t\t% Store the buffer address");
        Code.AppendLine($"\t\tjl r15,intstr\t\t% Call the int -> string subroutine");
        Code.AppendLine($"\t\tsw -8(r14),r13\t\t% Store the string address");
        Code.AppendLine($"\t\tjl r15,putstr\t\t% Call the print subroutine");

        // Print a newline
        Code.AppendLine($"\n\t\taddi r4,r0,nl\t\t% Load the newline");
        Code.AppendLine($"\t\tsw -8(r14),r4\t\t% Store the newline");
        Code.AppendLine($"\t\tjl r15,putstr\t\t% Call the print subroutine");

        // Restore the contents of r1, r2, r3 and r4
        Code.AppendLine($"\t\tlw r1,-36(r14)\t\t% Restore contents of r1");
        Code.AppendLine($"\t\tlw r2,-40(r14)\t\t% Restore contents of r2");
        Code.AppendLine($"\t\tlw r3,-44(r14)\t\t% Restore contents of r3");
        Code.AppendLine($"\t\tlw r4,-48(r14)\t\t% Restore contents of r4");

        // Restore the return address
        Code.AppendLine($"\t\tlw r15,-52(r14)\t\t% Restore the return address");

        // Return from the subroutine
        Code.AppendLine($"\t\tjr r15\t\t% Return from the integer write subroutine");
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
        Code.AppendLine($"\t\tsw -56(r14),r5\t\t% Save contents of r5");

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
        Code.AppendLine($"\n\t\tclt r2,r4,r0\t\t% Check if the fractional part is negative");
        Code.AppendLine($"\t\tbnz r2,negfrac\t\t% If the fractional part is negative, jump to negfrac");
        Code.AppendLine($"showfrac\t\tsw -8(r14),r4\t\t% Store the fractional part of the float value");
        
        // Get the length of the fractional part
        Code.AppendLine($"\t\taddi r2,r0,buf\t\t% Load the buffer address");
        Code.AppendLine($"\t\tsw -12(r14),r2\t\t% Store the buffer address");
        Code.AppendLine($"\t\tjl r15,intstr\t\t% Call the int -> string subroutine");
        Code.AppendLine($"\t\tsw -8(r14),r13\t\t% Store the string address");
        Code.AppendLine($"\t\tjl r15,lenstr\t\t% Call the length of string subroutine");
        Code.AppendLine($"\t\tlw r2,-32(r14)\t\t% Load the point position");
        Code.AppendLine($"\t\tsub r5,r2,r13\t\t% Calculate the length of the fractional part");

        // Calculate the number of zeros to add to the fractional part
        Code.AppendLine($"whileleadingzero\t\tcgti r2,r5,0\t\t% Load the point position");
        Code.AppendLine($"\t\tbz r2,endwhileleadingzero\t\t% If the length of the fractional part is not less than the point position, exit the loop");
        Code.AppendLine($"\t\t\taddi r2,r0,zero\t\t% Load the zero character");
        Code.AppendLine($"\t\t\tsw -8(r14),r2\t\t% Store the zero character");
        Code.AppendLine($"\t\t\tjl r15,putstr\t\t% Call the print subroutine");
        Code.AppendLine($"\t\t\tsubi r5,r5,1\t\t% Decrement the length of the fractional part");
        Code.AppendLine($"\t\t\tj whileleadingzero\t\t% Jump back to the start of the loop");
        
        Code.AppendLine($"endwhileleadingzero\n\t\tsw -8(r14),r4\t\t% Store the fractional part of the float value");
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
        Code.AppendLine($"\t\tlw r5,-56(r14)\t\t% Restore contents of r5");

        // Restore the return address
        Code.AppendLine($"\t\tlw r15,-52(r14)\t\t% Restore the return address");

        // Return from the subroutine
        Code.AppendLine($"\t\tjr r15\t\t% Return from the float write subroutine");

        // Negative fractional part of the float value
        Code.AppendLine($"\nnegfrac\t\tmuli r4,r4,-1\t\t% Make the fractional part positive");
        Code.AppendLine($"\t\tj showfrac\t\t% Jump to showfrac");
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
    






    #region Stack Frame Management

    public void AddFramePointer(ISymbolTable currentTable)
    {
        Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Add the frame pointer");
    }

    public void RemoveFramePointer(ISymbolTable currentTable)
    {
        Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Remove the frame pointer");
    }

    public void LoadDataMember(ISymbolTable currentTable, ISymbolTableEntry entry, ref bool isArray)
    {
        // Get the offset of the variable
        int offset = entry.Offset;

        // Check if the current table contains a class reference
        if (currentTable.Entries.Any(e => e.Kind == SymbolEntryKind.ClassAddress))
        {
            return;
        }

        if (isArray)
        {
            //WriteLine("Load Data Member: " + entry.Name + " with offset: " + offset);
            string reg = RegistersInUse.Pop();
            Code.AppendLine($"\t\taddi {reg},{reg},{offset}");
            RegistersInUse.Push(reg);
        }

        //WriteLine(currentTable.Name + " Load Data Member: " + entry.Name + " with offset: " + offset);
        Code.AppendLine("\t\taddi r14,r14," + offset + "\t\t% Load Data Member: " + entry.Name);

    }

    public void UnloadDataMember(ISymbolTable currentTable, int offset)
    {
        // Check if the current table contains a class reference
        if (currentTable.Entries.Any(e => e.Kind == SymbolEntryKind.ClassAddress))
        {
            return;
        }

        //WriteLine("Unload Data Member");
        Code.AppendLine("\n\t\tsubi r14,r14," + offset + "\t\t% Unload Data Member");
    }

    #endregion Stack Frame Management

    #region Function/Variable Declarations

    public void FunctionDeclaration(ISymbolTable currentTable)
    {
        Code.AppendLine($"\n\t\t%==================== Function/Method: {currentTable.Name} ====================\n");

        if (currentTable.Name == "main")
        {
            Code.AppendLine("entry\t\t% Start of the program");
            Code.AppendLine($"\t\taddi r14,r0,topaddr\t\t% Set the top of the stack");
            return;
        }

        int jumpOffset = currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.JumpAddress).First().Offset;

        string tagName = currentTable.Name;

        // Check if the function is a class method
        if (currentTable.Entries.Any(e => e.Kind == SymbolEntryKind.ClassAddress))
        {
            tagName += "_" + currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.ClassAddress).First().Type;

        }

        // Tag the function's call address
        Code.AppendLine($"{tagName}\t\tsw {jumpOffset}(r14),r15\t\t\t% Tag the function call address");

        if (currentTable.Name != "main")
            BufferSave(currentTable);

    }

    public void FunctionDeclarationEnd(ISymbolTable currentTable)
    {
        if (currentTable.Name == "main")
            Code.AppendLine("hlt\t\t% Halt the program\n");
        else
        {
            // Get the return address to jump back to
            int jumpOffset = currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.JumpAddress).First().Offset;

            BufferRestore(currentTable);

            // Jump back to the return address
            Code.AppendLine($"\t\tlw r15,{jumpOffset}(r14)\t\t\t% Jump back to the return address");
            Code.AppendLine($"\t\tjr r15");
        }


        Code.AppendLine($"\n\t\t%==================== End of {currentTable.Name} ====================\n");
    }

    public void VarDeclaration(ISymbolTable currentTable, ISymbolTableEntry entry)
    {
        // Get the offset of the variable
        int offset = entry.Offset;

        // Declare the variable in the stack frame
        Code.AppendLine($"\t\tsw {offset}(r14),r0\t\t% Declare the variable {entry.Name}");
    }

    #endregion Function/Variable Declarations

    #region Assignments

    public void AssignDataMember(ISymbolTable currentTable, ISymbolTableEntry? entry, string type, bool isArray = false)
    {
        //WriteLine(currentTable.Name + " Assign Data Member: " + type);



        if (type == "integer")
        {
            string valueLocReg = RegistersInUse.Pop();
            string locReg = RegistersInUse.Pop();

            // Check if the valueLocReg is a value or a memory location
            // Get the value to assign
            if (IsMemLoc(valueLocReg))
                Code.AppendLine($"\n\t\tlw {valueLocReg},0({valueLocReg})\t\t% Get the value to assign to the data member");
            Code.AppendLine($"\t\tsw 0({locReg}),{valueLocReg}\t\t% Assign Data Member\n");

            // Free the registers
            FreeRegister(valueLocReg);
            FreeRegister(locReg);
        }
        else if (type == "float")
        {
            string valueLocReg = RegistersInUse.Pop();
            string pointReg;
            string locReg = RegistersInUse.Pop();

            // Check if the valueLocReg is a value or a memory location
            if (IsMemLoc(valueLocReg))
            {
                pointReg = GetRegister();
                pointReg = RegistersInUse.Pop();
                Code.AppendLine($"\n\t\tlw {pointReg},-4({valueLocReg})\t\t% Get the point position of the float value");
                Code.AppendLine($"\n\t\tlw {valueLocReg},0({valueLocReg})\t\t% Get the value to assign to the data member");
            }
            else
            {
                pointReg = valueLocReg;
                valueLocReg = locReg;
                locReg = RegistersInUse.Pop();
            }

            // Store the value
            Code.AppendLine($"\t\tsw 0({locReg}),{valueLocReg}\t\t% Assign Data Member: Value");
            Code.AppendLine($"\t\tsw -4({locReg}),{pointReg}\t\t% Assign Data Member: Point Position\n");

            // Free the registers
            FreeRegister(pointReg);
            FreeRegister(valueLocReg);
            FreeRegister(locReg);
        }
        else
        {
            string valueLocReg = RegistersInUse.Pop();
            string locReg = RegistersInUse.Pop();

            //WriteLine("Return Value: " + valueLocReg);
            //WriteLine("Location: " + locReg);

            // Get the table of the class
            ISymbolTable classTable = currentTable.Entries.Where(e => e.Type == type).First().Link!;

            // Get all data members of the class
            List<ISymbolTableEntry> dataMembers = classTable.Entries.Where(e => e.Kind == SymbolEntryKind.Data).ToList();

            Code.AppendLine($"\n\t\t%----------------- Assign {classTable.Name} -----------------");

            int offset = 0;

            foreach (ISymbolTableEntry dataMember in dataMembers)
            {
                string valueReg = GetRegister();

                // Update the location register for both the value and the data member
                Code.AppendLine($"\n\t\taddi {locReg},{locReg},{offset}\t\t% Update the location register for the data member {dataMember.Name}");
                Code.AppendLine($"\t\taddi {valueLocReg},{valueLocReg},{offset}\t\t% Update the location register for the value {dataMember.Name}");

                // Load the value from the data member
                Code.AppendLine($"\t\tlw {valueReg},0({valueLocReg})\t\t% Load the value of the data member {dataMember.Name}");

                // Store the value
                Code.AppendLine($"\t\tsw 0({locReg}),{valueReg}\t\t% Assign Data Member: {dataMember.Name}");

                offset = -dataMember.Size;

                FreeRegister(RegistersInUse.Pop());
                //WriteLine("Data Member: " + dataMember.Name);
            }

            // Free the registers
            FreeRegister(valueLocReg);
            FreeRegister(locReg);
        }


        Code.AppendLine();

    }

    #endregion Assignments

    #region Load Literals/Variables

    public void LoadClassIndex(ISymbolTable currentTable, ISymbolTableEntry? entry, ref bool isArray)
    {
        // Return if the entry is null
        if (entry == null)
            return;

        // Return if the entry is not a class type
        if (entry.Type.Contains("integer") || entry.Type.Contains("float"))
            return;

        // Return if the entry is not an array
        if (!entry.Type.Contains('['))
            return;

        // Get the array size
        int size = GetDimSize(entry.Type);

        // Get the dimension count
        int dimCount = GetDimCount(entry.Type);

        // Get the element size
        int elementSize = entry.Size / size;

        // Get the dimensions of the array
        int[] dims = new int[dimCount];
        GetDims(entry.Type, dims);

        // NOTE: Objects are accessed by moving the stack pointer to the object's location
        // and then moving the stack pointer to the data member's location

        Stack<string> unloadRegs = new Stack<string>();

        for (int i = 0; i < dimCount; i++)
        {
            // Get the register the index register
            string indexReg = RegistersInUse.Pop();

            // Calculate the index offset
            Code.AppendLine($"\t\tmuli {indexReg},{indexReg},{elementSize}\t\t% Multiply the index by the element size");

            // Load the index value
            Code.AppendLine($"\t\tsub r14,r14,{indexReg}\t\t% Load the class reference {entry.Name} (r14)");

            // Update the element size to account for the next dimension
            elementSize *= dims[i];

            // Free the index register
            unloadRegs.Push(indexReg);
        }

        string locReg = GetRegister();

        // Load the class reference
        Code.AppendLine($"\n\t\taddi {locReg},r14,0\t\t% Load the class reference {entry.Name} (r14)");

        // Unload the array
        while (unloadRegs.Count > 0)
        {
            string reg = unloadRegs.Pop();
            Code.AppendLine($"\t\tadd r14,r14,{reg}\t\t% Unload the array");
            FreeRegister(reg);
        }

        isArray = true;

        //WriteLine("Load Class Index: " + entry.Name);
    }

    public void LoadVariableFromDataMember(ISymbolTable currentTable, ISymbolTableEntry entry, ref bool isArray)
    {
        //WriteLine("Load Variable From Data Member: " + entry.Name);
        //WriteLine("Type: " + entry.Type + "Is Array: " + isArray);

        // Get a register to store the location of the variable
        string locReg = GetRegister();

        // Check if the entry is a class data member
        if (currentTable.Entries.Any(e => e.Kind == SymbolEntryKind.ClassAddress) && !currentTable.Entries.Any(e => e.Name == entry.Name))
        {
            ISymbolTableEntry classRef = currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.ClassAddress).First();
            ISymbolTable classTable = classRef.Link!;

            // Load the class reference
            Code.AppendLine($"\n\t\tlw {locReg},{classRef.Offset}(r14)\t\t% Load the class reference {classRef.Name}");

            // Get the offset of the data member
            int offset = classTable.Entries.Where(e => e.Name == entry.Name).First().Offset;
            offset += classTable.ScopeSize;

            // Add the offset to the class reference
            Code.AppendLine($"\t\taddi {locReg},{locReg},{offset}\t\t% Load the location of the variable {entry.Name}: <|DATA|>");

            //WriteLine("Load Variable From Data Member: " + entry.Name);
            return;
        }

        if (isArray)
        {
            locReg = RegistersInUse.Pop();
            string indexReg = RegistersInUse.Pop();


            Code.AppendLine($"\n\t\taddi {locReg},{indexReg},0\t\t% Load the location of the variable {entry.Name} - r14 ");


            isArray = false;

            RegistersInUse.Push(locReg);
        }
        else
        {
            Code.AppendLine($"\n\t\taddi {locReg},r14,0\t\t% Load the location of the variable {entry.Name} (r14)");
            if (entry.Kind == SymbolEntryKind.Parameter && entry.Type.Contains('['))
            {
                Code.AppendLine($"\t\tlw {locReg},0({locReg})\t\t% Load the location of the variable {entry.Name} ");
            }
        }


        // Check if the variable is an array
        if (entry.Type.Contains('['))
        {
            // Get the array size
            int size = GetDimSize(entry.Type);

            // Get the dimension count
            int dimCount = GetDimCount(entry.Type);

            // Get the element size
            int elementSize = entry.Size / size;

            //WriteLine("Element Size: " + elementSize);

            if (elementSize<4)
                elementSize = 4;

            // Get the dimensions of the array
            int[] dims = new int[dimCount];
            GetDims(entry.Type, dims);

            // Get the register for the base address of the array
            string baseReg = RegistersInUse.Pop();

            if (RegistersInUse.Count < dimCount)
            {
                RegistersInUse.Push(baseReg);
                return;
            }

            for (int i = 0; i < dimCount; i++)
            {
                // Get the register the index register
                string indexReg = RegistersInUse.Pop();

                if (IsMemLoc(indexReg))
                    Code.AppendLine($"\t\tlw {indexReg},0({indexReg})\t\t% Load the index value");

                // Calculate the index offset
                Code.AppendLine($"\t\tmuli {indexReg},{indexReg},{elementSize}\t\t% Multiply the index by the element size");

                // Load the index value
                Code.AppendLine($"\t\tsub {baseReg},{baseReg},{indexReg}\t\t% Load memory location of the array {entry.Name} (r14)");

                // Update the element size to account for the next dimension
                elementSize *= dims[i];

                // Free the index register
                FreeRegister(indexReg);
            }

            // Add back the base address register
            RegistersInUse.Push(baseReg);
        }

    }

    public void LoadIntegerValue(string value)
    {
        string register = GetRegister();

        // Convert the value to an integer
        bool isValid = int.TryParse(value, out int intValue);

        if (!isValid)
        {
            //WriteLine("Invalid Integer Value: " + value);
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
                    Code.AppendLine($"\n\t\taddi {register},r0,{val}\t\t% Load the integer value {value} into {register}");
                else
                    Code.AppendLine($"\t\taddi {register},{register},{val}");

                if (i != 24)
                    Code.AppendLine($"\t\tsl {register},8");
            }
        else
            Code.AppendLine($"\n\t\taddi {register},r0,{intValue}\t\t% Load the integer value {value} into {register}");
    }

    public void LoadFloatValue(string value)
    {
        // Convert the value to a float
        bool isValid = float.TryParse(value, out float floatValue);

        if (!isValid)
        {
            //WriteLine("Invalid Float Value: " + value);
            floatValue = 0;
        }

        // Get the point position of the float
        int pointPos = value.Length - (value.IndexOf('.') + 1);
        value = value.Replace(".", "");

        LoadIntegerValue(value);

        // Get the register to store the point position
        string pointReg = GetRegister();

        // Load the point position into the register
        Code.AppendLine($"\t\taddi {pointReg},r0,{pointPos}\t\t% Load the point position of the float value {value} into {pointReg}");
    }

    #endregion Load Literals/Variables

    #region Function Calls/Returns

    public void FunctionCall(ISymbolTable currentTable, ISymbolTable functionTable, ref bool isArray, int? offset = null)
    {
        //WriteLine("Calling function: " + functionTable.Name + " from " + currentTable.Name);
        Code.AppendLine($"\n\t\t%----------------- Function Call: {currentTable.Name} -> {functionTable.Name} -----------------");

        // Get the current table scope size
        int currentScopeSize = offset == null ? currentTable.ScopeSize : currentTable.ScopeSize + offset.Value;

        // Get the parameters of the function
        List<ISymbolTableEntry> parameters = functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.Parameter).ToList();

        // Register to store the parameter address
        string paramReg;

        //Code.AppendLine($"{currentScopeSize}");
        parameters.Reverse();

        foreach (ISymbolTableEntry parameter in parameters)
        {
            // Get the parameter register
            paramReg = RegistersInUse.Pop();

            // Check if the param is a value (integer or float)
            // If it is a value, load the value into the register
            if (parameter.Type == "integer")
            {
                if (IsMemLoc(paramReg))
                    Code.AppendLine($"\t\tlw {paramReg},0({paramReg})\t\t% Load param {parameter.Name} value");
                Code.AppendLine($"\t\tsw {parameter.Offset - currentScopeSize}(r14),{paramReg}\t\t% Pass param {parameter.Name}");
            }
            else if (parameter.Type == "float")
            {
                string pointReg;

                if (IsMemLoc(paramReg))
                {
                    pointReg = GetRegister();
                    pointReg = RegistersInUse.Pop();
                    Code.AppendLine($"\t\tlw {pointReg},-4({paramReg})\t\t% Get the point position of the float value");
                    Code.AppendLine($"\t\tlw {paramReg},0({paramReg})\t\t% Load param {parameter.Name} value");
                }
                else
                {
                    pointReg = paramReg;
                    paramReg = RegistersInUse.Pop();
                }


                // Load the float value
                Code.AppendLine($"\t\tsw {parameter.Offset - currentScopeSize}(r14),{paramReg}\t\t% Pass param {parameter.Name}");
                Code.AppendLine($"\t\tsw {parameter.Offset - currentScopeSize - 4}(r14),{pointReg}\t\t% Pass param {parameter.Name} point position");

                // Free the value register
                FreeRegister(pointReg);
            }
            else
                Code.AppendLine($"\t\tsw {parameter.Offset - currentScopeSize}(r14),{paramReg}\t\t% Pass param {parameter.Name} reference");

            // Free the parameter register
            FreeRegister(paramReg);
        }

        // Set the this pointer to the current object
        if (isArray)
        {
            string indexReg = RegistersInUse.Pop();

            functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.ClassAddress).ToList().ForEach(e => Code.AppendLine($"\t\tsw {e.Offset - currentScopeSize}(r14),{indexReg}\t\t% Set the pointer to the current class instance"));

            isArray = false;
            FreeRegister(indexReg);
        }
        else
        {
            functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.ClassAddress).ToList().ForEach(e => Code.AppendLine($"\t\tsw {e.Offset - currentScopeSize}(r14),r14\t\t% Set the pointer to the current class instance"));
        }



        // Load the function stack frame
        Code.AppendLine($"\n\t\taddi r14,r14,-{currentScopeSize}\t\t% Load the function stack frame");

        string tagName = functionTable.Name;

        // Check if the function is a class method
        if (functionTable.Entries.Any(e => e.Kind == SymbolEntryKind.ClassAddress))
        {
            tagName += "_" + functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.ClassAddress).First().Type;
        }

        // Jump to the function
        Code.AppendLine($"\t\tjl r15,{tagName}\t\t% Jump to the function {tagName}");

        // Restore the stack frame
        Code.AppendLine($"\t\taddi r14,r14,{currentScopeSize}\t\t% Restore the stack frame");

        // If the function returns a value, get the return value
        if (functionTable.Entries.Any(e => e.Kind == SymbolEntryKind.ReturnVal && e.Type != "void"))
        {
            // Get the return type  
            string returnType = functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Type;

            if (returnType == "integer")
            {
                string returnValReg = GetRegister();

                // Get the return value
                Code.AppendLine($"\t\tlw {returnValReg},{functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset - currentScopeSize}(r14)\t\t% Get the return value");
                Code.AppendLine($"\t\taddi {returnValReg},{returnValReg},0");
            }
            else if (returnType == "float")
            {
                string returnValReg = GetRegister();
                string pointReg = GetRegister();

                // Get the return value
                Code.AppendLine($"\t\tlw {returnValReg},{functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset - currentScopeSize}(r14)\t\t% Get the return value");
                Code.AppendLine($"\t\tlw {pointReg},{functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset - currentScopeSize - 4}(r14)\t\t% Get the point position");

                Code.AppendLine($"\t\taddi {returnValReg},{returnValReg},0");
                Code.AppendLine($"\t\taddi {pointReg},{pointReg},0");
            }
            else
            {
                string returnValReg = GetRegister();

                // Get the return value
                Code.AppendLine($"\t\tlw {returnValReg},{functionTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset - currentScopeSize}(r14)\t\t% Get the return value");
            }
        }
    }

    public void Return(ISymbolTable currentTable, string type)
    {
        switch (type)
        {
            case "integer":
                // Returns an integer value
                // Get the return value register
                string returnIntReg = RegistersInUse.Pop();

                // Make sure the register is not a memory location
                if (IsMemLoc(returnIntReg))
                    Code.AppendLine($"\t\tlw {returnIntReg},0({returnIntReg})\t\t% Load the value of {returnIntReg}");

                // Store the return value
                Code.AppendLine($"\t\tsw {currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset}(r14),{returnIntReg}\t\t% Store the return value");

                FreeRegister(returnIntReg);
                break;
            case "float":
                // Returns a float value
                // Get the return value register
                string returnFloatReg = RegistersInUse.Pop();
                string pointReg;

                // Make sure the register is not a memory location
                if (IsMemLoc(returnFloatReg))
                {
                    pointReg = GetRegister();
                    pointReg = RegistersInUse.Pop();

                    Code.AppendLine($"\t\tlw {pointReg},-4({returnFloatReg})\t\t% Load the point position of the float value");
                    Code.AppendLine($"\t\tlw {returnFloatReg},0({returnFloatReg})\t\t% Load the value of {returnFloatReg}");
                }
                else
                    pointReg = RegistersInUse.Pop();

                // Store the return value
                Code.AppendLine($"\t\tsw {currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset}(r14),{returnFloatReg}\t\t% Store the return value");
                Code.AppendLine($"\t\tsw {currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset - 4}(r14),{pointReg}\t\t% Store the point position of the float value");

                FreeRegister(pointReg);
                FreeRegister(returnFloatReg);

                break;
            default:
                // Returns a memory location (reference)
                // Get the return value register
                string returnReg = RegistersInUse.Pop();

                // Store the return value
                Code.AppendLine($"\t\tsw {currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.ReturnVal).First().Offset}(r14),{returnReg}\t\t% Store the return value");

                FreeRegister(returnReg);
                break;
        }
    }

    #endregion Function Calls/Returns

    #region Read/Write

    public void Write(ISymbolTable currentTable, string type)
    {
        if (type == "integer")
        {
            Code.AppendLine($"\n\t\t%----------------- WRITE Integer -----------------");

            // Go to the next stack frame
            Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

            // Get the integer value register
            string valueReg = RegistersInUse.Pop();

            // Check if the valueLocReg is a value or a memory location
            if (IsMemLoc(valueReg))
                Code.AppendLine($"\t\tlw {valueReg},0({valueReg})\t\t% Get the integer value to write");

            // Write the value to the console/screen
            Code.AppendLine($"\t\tsw -28(r14),{valueReg}");
            Code.AppendLine($"\t\tjl r15,intwrite\t\t% Call the integer write subroutine");

            // Free the value register
            FreeRegister(valueReg);

            // Go back to the current stack frame
            Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Move back to the current stack frame\n");
        }
        else if (type == "float")
        {
            string valueLocReg = RegistersInUse.Pop();
            string pointReg;

            if (IsMemLoc(valueLocReg))
            {
                pointReg = GetRegister();
                pointReg = RegistersInUse.Pop();
                Code.AppendLine($"\n\t\tlw {pointReg},-4({valueLocReg})\t\t% Load the point position of the float value to write");
                Code.AppendLine($"\t\tlw {valueLocReg},0({valueLocReg})\t\t% Load the float value to write");
            }
            else
            {
                pointReg = valueLocReg;
                valueLocReg = RegistersInUse.Pop();
            }

            Code.AppendLine($"\n\t\t%----------------- WRITE Float -----------------");

            // Go to the next stack frame
            Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

            Code.AppendLine($"\t\tsw -28(r14),{valueLocReg}\t\t% Store the float value");
            Code.AppendLine($"\t\tsw -32(r14),{pointReg}\t\t% Store the point position");

            // Call the float write subroutine
            Code.AppendLine($"\t\tjl r15,floatwrite\t\t% Call the float write subroutine");

            // Go back to the current stack frame
            Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Move back to the current stack frame\n");

            // Free the value register
            FreeRegister(valueLocReg);
            FreeRegister(pointReg);


        }

    }

    public void Read(ISymbolTable currentTable, string type)
    {
        if (type == "integer")
        {
            Code.AppendLine($"\n\t\t%----------------- READ Integer -----------------");

            // Go to the next stack frame
            Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

            // Prompt the user for an integer
            Code.AppendLine($"\t\taddi r2,r0,entint\t\t% Prompt for an integer");
            Code.AppendLine($"\t\tsw -8(r14),r2");
            Code.AppendLine($"\t\tjl r15,putstr");

            // Get the integer from the user
            Code.AppendLine($"\t\taddi r2,r0,buf");
            Code.AppendLine($"\t\tsw -8(r14),r2");
            Code.AppendLine($"\t\tjl r15,getstr\t\t% Call the get string subroutine");
            Code.AppendLine($"\t\tjl r15,strint\t\t% Call the string -> int subroutine");

            // Get the integer value register
            string valueReg = GetRegister();

            // Store the integer value
            Code.AppendLine($"\t\taddi {valueReg},r13,0");

            // Restore the memory location
            Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Move back to the current stack frame\n");

            // Store the value in the variable
            AssignDataMember(currentTable, null, "integer");
        }
        else
        {
            Code.AppendLine($"\n\t\t%----------------- READ Float -----------------");

            // Go to the next stack frame
            Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

            // Prompt the user for a float
            Code.AppendLine($"\t\taddi r2,r0,entfloat\t\t% Prompt for a float");
            Code.AppendLine($"\t\tsw -8(r14),r2");
            Code.AppendLine($"\t\tjl r15,putstr");

            // Store the buffer address
            Code.AppendLine($"\t\taddi r2,r0,buf");
            Code.AppendLine($"\t\tsw -8(r14),r2");

            // Get the float from the user
            Code.AppendLine($"\t\tjl r15,getfloat\t\t% Call the float read subroutine");

            // Get registers to store the float value and the point position
            string valueReg = GetRegister();
            string pointReg = GetRegister();

            // Get the integer and point position from the stack
            Code.AppendLine($"\t\tlw {valueReg},-60(r14)\t\t% Load the integer part of the float value");
            Code.AppendLine($"\t\tlw {pointReg},-56(r14)\t\t% Load the point position of the float value");

            Code.AppendLine($"\t\taddi {valueReg},{valueReg},0");
            Code.AppendLine($"\t\taddi {pointReg},{pointReg},0");

            // Restore the memory location
            Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Move back to the current stack frame\n");

            // Store the value in the variable
            AssignDataMember(currentTable, null, "float");
        }
    }

    #endregion Read/Write

    #region Helper Methods

    private bool IsMemLoc(string valueLocReg)
    {
        string line = Code.ToString().Split("\n").ToList().Where(l => l.Contains(valueLocReg)).Last();

        return line.Contains("r14") || line.Contains("<|DATA|>");
    }

    private void BufferSave(ISymbolTable currentTable)
    {
        Code.AppendLine($"\n\t\t%----------------- Save Buffer -----------------");

        // Get the buffer offset
        int bufferOffset = currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.Buffer).First().Offset;

        for (int i = 1; i < 16; i++)
        {
            Code.AppendLine($"\t\tsw {bufferOffset - (i - 1) * 4}(r14),r{i}\t\t% Save buffer register r{i}");
        }
    }

    private void BufferRestore(ISymbolTable currentTable)
    {
        Code.AppendLine($"\n\t\t%----------------- Restore Buffer -----------------");

        // Get the buffer offset
        int bufferOffset = currentTable.Entries.Where(e => e.Kind == SymbolEntryKind.Buffer).First().Offset;

        for (int i = 1; i < 16; i++)
        {
            Code.AppendLine($"\t\tlw r{i},{bufferOffset - (i - 1) * 4}(r14)\t\t% Save buffer register r{i}");
        }
    }

    #endregion Helper Methods

    #region Expressions

    public void AddExpression(ISymbolTable currentTable, string operation, string type)
    {
        ///WriteLine("Add Expression: " + operation + " Type: " + type);

        string op = operation switch
        {
            "+" => "add",
            "-" => "sub",
            "|" => "or",
            _ => "add"
        };


        if (type == "integer")
        {
            // Get the registers
            string reg2 = RegistersInUse.Pop();
            string reg1 = RegistersInUse.Pop();
            string resultReg = GetRegister();

            // Make sure the registers are not memory locations
            if (IsMemLoc(reg1))
                Code.AppendLine($"\t\tlw {reg1},0({reg1})\t\t% Load the value of {reg1}");
            if (IsMemLoc(reg2))
                Code.AppendLine($"\t\tlw {reg2},0({reg2})\t\t% Load the value of {reg2}");


            if (op == "or")
            {
                // Check how many times the zero subroutine has been declared
                int nonZeroSubroutineCount = Code.ToString().Split("endOr").Length / 2 + 1;
                string nonZeroSubroutine = $"nonzero{nonZeroSubroutineCount}";
                string endOrSubroutine = $"endOr{nonZeroSubroutineCount}";

                Code.AppendLine($"\n\t\tbnz {reg1},{nonZeroSubroutine}\t\t% Check if {reg1} is not zero");
                Code.AppendLine($"\t\tbnz {reg2},{nonZeroSubroutine}\t\t% Check if {reg2} is not zero");
                Code.AppendLine($"\t\taddi {reg1},r0,0\t\t% {reg1} {operation} {reg2} = {reg1}");
                Code.AppendLine($"\t\tj {endOrSubroutine}\t\t% Jump to the end of the {operation} subroutine");
                Code.AppendLine($"{nonZeroSubroutine}\t\t addi {reg1},r0,1\t\t% Set the result to 1");
                Code.AppendLine($"{endOrSubroutine}\t\t nop\t\t% End of the {operation} subroutine");
            }
            else
            {
                // Calculate the result
                Code.AppendLine($"\t\t{op} {resultReg},{reg1},{reg2}\t\t% {operation} the values");
            }

            // Free the registers that are not used anymore
            FreeRegister(reg1);
            FreeRegister(reg2);

        }
        else if (type == "float")
        {
            Code.AppendLine($"\n\t\t%----------------- Add Float Expression: {operation} -----------------");

            string valueReg = RegistersInUse.Pop();
            string pointReg;

            // Make sure the register is not a memory location
            if (IsMemLoc(valueReg))
            {
                pointReg = GetRegister();
                pointReg = RegistersInUse.Pop();
                Code.AppendLine($"\t\tlw {pointReg},-4({valueReg})\t\t% Get the point position of the float value");
                Code.AppendLine($"\t\tlw {valueReg},0({valueReg})\t\t% Get the value");
            }
            else
            {
                pointReg = valueReg;
                valueReg = RegistersInUse.Pop();
            }

            string valueReg2 = RegistersInUse.Pop();
            string pointReg2;

            // Make sure the register is not a memory location
            if (IsMemLoc(valueReg2))
            {
                pointReg2 = GetRegister();
                pointReg2 = RegistersInUse.Pop();
                Code.AppendLine($"\t\tlw {pointReg2},-4({valueReg2})\t\t% Get the point position of the float value");
                Code.AppendLine($"\t\tlw {valueReg2},0({valueReg2})\t\t% Get the value to negate");
            }
            else
            {
                pointReg2 = valueReg2;
                valueReg2 = RegistersInUse.Pop();
            }

            // Move to the next stack frame
            Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

            // Store the float values
            Code.AppendLine($"\t\tsw 0(r14),{valueReg2}\t\t% Store the float value");
            Code.AppendLine($"\t\tsw -4(r14),{pointReg2}\t\t% Store the point position");
            Code.AppendLine($"\t\tsw -8(r14),{valueReg}\t\t% Store the float value");
            Code.AppendLine($"\t\tsw -12(r14),{pointReg}\t\t% Store the point position");

            // Call the float compare subroutine
            Code.AppendLine($"\t\tjl r15,{op}float\t\t% Call the float compare subroutine");

            // Free the registers
            FreeRegister(valueReg);
            FreeRegister(pointReg);
            FreeRegister(valueReg2);
            FreeRegister(pointReg2);

            // Get the result
            string resultReg = GetRegister();
            string resultPointReg = GetRegister();

            // Load the result
            Code.AppendLine($"\t\tlw {resultReg},0(r14)\t\t% Load the result");
            Code.AppendLine($"\t\tlw {resultPointReg},-4(r14)\t\t% Load the point position");

            Code.AppendLine($"\t\taddi {resultReg},{resultReg},0");
            Code.AppendLine($"\t\taddi {resultPointReg},{resultPointReg},0");

            // Move back to the current stack frame
            Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Move back to the current stack frame\n");
        }
    }

    public void MultExpression(ISymbolTable currentTable, string operation, string type)
    {
        //WriteLine("Mult Expression: " + operation + " Type: " + type);

        string op = operation switch
        {
            "*" => "mul",
            "/" => "div",
            "&" => "and",
            _ => "muli"
        };

        if (type == "integer")
        {
            // Get the registers
            string reg2 = RegistersInUse.Pop();
            string reg1 = RegistersInUse.Pop();
            string resultReg = GetRegister();

            // Make sure the registers are not memory locations
            if (IsMemLoc(reg1))
                Code.AppendLine($"\t\tlw {reg1},0({reg1})\t\t% Load the value of {reg1}");
            if (IsMemLoc(reg2))
                Code.AppendLine($"\t\tlw {reg2},0({reg2})\t\t% Load the value of {reg2}");


            if (op == "and")
            {
                // Check how many times the zero subroutine has been declared
                int zeroSubroutineCount = Code.ToString().Split("endAnd").Length / 2 + 1;
                string zeroSubroutine = $"zero{zeroSubroutineCount}";
                string endAndSubroutine = $"endAnd{zeroSubroutineCount}";

                Code.AppendLine($"\t\tbz {reg1},{zeroSubroutine}\t\t% Check if {reg1} is zero");
                Code.AppendLine($"\t\tbz {reg2},{zeroSubroutine}\t\t% Check if {reg2} is zero");
                Code.AppendLine($"\t\taddi {resultReg},r0,1\t\t% {reg1} {operation} {reg2} = {resultReg}");
                Code.AppendLine($"\t\tj {endAndSubroutine}\t\t% Jump to the end of the {operation} subroutine");
                Code.AppendLine($"{zeroSubroutine}\t\t addi {resultReg},r0,0\t\t% Set the result to 0");
                Code.AppendLine($"{endAndSubroutine}\t\t nop\t\t% End of the {operation} subroutine");
            }
            else
            {
                // Calculate the result
                Code.AppendLine($"\t\t{op} {resultReg},{reg1},{reg2}\t\t% {operation} the values");
            }

            // Free the registers that are not used anymore
            FreeRegister(reg1);
            FreeRegister(reg2);
        }
        else if (type == "float")
        {
            Code.AppendLine($"\n\t\t%----------------- Mult Float Expression: {operation} -----------------");

            string valueReg = RegistersInUse.Pop();
            string pointReg;

            // Make sure the register is not a memory location
            if (IsMemLoc(valueReg))
            {
                pointReg = GetRegister();
                pointReg = RegistersInUse.Pop();
                Code.AppendLine($"\t\tlw {pointReg},-4({valueReg})\t\t% Get the point position of the float value");
                Code.AppendLine($"\t\tlw {valueReg},0({valueReg})\t\t% Get the value to negate");
            }
            else
            {
                pointReg = valueReg;
                valueReg = RegistersInUse.Pop();
            }

            string valueReg2 = RegistersInUse.Pop();
            string pointReg2;

            // Make sure the register is not a memory location
            if (IsMemLoc(valueReg2))
            {
                pointReg2 = GetRegister();
                pointReg2 = RegistersInUse.Pop();
                Code.AppendLine($"\t\tlw {pointReg2},-4({valueReg2})\t\t% Get the point position of the float value");
                Code.AppendLine($"\t\tlw {valueReg2},0({valueReg2})\t\t% Get the value to negate");
            }
            else
            {
                pointReg2 = valueReg2;
                valueReg2 = RegistersInUse.Pop();
            }

            // Move to the next stack frame
            Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

            // Store the float values
            Code.AppendLine($"\t\tsw 0(r14),{valueReg2}\t\t% Store the float value");
            Code.AppendLine($"\t\tsw -4(r14),{pointReg2}\t\t% Store the point position");
            Code.AppendLine($"\t\tsw -8(r14),{valueReg}\t\t% Store the float value");
            Code.AppendLine($"\t\tsw -12(r14),{pointReg}\t\t% Store the point position");

            // Call the float compare subroutine
            Code.AppendLine($"\t\tjl r15,{op}float\t\t% Call the float mult subroutine");

            // Free the registers
            FreeRegister(valueReg);
            FreeRegister(pointReg);
            FreeRegister(valueReg2);
            FreeRegister(pointReg2);

            // Get the result
            string resultReg = GetRegister();
            string resultPointReg = GetRegister();

            // Load the result
            Code.AppendLine($"\t\tlw {resultReg},0(r14)\t\t% Load the result");
            Code.AppendLine($"\t\tlw {resultPointReg},-4(r14)\t\t% Load the point position");

            Code.AppendLine($"\t\taddi {resultReg},{resultReg},0");
            Code.AppendLine($"\t\taddi {resultPointReg},{resultPointReg},0");

            // Move back to the current stack frame
            Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Move back to the current stack frame\n");
        }
    }

    public void RelExpression(ISymbolTable currentTable, string operation, string type)
    {
        string op = operation switch
        {
            "<" => "clt",
            "<=" => "cle",
            ">" => "cgt",
            ">=" => "cge",
            "==" => "ceq",
            "<>" => "cne",
            _ => "ceq"
        };

        if (type == "integer")
        {
            // Get the registers
            string reg2 = RegistersInUse.Pop();
            string reg1 = RegistersInUse.Pop();
            string resultReg = GetRegister();

            // Make sure the registers are not memory locations
            if (IsMemLoc(reg1))
                Code.AppendLine($"\t\tlw {reg1},0({reg1})\t\t% Load the value of {reg1}");
            if (IsMemLoc(reg2))
                Code.AppendLine($"\t\tlw {reg2},0({reg2})\t\t% Load the value of {reg2}");

            // Compare the values
            Code.AppendLine($"\t\t{op} {resultReg},{reg1},{reg2}\t\t% {operation} the values");

            // Free the registers that are not used anymore
            FreeRegister(reg1);
            FreeRegister(reg2);
        }
        else if (type == "float")
        {
            Code.AppendLine($"\n\t\t%----------------- Rel Float Expression: {operation} -----------------");

            string valueReg = RegistersInUse.Pop();
            string pointReg;

            // Make sure the register is not a memory location
            if (IsMemLoc(valueReg))
            {
                pointReg = GetRegister();
                pointReg = RegistersInUse.Pop();
                Code.AppendLine($"\t\tlw {pointReg},-4({valueReg})\t\t% Get the point position of the float value");
                Code.AppendLine($"\t\tlw {valueReg},0({valueReg})\t\t% Get the value to negate");
            }
            else
            {
                pointReg = valueReg;
                valueReg = RegistersInUse.Pop();
            }

            string valueReg2 = RegistersInUse.Pop();
            string pointReg2;

            // Make sure the register is not a memory location
            if (IsMemLoc(valueReg2))
            {
                pointReg2 = GetRegister();
                pointReg2 = RegistersInUse.Pop();
                Code.AppendLine($"\t\tlw {pointReg2},-4({valueReg2})\t\t% Get the point position of the float value");
                Code.AppendLine($"\t\tlw {valueReg2},0({valueReg2})\t\t% Get the value to negate");
            }
            else
            {
                pointReg2 = valueReg2;
                valueReg2 = RegistersInUse.Pop();
            }

            // Move to the next stack frame
            Code.AppendLine($"\t\taddi r14,r14,-{currentTable.ScopeSize}\t\t% Move to the next stack frame");

            // Store the float values
            Code.AppendLine($"\t\tsw 0(r14),{valueReg2}\t\t% Store the float value");
            Code.AppendLine($"\t\tsw -4(r14),{pointReg2}\t\t% Store the point position");
            Code.AppendLine($"\t\tsw -8(r14),{valueReg}\t\t% Store the float value");
            Code.AppendLine($"\t\tsw -12(r14),{pointReg}\t\t% Store the point position");

            // Call the float compare subroutine
            Code.AppendLine($"\t\tjl r15,{op}float\t\t% Call the float compare subroutine");

            // Get the result
            string resultReg = GetRegister();
            string resultPointReg = GetRegister();

            // Load the result
            Code.AppendLine($"\t\tlw {resultReg},0(r14)\t\t% Load the result");
            Code.AppendLine($"\t\tlw {resultPointReg},-4(r14)\t\t% Load the point position");

            Code.AppendLine($"\t\taddi {resultReg},{resultReg},0");
            Code.AppendLine($"\t\taddi {resultPointReg},{resultPointReg},0");

            // Free the registers
            FreeRegister(valueReg);
            FreeRegister(pointReg);
            FreeRegister(valueReg2);
            FreeRegister(pointReg2);

            // Move back to the current stack frame
            Code.AppendLine($"\t\taddi r14,r14,{currentTable.ScopeSize}\t\t% Move back to the current stack frame\n");
        }
    }

    public void NotExpression(ISymbolTable currentTable, string type)
    {
        //WriteLine("Not Expression");

        if (type == "integer")
        {
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
        else if (type == "float")
        {

        }
    }

    public void NegExpression(ISymbolTable currentTable, string type)
    {
        if (type == "integer")
        {
            // Get the register
            string reg = RegistersInUse.Pop();

            // Make sure the register is not a memory location
            if (IsMemLoc(reg))
                Code.AppendLine($"\t\tlw {reg},0({reg})\t\t% Load the value of {reg}");

            // Negate the value
            Code.AppendLine($"\t\tmuli {reg},{reg},-1\t\t% Negate the value");

            // Add the register back to the stack
            RegistersInUse.Push(reg);
        }
        else if (type == "float")
        {
            string valueReg = RegistersInUse.Pop();
            string pointReg;

            // Make sure the register is not a memory location
            if (IsMemLoc(valueReg))
            {
                pointReg = GetRegister();
                pointReg = RegistersInUse.Pop();
                Code.AppendLine($"\t\tlw {pointReg},-4({valueReg})\t\t% Get the point position of the float value");
                Code.AppendLine($"\t\tlw {valueReg},0({valueReg})\t\t% Get the value to negate");
            }
            else
            {
                pointReg = valueReg;
                valueReg = RegistersInUse.Pop();
            }

            // Negate the value
            Code.AppendLine($"\t\tmuli {valueReg},{valueReg},-1\t\t% Negate the value");

            // Add the register back to the stack
            RegistersInUse.Push(valueReg);
            RegistersInUse.Push(pointReg);
        }
    }

    #endregion Expressions

    #region While Loop

    public void While(ISymbolTable currentTable, ref int whileCount)
    {
        string endWhileLabel = $"endwhile{whileCount}";

        string conditionReg = RegistersInUse.Pop();

        Code.AppendLine($"\t\tbz {conditionReg},{endWhileLabel}\t\t% Check the while condition");

        FreeRegister(conditionReg);
    }
    public void WhileCond(ISymbolTable currentTable, ref int whileCount)
    {
        whileCount = Code.ToString().Split("gowhile").Length;

        string goWhileLabel = $"gowhile{whileCount}";

        Code.AppendLine($"{goWhileLabel}\t\t nop\t\t% Go to the while loop");
    }
    public void EndWhile(ISymbolTable currentTable, ref int whileCount)
    {
        string goWhileLabel = $"gowhile{whileCount}";
        string endWhileLabel = $"endwhile{whileCount}";

        Code.AppendLine($"\t\tj {goWhileLabel}\t\t% Go to the while loop");
        Code.AppendLine($"{endWhileLabel}\t\t nop\t\t% End of the while loop");
    }

    #endregion

    #region If Statement

    public void If(ISymbolTable currentTable, ref int ifCount)
    {
        string conditionReg = RegistersInUse.Pop();

        ifCount = Code.ToString().Split("ifthen").Length;

        string ifThenLabel = $"ifthen{ifCount}";
        string elseLabel = $"else{ifCount}";

        Code.AppendLine($"{ifThenLabel}\t\tbz {conditionReg},{elseLabel}\t\t% Check the if condition");

        FreeRegister(conditionReg);
    }
    public void Else(ISymbolTable currentTable, ref int ifCount)
    {
        string endIfLabel = $"endif{ifCount}";
        string elseLabel = $"else{ifCount}";

        Code.AppendLine($"\t\tj {endIfLabel}\t\t% Jump to the end of the if statement");
        Code.AppendLine($"{elseLabel}\t\t nop\t\t% Else statement");
    }

    public void EndIf(ISymbolTable currentTable, ref int ifCount)
    {
        string endIfLabel = $"endif{ifCount}";
        Code.AppendLine($"{endIfLabel}\t\t nop\t\t% End of the if statement");
    }

    #endregion If Statement

    public void Subroutines()
    {
        //WriteLine("Subroutines");
    }

}
