using static System.Console;
using System.IO;

namespace LexicalAnalyzer;

/// <summary>
/// Driver to run the scanner on a set of test files.
/// </summary>
public static class ScannerDriver
{
    /// <summary>
    /// Runs the scanner on a set of test files.
    /// </summary>
    public static void GenerateTokens(string sourceFolder)
    {
        WriteLine("Running Scanner Driver...");

        // Get all the test files
        string[] testFiles = Directory.GetFiles(sourceFolder, "*.src");

        // Run the scanner on each file
        foreach (string file in testFiles)
        {
            WriteLine("Running Scanner on " + file);

            Scanner scanner = new(file);
        
            Token token;

            WriteLine("Fetching Tokens...");

            // Fetch all the tokens until the end of file
            do
            {
                token = scanner.NextToken();
            }
            while (token.Type != TokenType.Eof);

            WriteLine("Done!");
        }
    }
}