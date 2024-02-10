using static System.Console;
using System.IO;

namespace LexicalAnalyzer;

public static class ScannerDriver
{
    public static void GenerateTokens(string sourceFolder)
    {
        WriteLine("Running Scanner Driver...");

        string[] testFiles = Directory.GetFiles(sourceFolder, "*.src");

        foreach (string file in testFiles)
        {
            WriteLine("Running Scanner on " + file);

            Scanner scanner = new(file);
        
            Token token;

            WriteLine("Fetching Tokens...");

            while(scanner.HasTokenLeft())
            {
                token = scanner.NextToken();
            }

            WriteLine("Done!");
        }
    }
}