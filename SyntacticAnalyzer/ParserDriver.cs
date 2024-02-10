using LexicalAnalyzer;

namespace SyntacticAnalyzer;


public static class ErrorHandler
{
    public static void ParseFile(string sourceFolder)
    {
        Scanner scanner = new (sourceFolder);
        Parser parser = new (scanner);

        bool result = parser.Parse();
        
        if (result)
            Console.WriteLine("Parsing successful");
        else
            Console.WriteLine("Parsing failed");
    }
}