using System.Runtime.InteropServices;
using LexicalAnalyzer;
using static AbstractSyntaxTreeGeneration.SementicOperation;

namespace AbstractSyntaxTreeGeneration;

public class ASTNode : IASTNode
{

    public SementicOperation Operation { get; set; }

    public IASTNode? Parent { get; set; }

    public IASTNode? LeftMostChild { get; set; }

    public IASTNode? RightSibling { get; set; }

    public Token? Token { get; set; } = null;

    public ASTNode()
    {
        Parent = null;
        LeftMostChild = null;
        RightSibling = null;
        Operation = SementicOperation.Null;
    }

    public bool IsLeaf()
    {
        return LeftMostChild == null;
    }

    public bool IsRoot()
    {
        return Parent == null;
    }

    public bool IsLeftMostChild()
    {
        return Parent?.LeftMostChild == this;
    }
    
    public IASTNode GetLeftMostSibling()
    {                   
        // If the current node has no parent, return the current node
        // Otherwise, return the leftmost child of the parent node, which is the current node's leftmost sibling
        return Parent == null ? this : Parent.LeftMostChild!;
    }

    public IASTNode MakeSiblings(IASTNode siblingNode)
    {
        // Get the current instance of the node
        IASTNode currentNode = this;

        // Navigate to the rightmost sibling of the current node
        while (currentNode.RightSibling != null)
            currentNode = currentNode.RightSibling;

        // Set the rightmost sibling of the current node to the leftmost node of the new sibling nodes
        currentNode.RightSibling = siblingNode.GetLeftMostSibling();

        // Set the parent of the new sibling nodes to the parent of the current node
        while (currentNode.RightSibling != null)
        {
            currentNode.Parent = Parent;
            currentNode = currentNode.RightSibling;
        }

        // Return the current node
        return this;
    }

    public IASTNode AdoptChildren(IASTNode childNode)
    {
        // If the current node has a leftmost child, make the leftmost child become siblings with the new child node
        if (LeftMostChild != null)
            LeftMostChild.MakeSiblings(childNode);
        else
        {
            // Otherwise, the new child node becomes the leftmost child of the current node
            LeftMostChild = childNode.GetLeftMostSibling();

            // Set the parent of the new child node to the current node
            IASTNode child = LeftMostChild;
            while (child.RightSibling != null)
            {
                child.Parent = this;
                child = child.RightSibling;
            }

            // Set the parent of the new child node to the current node
            child.Parent = this;
        }

        // Return the current node
        return this;
    }

    /// <summary>
    /// Creates a new node with the specified operation and makes the specified child nodes the children of the new node.
    /// </summary>
    /// <param name="operation"> The operation of the new node. </param>
    /// <param name="childNodes"> The child nodes of the new node. </param>
    /// <returns> The new node. </returns>
    public static IASTNode MakeFamily(SementicOperation operation, params IASTNode[] childNodes)
    {
        // Create a new node with the specified operation
        IASTNode node = MakeNode(operation);

        // If there are no child nodes, return the new node
        if (childNodes.Length == 0)
            return node;

        // Get the first child node
        IASTNode firstChildNode = childNodes[0];

        // Make all the other child nodes siblings of the first child node
        foreach (IASTNode child in childNodes.Skip(1))
            firstChildNode.MakeSiblings(child);

        // Adopt the first child node as the child of the new node
        node.AdoptChildren(firstChildNode);

        // Return the new node
        return node;
    }



    public static IASTNode MakeNode()
    {
        return new ASTNode();
    }

    public static IASTNode MakeNode(SementicOperation operation)
    {
        return new ASTNode { Operation = operation };
    }

    public static IASTNode MakeNode(SementicOperation operation, Token token)
    {
        return new ASTNode { Operation = operation, Token = token};
    }


    public override string ToString()
    {
        return GetAST(this);
    }

    private string GetAST(IASTNode node, string indent = "")
    {
        string tree = indent + node.Operation.ToString();

        if(node.LeftMostChild==null)
        {
            if(node.Token!=null)
            {
                tree += " - " + node.Token.Lexeme;
            }
            else
            {
                tree += " - null";
            }
        }
        


        tree += "\n";

        IASTNode? child = node.LeftMostChild;

        while (child != null)
        {
            tree += GetAST(child, indent + "| ");
            child = child.RightSibling;
        }

        return tree;
    }

}

public class SementicStack
{
    private readonly Stack<IASTNode> _stack;

    public SementicStack()
    {
        _stack = new Stack<IASTNode>();
    }

    public void Push(IASTNode node)
    {
        _stack.Push(node);
    }


    /// <summary>
    /// Pushes an empty node onto the stack.
    /// </summary>
    public void PushEmptyNode()
    {
        _stack.Push(ASTNode.MakeNode());
    }

    /// <summary>
    /// Pushes an empty node onto the stack and then pushes the specified number of nodes onto the stack.
    /// </summary>
    /// <param name="x"> The number of nodes to push onto the stack. </param>
    public void PushEmptyBeforeX(int x)
    {
        // Create a list to store the nodes
        LinkedList<IASTNode> nodes = new();

        // Pop the specified number of nodes
        for (int i = 0; i < x; i++)
            nodes.AddFirst(_stack.Pop());

        // Push an empty node
        _stack.Push(ASTNode.MakeNode());

        // Push the popped nodes back onto the stack
        foreach (IASTNode node in nodes)
            _stack.Push(node);
    }

    public bool IsEmptyNode(int x)
    {
        // Create a list to store the nodes
        LinkedList<IASTNode> nodes = new();

        // Pop the specified number of nodes
        for (int i = 0; i < x; i++)
            nodes.AddFirst(_stack.Pop());

        // Check if the next node is an empty node
        bool isEmpty = _stack.Peek().Operation == Null;

        // Push the popped nodes back onto the stack
        foreach (IASTNode node in nodes)
            _stack.Push(node);

        // Return whether the next node is an empty node
        return isEmpty;
    }


    public void PushNode(SementicOperation operation, Token token)
    {
        _stack.Push(ASTNode.MakeNode(operation, token));
    }

    public void PushIfXEmpty(SementicOperation operation, int x)
    {
        if (IsEmptyNode(x))
        {
            PushNextX(operation, x);

            // Remove the empty node
            IASTNode node = _stack.Pop();
            _stack.Pop();
            _stack.Push(node);
        }

    }

    public void PushUntilEmptyNode(SementicOperation operation)
    {
        // Create a list to store the nodes
        LinkedList<IASTNode> nodes = new();

        // Pop nodes until an empty node is found
        while (_stack.Peek().Operation != Null)
            nodes.AddFirst(_stack.Pop());

        // Pop the empty node
        _stack.Pop();
        
        // Push a new node with the specified operation and the popped nodes as children
        _stack.Push(ASTNode.MakeFamily(operation, nodes.ToArray()));
    }

    public void PushNextX(SementicOperation operation, int x)
    {
        // Create a list to store the nodes
        LinkedList<IASTNode> nodes = new();

        // Pop the specified number of nodes
        for (int i = 0; i < x; i++)
            nodes.AddFirst(_stack.Pop());

        // Push a new node with the specified operation and the popped nodes as children
        _stack.Push(ASTNode.MakeFamily(operation, nodes.ToArray()));
    }
    

    public IASTNode Pop()
    {
        return _stack.Pop();
    }

    public IASTNode Peek()
    {
        return _stack.Peek();
    }

    public bool IsEmpty()
    {
        return _stack.Count == 0;
    }

}

public enum SementicOperation
{
    Null,
    Program,
    StructOrImplOrFunc,
    StructDecl,
    StructInheritList,
    StructMemberList,
    StructMember,
    ImplDef,
    FuncDef,
    FuncHead,
    Identifier,
    IntLit,
    RelOp,
    AddOp,
    MultOp,
    Visibility,
    Type,
    Sign,
    ArraySize,
    VarDecl,
    FParamList,
    IndexList,
    DataMember,
    FParam,
    VarDeclOrStatList,
    StatBlock,
    IfStat, 
    WhileStat,
    ReturnStat,
    AssignStat,
    ReadStat,
    WriteStat,
    RelExpr,
    MultExpr,
    AddExpr,
    FuncCall,
    FloatLit,
    NotFactor,
    SignFactor,
    Variable,
    DotChain,
    AParamList,
}