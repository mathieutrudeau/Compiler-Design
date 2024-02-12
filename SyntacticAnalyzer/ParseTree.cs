namespace SyntacticAnalyzer;
public class ParseTree
{
    public ParseNode Root { get; set; }

    public ParseTree(ParseNode root)
    {
        Root = root;
    }

    public void Print()
    {
        Print(Root, 0);
    }

    private void Print(ParseNode node, int depth)
    {
        Console.WriteLine($"{new string(' ', depth * 2)}{node}");
        foreach (ParseNode child in node.Children)
        {
            Print(child, depth + 1);
        }
    }
}

public class ParseNode
{
}
