using LexicalAnalyzer;
using SemanticAnalyzer;

namespace AbstractSyntaxTreeGeneration;

public interface ISemanticStack
{   
    /// <summary>
    /// Peeks at the top of the stack.
    /// </summary>
    /// <returns> The node at the top of the stack. </returns>
    public IASTNode Peek();

    /// <summary>
    /// Checks if the stack is empty.
    /// </summary>
    /// <returns> True if the stack is empty, otherwise false. </returns>
    public bool IsEmpty();

    /// <summary>
    /// Pushes a node onto the stack.
    /// </summary>
    /// <param name="node"> The node to push onto the stack. </param>
    public void Push(IASTNode node);

    /// <summary>
    /// Pushes an empty node onto the stack.
    /// </summary>
    public void PushEmptyNode();

    /// <summary>
    /// Pushes a placeholder node onto the stack before the xth node.
    /// </summary>
    /// <param name="x"> The number of nodes to push before. </param>
    public void PushPlaceholderNodeBeforeX(int x);

    /// <summary>
    /// Pushes a node with the given operation onto the stack if the node before the xth node is a placeholder node.
    /// </summary>
    /// <param name="operation"> The operation to push. </param>
    /// <param name="x"> The number of nodes to check before. </param>
    public void PushIfXPlaceholder(SemanticOperation operation, int x);

    /// <summary>
    /// Pushes a leaf node with the given operation and token onto the stack.
    /// </summary>
    /// <param name="operation"> The operation to push. </param>
    /// <param name="token"> The token to push. </param>
    public void PushNode(SemanticOperation operation, Token? token);
    
    /// <summary>
    /// Pushes a node with the given operation onto the stack that encompasses the next x nodes as children.
    /// </summary>
    /// <param name="operation"> The operation to push. </param>
    /// <param name="x"> The number of nodes to encompass. </param>
    public void PushNextX(SemanticOperation operation, int x);

    
    /// <summary>
    /// Pushes a node with the given operation onto the stack that encompasses all nodes until an empty node is reached.
    /// </summary>
    /// <param name="operation"> The operation to push. </param>
    public void PushUntilEmptyNode(SemanticOperation operation);

    /// <summary>
    /// Checks if the xth node is a placeholder node.
    /// </summary>
    /// <param name="x"> The number of nodes to check before. </param>
    /// <returns> True if the xth node is a placeholder node, otherwise false. </returns>
    public bool IsPlaceholderNode(int x);

    /// <summary>
    /// Pops a node from the stack.
    /// </summary>
    /// <returns> The node that was popped from the top of the stack. </returns>
    public IASTNode Pop();

}

/// <summary>
/// Interface for an abstract syntax tree node.
/// </summary>
public interface IASTNode: IVisitor
{
    /// <summary>
    /// Gets or sets the SemanticOperation of the node.
    /// </summary>
    public SemanticOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the parent of the node.
    /// </summary>
    public IASTNode? Parent { get; set; }

    /// <summary>
    /// Gets or sets the leftmost child of the node.
    /// </summary>
    public IASTNode? LeftMostChild { get; set; }

    /// <summary>
    /// Gets or sets the right sibling of the node.
    /// </summary>
    public IASTNode? RightSibling { get; set; }

    /// <summary>
    /// Gets or sets the token of the node. Only applicable for leaf nodes.
    /// </summary>
    public Token? Token { get; set; }

    /// <summary>
    /// Gets the leftmost sibling of the current node.
    /// </summary>
    /// <returns> The leftmost sibling of the current node. </returns>
    public IASTNode GetLeftMostSibling();

    /// <summary>
    /// Adds the sibling node to the right of the current node.
    /// </summary>
    /// <param name="siblingNode"> The sibling node to add to the right of the current node. </param>
    /// <returns> The current node. </returns>
    public IASTNode MakeSiblings(IASTNode siblingNode);

    /// <summary>
    /// Adds the child node to the child list of the current node.
    /// </summary>
    /// <param name="childNode"> The child node to add to the child list of the current node. </param>
    /// <returns> The current node. </returns>
    public IASTNode AdoptChildren(IASTNode childNode);

    /// <summary>
    /// Checks if the current node is the root of the tree.
    /// </summary>
    /// <returns> True if the current node is the root of the tree, otherwise false. </returns>
    public bool IsRoot();

    /// <summary>
    /// Checks if the current node is a leaf node.
    /// </summary>
    /// <returns> True if the current node is a leaf node, otherwise false. </returns>
    public bool IsLeaf();

    /// <summary>
    /// Gets the string representation of the abstract syntax tree as a dot file.
    /// </summary>
    /// <returns> The string representation of the abstract syntax tree as a dot file. </returns>
    public string DotASTString();

}