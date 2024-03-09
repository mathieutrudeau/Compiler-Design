using System.Transactions;
using LexicalAnalyzer;
using static AbstractSyntaxTreeGeneration.SemanticOperation;

namespace AbstractSyntaxTreeGeneration;

/// <summary>
/// Implementation of an abstract syntax tree node.
/// </summary>
public class ASTNode : IASTNode
{

    public SemanticOperation Operation { get; set; }

    public IASTNode? Parent { get; set; }

    public IASTNode? LeftMostChild { get; set; }

    public IASTNode? RightSibling { get; set; }

    public Token? Token { get; set; } = null;

    /// <summary>
    /// The next id for the abstract syntax tree.
    /// </summary>
    private int _next_id = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ASTNode"/> class.
    /// By default, the parent, leftmost child, and right sibling are set to null.
    /// </summary>
    public ASTNode()
    {
        Parent = null;
        LeftMostChild = null;
        RightSibling = null;
        Operation = SemanticOperation.Null;
    }

    #region Instance Methods

    public bool IsLeaf()
    {
        // If the current node has no leftmost child, it is a leaf node
        return LeftMostChild == null;
    }

    public bool IsRoot()
    {
        // If the current node has no parent, it is the root of the tree
        return Parent == null;
    }

    public bool IsLeftMostChild()
    {
        // If the current node has a parent and the leftmost child of the parent is the current node, the current node is the leftmost child
        return Parent?.LeftMostChild == this;
    }

    public IASTNode GetLeftMostSibling()
    {
        // If the current node has no parent, return the current node (as it is the root of the tree)
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
    /// Returns a string representation of the abstract syntax tree.
    /// </summary>
    /// <param name="node"> The root node of the abstract syntax tree. </param>
    /// <param name="indent"> The indentation for the tree. </param>
    /// <returns> A string representation of the abstract syntax tree. </returns>
    private string GetAST(IASTNode node, string indent = "")
    {
        // Get the operation of current node
        string tree = indent + node.Operation.ToString();

        // If the current node is a leaf node, append the token to the output
        if (node.LeftMostChild == null)
            if (node.Token != null)
                tree += " - " + node.Token.Lexeme;
            else
                tree += " - null";

        // Append a newline to the output
        tree += "\n";

        // Get the leftmost child of the current node
        IASTNode? child = node.LeftMostChild;

        // Recursively get the abstract syntax tree of the leftmost child and its siblings
        while (child != null)
        {
            tree += GetAST(child, indent + "| ");
            child = child.RightSibling;
        }

        // Return the string representation of the abstract syntax tree
        return tree;
    }

    public override string ToString()
    {
        // Get the abstract syntax tree of the current node
        return GetAST(this);
    }

    /// <summary>
    /// Returns a string representation of the abstract syntax tree in DOT format.
    /// </summary>
    /// <returns> A string representation of the abstract syntax tree in DOT format. </returns>
    public string DotASTString()
    {
        // Reset the next id for the abstract syntax tree
        _next_id = 0;

        // Create the DOT string for the abstract syntax tree
        string dot = "digraph AST {\n";
        dot += "node [shape=record];\n";
        dot += "node [fontname=Sans];charset=\"UTF-8\" splines=true splines=spline rankdir =LR\n";
        
        // Get the DOT string for the abstract syntax tree
        dot += DotASTString(this, _next_id++);
        dot += "}\n";

        return dot;
    }

    /// <summary>
    /// Returns a string representation of the abstract syntax tree in DOT format.
    /// </summary>
    /// <param name="node"> The current node. </param>
    /// <param name="id"> The id of the current node. </param>
    /// <returns> A string representation of the abstract syntax tree in DOT format. </returns>
    private string DotASTString(IASTNode node, int id)
    {       
        // Create the DOT string for the current node
        string dot = id + " [label=\"" + node.Operation.ToString()+"\"]\n";

        // Get the DOT string for the leftmost child of the current node and its siblings
        IASTNode? child = node.LeftMostChild;
        while (child != null)
        {
            // Get the DOT string for the child node
            int nextId = _next_id++;
            dot += id + " -> " + nextId + "\n";
            dot += DotASTString(child, nextId);
            child = child.RightSibling;
        }

        return dot;
    }

    #endregion Instance Methods

    #region Static Methods

    /// <summary>
    /// Creates a new node with the specified operation and makes the specified child nodes the children of the new node.
    /// </summary>
    /// <param name="operation"> The operation of the new node. </param>
    /// <param name="childNodes"> The child nodes of the new node. </param>
    /// <returns> The new node. </returns>
    public static IASTNode MakeFamily(SemanticOperation operation, params IASTNode[] childNodes)
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

    /// <summary>
    /// Creates a new empty node.
    /// </summary>
    /// <returns> The new node. </returns>
    public static IASTNode MakeNode()
    {
        return new ASTNode();
    }

    /// <summary>
    /// Creates a new node with the specified operation.
    /// </summary>
    /// <param name="operation"> The operation of the new node. </param>
    /// <returns> The new node. </returns>
    public static IASTNode MakeNode(SemanticOperation operation)
    {
        return new ASTNode { Operation = operation };
    }

    /// <summary>
    /// Creates a new node with the specified operation and token.
    /// </summary>
    /// <param name="operation"> The operation of the new node. </param>
    /// <param name="token"> The token of the new node. </param>
    /// <returns> The new node. </returns>
    public static IASTNode MakeNode(SemanticOperation operation, Token? token)
    {
        return new ASTNode { Operation = operation, Token = token };
    }

    #endregion Static Methods
}

/// <summary>
/// Implementation of a stack for the abstract syntax tree.
/// </summary>
public class SemanticStack : ISemanticStack
{
    /// <summary>
    /// Stack to store the nodes of the abstract syntax tree.
    /// </summary>
    private readonly Stack<IASTNode> _stack;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticStack"/> class.
    /// </summary>
    public SemanticStack()
    {
        _stack = new Stack<IASTNode>();
    }

    #region Push Methods

    public void Push(IASTNode node)
    {
        _stack.Push(node);
    }

    public void PushEmptyNode()
    {
        _stack.Push(ASTNode.MakeNode());
    }

    public void PushPlaceholderNodeBeforeX(int x)
    {
        // Create a list to store the nodes
        LinkedList<IASTNode> nodes = new();

        // Pop the specified number of nodes
        for (int i = 0; i < x; i++)
            nodes.AddFirst(_stack.Pop());

        // Push a placeholder node
        _stack.Push(ASTNode.MakeNode(Placeholder));

        // Push the popped nodes back onto the stack
        foreach (IASTNode node in nodes)
            _stack.Push(node);
    }

    public void PushIfXPlaceholder(SemanticOperation operation, int x)
    {
        // Check if the node before the xth node is a placeholder node
        if (IsPlaceholderNode(x))
        {
            // Push a new node with the specified operation before the xth node
            PushNextX(operation, x);

            // Remove the placeholder node
            IASTNode node = _stack.Pop();
            _stack.Pop();
            _stack.Push(node);
        }
    }

    public void PushNode(SemanticOperation operation, Token? token)
    {
        // Push a new node with the specified operation and token
        _stack.Push(ASTNode.MakeNode(operation, token));
    }

    public void PushNextX(SemanticOperation operation, int x)
    {
        // Create a list to store the nodes
        LinkedList<IASTNode> nodes = new();

        // Pop the specified number of nodes
        for (int i = 0; i < x; i++)
            nodes.AddFirst(_stack.Pop());

        // Push a new node with the specified operation and the popped nodes as children
        _stack.Push(ASTNode.MakeFamily(operation, nodes.ToArray()));
    }

    public void PushUntilEmptyNode(SemanticOperation operation)
    {
        // Create a list to store the nodes
        LinkedList<IASTNode> nodes = new();

        // Pop nodes until an empty node is found
        while (_stack.Peek().Operation != SemanticOperation.Null)
            nodes.AddFirst(_stack.Pop());

        // Pop the empty node
        _stack.Pop();

        // Push a new node with the specified operation and the popped nodes as children
        _stack.Push(ASTNode.MakeFamily(operation, nodes.ToArray()));
    }

    #endregion Push Methods

    public bool IsPlaceholderNode(int x)
    {
        // Create a list to store the nodes
        LinkedList<IASTNode> nodes = new();

        // Pop the specified number of nodes
        for (int i = 0; i < x; i++)
            nodes.AddFirst(_stack.Pop());

        // Check if the next node is a placeholder node
        bool isPlaceholder = _stack.Peek().Operation == SemanticOperation.Placeholder;

        // Push the popped nodes back onto the stack
        foreach (IASTNode node in nodes)
            _stack.Push(node);

        // Return whether the next node is a placeholder node
        return isPlaceholder;
    }

    public IASTNode Pop()
    {
        return _stack.Pop();
    }
}

/// <summary>
/// Semantic operations for the abstract syntax tree. 
/// These operations are used to identify the type of node in the abstract syntax tree.
/// </summary>
public enum SemanticOperation
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
    FuncDefList,
    Placeholder,
    ArrayIndex,
}