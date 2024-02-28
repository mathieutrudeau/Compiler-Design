using static System.Console;
using LexicalAnalyzer;

namespace SyntacticAnalyzer;

/// <summary>
/// Driver class for the parser.
/// </summary>
public static class ParserDriver
{
    /// <summary>
    /// Parses a set of test files.
    /// </summary>
    public static void ParseFile(string sourceFolder)
    {
        // Get all the test files
        string[] testFiles = Directory.GetFiles(sourceFolder, "*.src");

        // Run the parser on each file
        foreach (string testFile in testFiles)
        {
            WriteLine("=========================================");
            WriteLine("Parsing file: " + testFile);
            WriteLine("=========================================");

            // Create a parser for the file and parse it
            Parser parser = new(testFile);

            if (parser.Parse())
                WriteLine("Parsing successful");
            else
                WriteLine("Parsing failed");
        }
    }
}