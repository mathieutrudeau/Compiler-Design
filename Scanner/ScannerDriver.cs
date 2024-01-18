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

            string tokens = "";
            string errors = "";
        
            Token token;
            int currentLine = 1;

            WriteLine("Fetching Tokens...");

            while(scanner.HasTokenLeft())
            {
                token = scanner.NextToken();
                
                if (token.Location != currentLine)
                {
                    tokens += "\n"+token.ToString();
                    currentLine = token.Location;
                }
                else
                    tokens += token.ToString()+ " ";

                errors += token.ShowError();
            }

            WriteLine("Writing Tokens to file...");

            using StreamWriter sw = new(file.Replace(".src", ".outlextokens"));
            sw.Write(tokens);

            WriteLine("Writing Errors to file...");

            using StreamWriter sw1 = new(file.Replace(".src", ".outlexerrors"));
            sw1.Write(errors);

            WriteLine("Done!");
        }
    }
}