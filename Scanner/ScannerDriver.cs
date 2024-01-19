using static System.Console;
using System.IO;

namespace Scanner;


public static class ScannerDriver
{
    public static void GenerateTokens(string sourceFolder, bool showConsole = false)
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