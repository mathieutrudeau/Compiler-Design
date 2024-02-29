namespace AbstractSyntaxTreeGeneration;

public interface IASTNode
{
    public SementicOperation Operation { get; set; }

    public IASTNode? Parent { get; set; }

    public IASTNode? LeftMostChild { get; set; }

    public IASTNode? RightSibling { get; set; }

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
}