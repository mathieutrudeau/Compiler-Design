using System.Transactions;
using LexicalAnalyzer;
using SemanticAnalyzer;
using static AbstractSyntaxTreeGeneration.SemanticOperation;
using static System.Console;

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
        string dot = id + " [label=\"" + node.Operation.ToString() + "\"]\n";

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

    private string GetType(IASTNode node)
    {
        // Perform the appropriate action based on the operation of the current node
        switch (node.Operation)
        {
            case IntLit:
                return "integer";
            case FloatLit:
                return "float";
            case SignFactor:
                return GetType(node.LeftMostChild!.RightSibling!);
            case AddExpr:
            case MultExpr:
                return GetType(node.LeftMostChild!) == "float" || GetType(node.LeftMostChild!.RightSibling!.RightSibling!) == "float" ?
                    "float" : "integer";
            case SemanticOperation.DataMember:
                return GetType(node.LeftMostChild!);
            default:
                return "";  
                break;
        }
    }

    private bool ValidateType(IASTNode node, List<ISemanticError> errors)
    {


        return false;
    }


    public void Visit(ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        // Perform the appropriate action based on the operation of the current node
        switch (Operation)
        {
            case ImplDef:
                /*
                The current node is a class implementation definition.
                Perform the following checks:
                - The class should already be declared in the symbol table
                
                */

                // The leftmost child of the current node is the identifier of the class implementation
                string implName = LeftMostChild!.Token!.Lexeme;

                // The class should already be declared in the symbol table
                ISymbolTableEntry? implEntry = currentTable.Lookup(implName);

                // If the class is not found, print an error message
                if (implEntry == null)
                {
                    WriteLine("Error: Class " + implName + " not found");
                    break;
                }

                // If the class is found, load the symbol table of the class
                ISymbolTable implTable = implEntry.Link!;
                currentTable = implTable;

                break;
            case FuncDef:

                /*
                The current node is a function definition.
                Perform the following checks:
                - Given that the function is a member of a class, the class should already be declared in the symbol table.
                - The function should not already be declared in the symbol table if it is a free function.
                */

                // The leftmost child of the current node is the function head
                IASTNode functionHead = LeftMostChild!;

                // The first child of the function head is the identifier of the function
                string functionName = functionHead.LeftMostChild!.Token!.Lexeme;

                // The third child of the function head is the return type of the function
                string returnType = functionHead.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme;

                // Check if the function is a member of a class
                bool isMember = Parent!.Operation == FuncDefList;

                // If the function is a member of a class, the class should already be declared in the symbol table
                if (isMember)
                {
                    // The method should already be declared in the symbol table
                    ISymbolTableEntry? methodEntry = currentTable.Lookup(functionName);

                    // If the method is not found, print an error message
                    if (methodEntry == null)
                    {
                        WriteLine("Error: Method " + functionName + " not found");
                        break;
                    }

                    // If the method is found, load the symbol table of the method
                    ISymbolTable methodTable = methodEntry.Link!;
                    currentTable = methodTable;

                    break;
                }

                // Create a new symbol table for the function
                ISymbolTable functionTable = new SymbolTable(functionName, currentTable);

                // Create a new symbol table entry for the function
                ISymbolTableEntry functionEntry = new SymbolTableEntry(functionName, SymbolEntryKind.Function, returnType, functionTable,functionHead.LeftMostChild!.Token!.Location);

                // Add the function entry to the current table
                currentTable.AddEntry(functionEntry);

                // Update the current table to the function table
                currentTable = functionTable;

                break;
            case FParam:

                // Get the identifier of the parameter
                string paramName = LeftMostChild!.Token!.Lexeme;

                // Get the type of the parameter
                string paramType = LeftMostChild!.RightSibling!.Token!.Lexeme;

                // Check if the parameter is an array
                IASTNode arraySize = LeftMostChild!.RightSibling!.RightSibling!;

                // Add the array size to the parameter type
                if (arraySize.LeftMostChild != null)
                    if (arraySize.LeftMostChild!.Token != null)
                        paramType += "[" + arraySize.LeftMostChild!.Token!.Lexeme + "]";
                    else
                        paramType += "[]";

                // Create a new symbol table entry for the parameter
                ISymbolTableEntry paramEntry = new SymbolTableEntry(paramName, SymbolEntryKind.Parameter, paramType, null,LeftMostChild!.Token!.Location);

                // Add the parameter entry to the current table
                currentTable.AddEntry(paramEntry);

                break;
            case VarDecl:

                // Make sure the variable is not a member of a struct
                if (Parent!.Operation == StructMember)
                    break;

                string varName = LeftMostChild!.Token!.Lexeme;
                string varType = LeftMostChild!.RightSibling!.Token!.Lexeme;

                // Check if the variable is an array
                IASTNode varArraySize = LeftMostChild!.RightSibling!.RightSibling!;

                // Add the array size to the variable type
                if (varArraySize.LeftMostChild != null)
                    if (varArraySize.LeftMostChild!.Token != null)
                        if (varArraySize.LeftMostChild!.Token!.Lexeme == "0")
                            errors.Add(new SemanticError(SemanticErrorType.ArraySizeZero, varArraySize.LeftMostChild!.Token!.Location,"Array size cannot be zero."));
                        else
                            varType += "[" + varArraySize.LeftMostChild!.Token!.Lexeme + "]";
                    else
                    {
                        varType += "[]";

                        // If the array size is not specified, then trigger a warning
                        warnings.Add(new SemanticWarning(SemanticWarningType.ArraySizeNotSpecified, LeftMostChild!.RightSibling!.Token!.Location,"Array size not specified."));    
                    }
                // Create a new symbol table entry for the variable
                ISymbolTableEntry varEntry = new SymbolTableEntry(varName, SymbolEntryKind.Variable, varType, null,LeftMostChild!.Token!.Location);

                // Add the variable entry to the current table
                currentTable.AddEntry(varEntry);

                break;
            case StructDecl:

                // The leftmost child of the current node is the identifier of the struct
                string structName = LeftMostChild!.Token!.Lexeme;

                // Create a new symbol table for the struct
                ISymbolTable structTable = new SymbolTable(structName, currentTable);

                // Create a new symbol table entry for the struct
                ISymbolTableEntry structEntry = new SymbolTableEntry(structName, SymbolEntryKind.Class, structName, structTable,LeftMostChild!.Token!.Location);

                // Add the struct entry to the current table
                currentTable.AddEntry(structEntry);

                // Update the current table to the struct table
                currentTable = structTable;


                break;
            case StructInheritList:
                // Get the leftmost child of the current node (the identifier of the inherited struct)
                IASTNode? inheritedStruct = LeftMostChild;

                // While there are more inherited structs, add them to the current struct table
                while (inheritedStruct != null)
                {
                    // Get the identifier of the inherited struct
                    string inheritedStructName = inheritedStruct.Token!.Lexeme;

                    // Create a new symbol table entry for the inherited struct
                    ISymbolTableEntry inheritedStructEntry = new SymbolTableEntry(inheritedStructName, SymbolEntryKind.Inherit, inheritedStructName, null,inheritedStruct.Token!.Location);

                    // Add the inherited struct entry to the current table
                    currentTable.AddEntry(inheritedStructEntry);

                    // Get the next inherited struct
                    inheritedStruct = inheritedStruct.RightSibling;
                }


                break;
            case StructMember:
                WriteLine("Struct Member");

                // The leftmost child of the current node is the visibility of the member
                string visibility = LeftMostChild!.Token!.Lexeme;

                IASTNode member = LeftMostChild!.RightSibling!;

                // The member is either a variable declaration or a function definition
                if (member.Operation == VarDecl)
                {
                    // The leftmost child of the member is the identifier of the member
                    string memberName = member.LeftMostChild!.Token!.Lexeme;
                    string memberType = member.LeftMostChild!.RightSibling!.Token!.Lexeme;

                    // Check if the member is an array
                    IASTNode memberArraySize = member.LeftMostChild!.RightSibling!.RightSibling!;
                    if (memberArraySize.LeftMostChild != null)
                        if (memberArraySize.LeftMostChild!.Token != null)
                            memberType += "[" + memberArraySize.LeftMostChild!.Token!.Lexeme + "]";
                        else
                            memberType += "[]";

                    // Create a new symbol table entry for the member
                    ISymbolTableEntry memberEntry = new SymbolTableEntry(memberName, SymbolEntryKind.Data, memberType, null,member.LeftMostChild!.Token!.Location);

                    // Add the member entry to the current table
                    currentTable.AddEntry(memberEntry);
                }
                else
                {
                    // The leftmost child of the member is the function head
                    IASTNode methodHead = member;

                    // The first child of the function head is the identifier of the function
                    string methodName = methodHead.LeftMostChild!.Token!.Lexeme;

                    // The third child of the function head is the return type of the function
                    string methodReturnType = methodHead.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme;

                    // Create a new symbol table for the function
                    ISymbolTable methodTable = new SymbolTable(methodName, currentTable);

                    // Create a new symbol table entry for the function
                    ISymbolTableEntry methodEntry = new SymbolTableEntry(methodName, SymbolEntryKind.Method, methodReturnType, methodTable,methodHead.LeftMostChild!.Token!.Location);

                    // Add the function entry to the current table
                    currentTable.AddEntry(methodEntry);

                    // Update the current table to the function table
                    currentTable = methodTable;
                }

                break;
            default:
                break;
        }

        // Visit the leftmost child of the current node and its siblings
        IASTNode? child = LeftMostChild;

        while (child != null)
        {
            child.Visit(currentTable, warnings, errors);
            child = child.RightSibling;
        }
    }


    public void SemanticCheck(ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        switch (Operation)
        {
            case Program:
                // Make sure the program has a main function
                // The main function should be a free function with the name "main" and return type "void"
                ISymbolTableEntry? mainEntry = currentTable.Lookup("main");

                if (mainEntry == null)
                    errors.Add(new SemanticError(SemanticErrorType.MainNotFound, 0,"Main function not found."));

                if(mainEntry!=null)
                {
                    // Make sure the main function returns void
                    if (mainEntry.Type != "void")
                        errors.Add(new SemanticError(SemanticErrorType.MainReturnType, mainEntry.Line,"Main function must return void."));
                
                    ISymbolTable mainTable = mainEntry.Link!;

                    // Make sure the main function has no paramaters
                    if (mainTable.Entries.Count > 0 && mainTable.Entries.Any(e => e.Kind == SymbolEntryKind.Parameter))
                        errors.Add(new SemanticError(SemanticErrorType.MainParameter, mainEntry.Line,"Main function must have no parameters."));
                }

                break;
            case ReturnStat:
                // Make sure the return type of the function matches the return type of the expression

                // Get the return type of the function
                WriteLine("Return Statement");

                break;

            case FuncDef:

                // Make sure the function has a return statement if it is not a void function
                // If the function is a void function, make sure it does not have a return statement
                // Make sure the return type of the function matches the return type of the expression

                // Get the function table entry
                ISymbolTableEntry? functionEntry = currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme);

                // Get the function table
                ISymbolTable functionTable = functionEntry!.Link!;

                // Set the current table to the function table
                currentTable = functionTable;
                


                break;

            case AssignStat:
                // Make sure the type of the variable matches the type of the expression

                IASTNode variable = LeftMostChild!;
                IASTNode expression = variable.RightSibling!;
                
                string variableType = currentTable.Lookup(variable.LeftMostChild!.Token!.Lexeme)!.Type;


                string expressionType = GetType(expression);

                WriteLine("Variable Type: " + variableType);
                WriteLine("Expression Type: " + expressionType);

                

                break;
            default:
                break;
        }


        // Perform the semantic Checks on the leftmost child of the current node and its siblings
        IASTNode? child = LeftMostChild;

        while (child != null)
        {
            child.SemanticCheck(currentTable, warnings, errors);
            child = child.RightSibling;
        }

    }

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

    public IASTNode Peek()
    {
        return _stack.Peek();
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