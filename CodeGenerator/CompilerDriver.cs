using AbstractSyntaxTreeGeneration;
using SemanticAnalyzer;
using SyntacticAnalyzer;
using static System.Console;

namespace CodeGenerator;


public static class CompilerDriver
{
    
    public static void GenerateCode(string sourceFolder)
    {
        // Get all the test files
        string[] testFiles = Directory.GetFiles(sourceFolder, "example-bubblesort.src");

        // Run the Code Generator on each file
        foreach (string testFile in testFiles)
        {
            WriteLine("=========================================");
            WriteLine("Compiling the following src file: " + testFile);
            WriteLine("=========================================");

            // Start by parsing the file
            IParser parser = new Parser(testFile);
            bool parseSuccess = parser.Parse();

            // Check if the parse was successful
            if(!parseSuccess)
            {
                WriteLine("Failed to parse file: " + testFile);
                continue;
            }

            // Get the AST root
            IASTNode root = parser.GetAST_Root();

            // Run the Semantic Analyzer
            ISemanticAnalyzer semanticAnalyzer = new SemanticAnalyzer.SemanticAnalyzer(root, testFile);

            // Check if the semantic analysis was successful
            if(!semanticAnalyzer.Analyze())
            {
                WriteLine("Failed to build Symbol Table for file: " + testFile);
                continue;
            }

            // Run the Code Generator
            ICodeGenerator codeGenerator = new CodeGenerator(root, testFile);

            // Generate the code
            codeGenerator.GenerateCode();
        }
    }
}