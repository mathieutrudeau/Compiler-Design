using static System.Console;
using LexicalAnalyzer;
using static LexicalAnalyzer.ScannerDriver;
using static SyntacticAnalyzer.ParserDriver;

namespace Compiler_Design;

class Program
{
    static void Main(string[] args)
    {
        //GenerateTokens("ScannerTestFiles");
        ParseFile("ParserTestFiles");
    }

    
}
