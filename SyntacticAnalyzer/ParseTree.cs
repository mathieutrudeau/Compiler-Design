using static System.Console;

namespace SyntacticAnalyzer;

/// <summary>
/// A parse tree
/// </summary>
public class ParseTree : IParseTree
{
    /// <summary>
    /// The root of the parse tree
    /// </summary>
    public ParseNode Root { get; set; }

    /// <summary>
    /// Whether to show the print of the parse tree or not in the console.
    /// </summary>
    private bool ShowPrint { get; set; } = false;

    /// <summary>
    /// The name of the file in which to print the derivations
    /// </summary>
    private string FileName { get; set; } = "";

    private StreamWriter File { get; set; } 

    /// <summary>
    /// Constructs a parse tree
    /// </summary>
    /// <param name="root">The root of the parse tree</param>
    /// <param name="fileName">The name of the file in which to print the derivations</param>
    /// <param name="showPrint">Whether to show the print of the parse tree or not in the console</param>
    public ParseTree(ParseNode root, string fileName, bool showPrint = false)
    {
        Root = root;
        FileName = fileName;
        ShowPrint = showPrint;
        File = new(FileName,append: true);
    }

    /// <summary>
    /// Adds a production to the parse tree
    /// </summary>
    /// <param name="production">The production to add</param>
    public void AddProduction(string production)
    {
        // Get the left most non terminal node in the parse tree
        ParseNode? leftMostNonTerminal = GetLeftMostNonTerminal();

        // If no non terminal left in the tree, return
        if (leftMostNonTerminal == null)
            return;

        // Split the production into parent and children
        string parentName = production.Split(" -> ")[0].Trim();
        string[] productions = production.Split(" -> ")[1].Trim().Split(" ");

        // If the current left most non terminal is not the parent of the production, return
        // This is to prevent adding productions to the wrong non terminal node
        if (parentName != leftMostNonTerminal.Name)
            return;

        // Add the children to the left most non terminal node
        foreach (string prod in productions)
            leftMostNonTerminal.Children.Add(new(prod, prod.StartsWith("'") && prod.EndsWith("'") || prod.StartsWith("EPSILON")));
    }

    /// <summary>
    /// Returns the left most non terminal node in the parse tree
    /// </summary>
    /// <returns>The left most non terminal node, null if no non terminal left in the tree</returns>
    /// <remarks> This method is a wrapper for the private GetLeftMostNonTerminal method </remarks>
    public ParseNode? GetLeftMostNonTerminal()
    {
        return GetLeftMostNonTerminal(Root);
    }

    /// <summary>
    /// Returns the left most non terminal node in the parse tree
    /// </summary>
    /// <param name="node">The node to start from</param>
    /// <returns>The left most non terminal node, null if no non terminal left in the tree</returns>
    private ParseNode? GetLeftMostNonTerminal(ParseNode node)
    {
        // If the node is a terminal, return null
        if (node.IsTerminal)
            return null;
        else
        {
            // If the node has no children and is not terminal, it is a leaf so return the node
            if (node.Children.Count == 0)
                return node;

            // If the node has children, iterate through them and return the left most non terminal
            for (int i = 0; i < node.Children.Count; i++)
                if (!node.Children[i].IsTerminal && GetLeftMostNonTerminal(node.Children[i]) != null)
                    return GetLeftMostNonTerminal(node.Children[i]);

            // If no non terminal was found, return null
            return null;
        }
    }

    /// <summary>
    /// Prints the parse tree to the console and a file
    /// </summary>
    /// <remarks> This method is a wrapper for the private Print method </remarks>
    /// <remarks> The file in which to print the derivations is specified in the constructor </remarks>
    /// <remarks> The ShowPrint property is used to determine whether to print to the console or not </remarks>
    public void Print()
    {
        string startProduction = Root.Name + " -> ";

        // Add the production to the file and console if ShowPrint is true
        if (ShowPrint)
            Write(startProduction);
        File.Write(startProduction);

        // Print the leaf nodes of the parse tree
        Print(Root);

        // Add a new line to the file and console if ShowPrint is true
        if (ShowPrint)
            WriteLine();
        File.WriteLine();
    }

    /// <summary>
    /// Prints the leaf nodes of the parse tree to the console and a file
    /// </summary>
    /// <param name="node">The node to start from</param>
    /// <remarks> This method is a recursive method that prints the leaf nodes of the parse tree </remarks>
    /// <remarks> The file in which to print the derivations is specified in the constructor </remarks>
    /// <remarks> The ShowPrint property is used to determine whether to print to the console or not </remarks>
    private void Print(ParseNode node)
    {
        // If the node is a leaf node, print it to the console and file
        if (node.Children.Count == 0)
        {
            if (ShowPrint)
                Write(node.ToString());
            File.Write(node.ToString());
        }
        else
        {
            // If the node is not a leaf node, iterate through its children and print them
            for (int i = 0; i < node.Children.Count; i++)
                Print(node.Children[i]);
        }
    }

    public void Close()
    {
        File.Close();
    }
}

/// <summary>
/// A node in the parse tree
/// </summary>
public class ParseNode : IParseNode
{
    /// <summary>
    /// Creates a new parse node
    /// </summary>
    public ParseNode(string name, bool isTerminal)
    {
        Name = name;
        IsTerminal = isTerminal;
    }

    /// <summary>
    /// The name of the node, which is the production or terminal symbol
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Whether the node is a terminal symbol or not
    /// </summary> 
    public bool IsTerminal { get; set; } = false;

    /// <summary>
    /// The list of children of the node
    /// </summary>
    public List<ParseNode> Children { get; set; } = new();

    /// <summary>
    /// Returns the name of the node
    /// </summary>
    /// <returns>The name of the node</returns>
    public override string ToString()
    {
        if (Name.StartsWith("'") && Name.EndsWith("'"))
        {
            return Name[1..^1]+ " ";
        }
        else if (Name.StartsWith("EPSILON"))
        {
            return "";
        }
        else
        {
            return Name+ " ";
        }
    }
}
