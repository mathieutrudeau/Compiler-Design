using static System.Console;

namespace SyntacticAnalyzer;

/// <summary>
/// Represents the derivations obtained from applying a list of rules.
/// </summary>
public class ParseList : IParseList
{
    /// <summary>
    /// The first node in the list.
    /// </summary>
    private ParseNode? First { get; set; }

    /// <summary>
    /// The left-most non-terminal in the list.
    /// </summary>
    private ParseNode? LeftMostNonTerminal { get; set; }

    /// <summary>
    /// Creates a new parse list.
    /// </summary>
    public ParseList()
    {
        LeftMostNonTerminal = new("<START>");
        First = LeftMostNonTerminal;
    }

    /// <summary>
    /// Applies a rule to the current derivation.
    /// </summary>
    /// <param name="Rule">The rule to apply.</param>
    public void Add(string Rule)
    {
        // Split the rule into its left-hand side and right-hand side
        string[] elements = Rule.Split(" -> ");
        string lhs = elements[0].Trim();
        string[] rhs = elements[1].Trim().Split(" ").Where(x => x.Trim() != "EPSILON").Select(x => x.Trim()).ToArray();

        // If the left-hand side is not the left-most non-terminal, return
        if (LeftMostNonTerminal == null)
            return;

        // If the left-hand side is the left-most non-terminal, apply the rule
        if (LeftMostNonTerminal.Value == lhs)
        {
            // Keep track of the previous and next nodes
            ParseNode? prev = LeftMostNonTerminal.Prev;
            ParseNode? next = LeftMostNonTerminal.Next;

            int count = rhs.Length;
            bool leftMostFound = false;

            // Create new nodes for each right-hand side element
            for (int i = 0; i < count; i++)
            {
                // Create a new node
                ParseNode node = new(rhs[i])
                {
                    Prev = prev,
                    Next = next,
                };

                // Update the previous and next nodes
                if (prev != null)
                    prev.Next = node;
                else
                    First = node;

                if (next != null)
                    next.Prev = node;

                // Update the left-most non-terminal
                if (!node.IsTerminal && !leftMostFound)
                {
                    LeftMostNonTerminal = node;
                    leftMostFound = true;
                }

                prev = node;
            }

            // If the right-hand side is empty, create an empty node
            if (count == 0)
            {
                ParseNode node = new("")
                {
                    Prev = prev,
                    Next = next,
                };

                if (prev != null)
                    prev.Next = node;
                else
                    First = node;

                if (next != null)
                    next.Prev = node;

                ParseNode current = node;
                while (current.Next != null)
                {
                    if (!current.Next.IsTerminal)
                    {
                        LeftMostNonTerminal = current.Next;
                        break;
                    }
                    current = current.Next;
                }
            }

            // Update the left-most non-terminal if necessary. This is necessary when the rule is applied to the left-most non-terminal. In this case, the left-most non-terminal is the first non-terminal in the right-hand side that is not a terminal. If no such non-terminal is found
            if (!leftMostFound && next != null)
            {
                ParseNode current = next;
                while (current.Next != null)
                {
                    if (!current.IsTerminal)
                    {
                        LeftMostNonTerminal = current;
                        break;
                    }
                    current = current.Next;
                }
            }
        }
    }

    /// <summary>
    /// Prints current derivation.
    /// </summary>
    public void Print()
    {
        ParseNode? current = First;
        while (current != null)
        {
            // Write the value of the node
            if (current.Value != "")
                Write(current.Value + " ");
            current = current.Next;
        }
        WriteLine();
    }

    /// <summary>
    /// Returns the current derivation.
    /// </summary>
    /// <returns>The current derivation.</returns> 
    public string GetDerivation()
    {
        ParseNode? current = First;
        string derivation = "";
        while (current != null)
        {
            if (current.Value != "")
                derivation += current.Value + " ";
            current = current.Next;
        }
        return derivation;
    }
}

/// <summary>
/// Represents a node in a parse list.
/// </summary>
public class ParseNode : IParseNode
{
    /// <summary>
    /// The next node in the list.
    /// </summary>
    public ParseNode? Next { get; set; }

    /// <summary>
    /// The previous node in the list.
    /// </summary>
    public ParseNode? Prev { get; set; }

    /// <summary>
    /// Whether the node is a terminal.
    /// </summary>
    public bool IsTerminal { get; set; }

    /// <summary>
    /// The value of the node.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Creates a new parse node.
    /// </summary>
    public ParseNode(string value)
    {
        value = value.Trim();
        Value = value;
        IsTerminal = (value.StartsWith("'") && value.EndsWith("'")) || value == "";
    }
}