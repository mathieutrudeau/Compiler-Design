using AbstractSyntaxTreeGeneration;
using SyntacticAnalyzer;
using static System.Console;

namespace SemanticAnalyzer;


public static class SymbolTableDriver
{
    
    public static void GenerateSymbolTable(string sourceFolder)
    {
        // Get all the test files
        string[] testFiles = Directory.GetFiles(sourceFolder, "bubblesort.src");

        // Run the semantic analyzer on each file
        foreach (string testFile in testFiles)
        {
            WriteLine("=========================================");
            WriteLine("Building Symbol Table for file: " + testFile);
            WriteLine("=========================================");

            // Start by parsing the file
            IParser parser = new Parser(testFile);
            bool parseSuccess = parser.Parse();

            // If the parse was successful, build the symbol table
            if (parseSuccess)
            {
                IASTNode root = parser.GetAST_Root();
                ISemanticAnalyzer semanticAnalyzer = new SemanticAnalyzer(root, testFile);
                bool analyzeSuccess = semanticAnalyzer.Analyze();


                if (analyzeSuccess)
                {
                    WriteLine("Symbol Table built successfully.");
                    WriteLine(semanticAnalyzer.GetGlobalSymbolTable().ToString());
                }
                else
                    WriteLine("Failed to build Symbol Table for file: " + testFile);
            }
            else
                WriteLine("Failed to parse file: " + testFile);
        }
    }
}