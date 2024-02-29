using System.Runtime.InteropServices;

namespace AbstractSyntaxTreeGeneration;

public class ASTNode : IASTNode
{

    public SementicOperation Operation { get; set; }

    public IASTNode? Parent { get; set; }

    public IASTNode? LeftMostChild { get; set; }

    public IASTNode? RightSibling { get; set; }



    public ASTNode()
    {
        Parent = null;
        LeftMostChild = null;
        RightSibling = null;
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
            currentNode.RightSibling.Parent = currentNode.Parent;
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
        }

        // Return the current node
        return this;
    }

    public static IASTNode MakeFamily(SementicOperation operation, IASTNode firstChildNode, IASTNode secondChildNode, params IASTNode[] otherChildNodes)
    {
        // Create a new node with the specified operation
        IASTNode node = MakeNode(operation);

        // Make all the child nodes siblings of the new node
        IASTNode lefChild = firstChildNode.MakeSiblings(secondChildNode);

        // Make all the other child nodes siblings of the leftmost child node
        foreach (IASTNode child in otherChildNodes)
            lefChild.MakeSiblings(child);

        // Adopt the leftmost child node as the child of the new node
        node.AdoptChildren(lefChild);

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

}



public enum SementicOperation
{
    Prog
}