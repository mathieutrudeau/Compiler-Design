using static System.Console;



using System.Diagnostics;
using SemanticAnalyzer;
using AbstractSyntaxTreeGeneration;
using SyntacticAnalyzer;
using CodeGenerator;

namespace Compiler_Design;

class Program
{
    static void Main(string[] args)
    {
        foreach (string arg in args)
        {
            CompileSourceFile(arg);
        }

        //GenerateTokens("ScannerTestFiles");
        //ParseFile("ParserTestFiles");
        //GenerateSymbolTable("SymbolTableTestFiles");

        //GenerateCode("CodeGeneratorTestFiles");
    }


    static void CompileSourceFile(string sourceFile)
    {

        // Check if the file exists
        if (!System.IO.File.Exists(sourceFile))
        {
            WriteLine("File does not exist: " + sourceFile);
            return;
        }

        WriteLine("=========================================");
        WriteLine("Compiling the following src file: " + sourceFile);
        WriteLine("=========================================");

        // Start by parsing the file
        IParser parser = new Parser(sourceFile);
        bool parseSuccess = parser.Parse();

        // Check if the parse was successful
        if (!parseSuccess)
        {
            WriteLine("Failed to parse file: " + sourceFile);
            return;
        }

        // Get the AST root
        IASTNode root = parser.GetAST_Root();

        // Run the Semantic Analyzer
        ISemanticAnalyzer semanticAnalyzer = new SemanticAnalyzer.SemanticAnalyzer(root, sourceFile);

        // Check if the semantic analysis was successful
        if (!semanticAnalyzer.Analyze())
        {
            WriteLine("Failed to build Symbol Table for file: " + sourceFile);
            return;
        }

        // Run the Code Generator
        ICodeGenerator codeGenerator = new CodeGenerator.CodeGenerator(root, semanticAnalyzer.GetGlobalSymbolTable(), sourceFile);

        // Generate the code
        codeGenerator.GenerateCode();
    }
}
