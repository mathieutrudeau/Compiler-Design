using LexicalAnalyzer;

namespace SyntacticAnalyzer;

public class Parser : IParser
{
    private IScanner Scanner { get; }
    private Token LookAhead { get; set; }

    public Parser(IScanner scanner)
    {
        Scanner = scanner;
        LookAhead = new Token();
    }

    public bool Parse()
    {
        LookAhead = Scanner.NextToken();
        
        return false;
    }

    public bool Match()
    {
        
        return false;
    }

    #region Non-Terminals Production Ruless

    public bool Start()
    {
        return false;
    }

    #endregion Non-Terminals Production Rules

}