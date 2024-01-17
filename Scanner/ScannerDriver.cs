using static System.Console;

namespace Scanner;


public static class ScannerDriver
{
    public static void GenerateTokens(string source)
    {
        WriteLine("Running Scanner Driver...");
        WriteLine("Source: " + source);

        Scanner scanner = new (source);

        scanner.PrintBuffer();
        WriteLine();

        Token token = scanner.NextToken();

        WriteLine("Token: " + token.ToString());

        while (scanner.HasTokenLeft())
        {
            token = scanner.NextToken();
            WriteLine("Token: " + token.ToString());
        }
    }
}