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

            if (parser.Parse())
            {
                IASTNode root = parser.GetAST_Root();
                WriteLine("Parsing successful.");
                WriteLine(root.ToString());
            }
            else
            {
                WriteLine("Failed to parse file: " + testFile);
                continue;
            }
        }
    }
}