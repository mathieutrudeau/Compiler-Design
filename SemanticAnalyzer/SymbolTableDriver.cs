using static System.Console;

namespace SemanticAnalyzer;


public static class SymbolTableDriver
{
    
    public static void GenerateSymbolTable(string sourceFolder)
    {
        // Get all the test files
        string[] testFiles = Directory.GetFiles(sourceFolder, "buubblesort.src");

        // Run the semantic analyzer on each file
        foreach (string testFile in testFiles)
        {
            WriteLine("=========================================");
            WriteLine("Building Symbol Table for file: " + testFile);
            WriteLine("=========================================");

            
        }
    }
}