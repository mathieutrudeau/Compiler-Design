namespace SyntacticAnalyzer;

/// <summary>
/// Represents a parse tree.
/// </summary>
public interface IParseTree
{
    public ParseNode Root { get; set; }
    public void AddProduction(string production);
    public ParseNode? GetLeftMostNonTerminal();
    public void Print();
    public void Close();
}

/// <summary>
/// Represents a parse node.
/// </summary>
public interface IParseNode
{
    public string Name { get; set; }
    public bool IsTerminal { get; set; }
    public List<ParseNode> Children { get; set; }
}