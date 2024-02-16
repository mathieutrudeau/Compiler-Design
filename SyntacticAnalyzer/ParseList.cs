using static System.Console;

namespace SyntacticAnalyzer;

public class ParseList : IParseList
{
    private ParseNode? First{ get; set;}
    private ParseNode? LeftMostNonTerminal { get; set; }

    public ParseList()
    {
        LeftMostNonTerminal = new("<START>");
        First = LeftMostNonTerminal;
    }

    public void Add(string Rule)
    {
        string[] elements = Rule.Split(" -> ");
        string lhs = elements[0].Trim();
        string[] rhs = elements[1].Trim().Split(" ").Where(x => x.Trim() != "EPSILON").Select(x=>x.Trim()).ToArray();

        
        if(LeftMostNonTerminal == null)
            return;


        if(LeftMostNonTerminal.Value == lhs)
        {
            ParseNode? prev = LeftMostNonTerminal.Prev;
            ParseNode? next = LeftMostNonTerminal.Next;

            int count = rhs.Length;

            bool leftMostFound = false;

            for(int i = 0; i < count; i++)
            {
                ParseNode node = new(rhs[i])
                {
                    Prev = prev,
                    Next = next,
                };
                
                if(prev != null)
                    prev.Next = node;
                else
                    First = node;
                
                if(next != null)
                    next.Prev = node;

                if(!node.IsTerminal && !leftMostFound)
                {
                    LeftMostNonTerminal = node;
                    leftMostFound = true;
                }

                prev = node;
            }

            if(count ==0)
            {
                ParseNode node = new("")
                {
                    Prev = prev,
                    Next = next,
                };

                if(prev != null)
                    prev.Next = node;
                else
                    First = node;
                
                if(next != null)
                    next.Prev = node;

                ParseNode current = node;
                while(current.Next != null)
                {
                    if(!current.Next.IsTerminal)
                    {
                        LeftMostNonTerminal = current.Next;
                        break;
                    }
                    current = current.Next;
                }
            }

            if(!leftMostFound && next != null)
            {
                ParseNode current = next;
                while(current.Next != null)
                {
                    if(!current.IsTerminal)
                    {
                        LeftMostNonTerminal = current;
                        break;
                    }
                    current = current.Next;
                }
            }
        }
    }
    
    public void Print()
    {
        ParseNode? current = First;
        while(current != null)
        {
            if (current.Value != "")
                Write(current.Value + " ");
            current = current.Next;
        }
        WriteLine();
    }

    public string GetDerivation()
    {
        ParseNode? current = First;
        string derivation = "";
        while(current != null)
        {
            if (current.Value != "")
                derivation += current.Value + " ";
            current = current.Next;
        }
        return derivation;
    }


}

public class ParseNode
{
    public ParseNode? Next { get; set; }
    public ParseNode? Prev { get; set; }
    public bool IsTerminal { get; set; }
    public string Value { get; set; }

    public ParseNode(string value)
    {
        value = value.Trim();
        Value = value;
        IsTerminal = (value.StartsWith("'") && value.EndsWith("'"))|| value =="";
    }
}