using LexicalAnalyzer;

namespace SyntacticAnalyzer;


public static class ParserDriver
{
    public static void ParseFile(string sourceFolder)
    {
        string[] testFiles = Directory.GetFiles(sourceFolder, "*.src");

        foreach (string testFile in testFiles)
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("Parsing file: " + testFile);
            Console.WriteLine("=========================================");

            Parser parser = new(testFile);

            bool result = parser.Parse();

            if (result)
                Console.WriteLine("Parsing successful");
            else
                Console.WriteLine("Parsing failed");
        }
    }
}