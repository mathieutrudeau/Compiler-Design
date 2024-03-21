using System.Transactions;
using LexicalAnalyzer;
using SemanticAnalyzer;
using static AbstractSyntaxTreeGeneration.SemanticOperation;
using static System.Console;
using System.Data;
using System;

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


    private string GetType(IASTNode node, ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        // Perform the appropriate action based on the operation of the current node
        switch (node.Operation)
        {
            case IntLit:
                return "integer";
            case FloatLit:
                return "float";
            case SignFactor:
                return GetType(node.LeftMostChild!.RightSibling!, currentTable, warnings, errors);
            case AddExpr:
            case MultExpr:
                return GetType(node.LeftMostChild!, currentTable,warnings,errors) == "float" || GetType(node.LeftMostChild!.RightSibling!.RightSibling!,currentTable,warnings,errors) == "float" ?
                    "float" : "integer";
            case DataMember:
                // Check if the data member is already declared in the current scope
                if (!currentTable.IsAlreadyDeclared(node.LeftMostChild!.Token!.Lexeme))
                    errors.Add(new SemanticError(SemanticErrorType.UndeclaredMember, node.LeftMostChild!.Token!.Location, $"No declaration found for data member '{node.LeftMostChild!.Token!.Lexeme}'."));
                else if (node.LeftMostChild!.RightSibling!.IsLeaf())
                    return currentTable.Lookup(node.LeftMostChild!.Token!.Lexeme)!.Type;
                else
                {
                    // Get the type of the data member
                    string type = currentTable.Lookup(node.LeftMostChild!.Token!.Lexeme)!.Type;

                    // Get the dimensions of the array
                    int dimensions = type.Count(c => c == '[');

                    // Loop through the array indices
                    IASTNode? index = node.LeftMostChild!.RightSibling!.LeftMostChild!;

                    // Check if the array index is of type integer and if the number of indices matches the number of dimensions
                    while (index != null)
                    {
                        string type1 = GetType(index, currentTable, warnings, errors);
                        
                        if (GetType(index, currentTable, warnings, errors) != "integer")
                            errors.Add(new SemanticError(SemanticErrorType.InvalidIndex, node.LeftMostChild!.Token!.Location, "Array index must be of type integer."));
                        else if (dimensions == 0)
                            errors.Add(new SemanticError(SemanticErrorType.InvalidIndex, node.LeftMostChild!.Token!.Location, "Invalid index for non-array type."));
                        else
                            dimensions--;

                        index = index.RightSibling;
                    }

                    // Return the type of the array
                    string dims = "";
                    for (int i = 0; i < dimensions; i++)
                        dims += "[]";

                    return string.Concat(type.AsSpan(0, type.IndexOf('[')), dims); ;
                }

                return "";

            case FuncCall:
                // Check if the function is already declared in the current scope
                if (!currentTable.IsAlreadyDeclared(node.LeftMostChild!.Token!.Lexeme))
                    errors.Add(new SemanticError(SemanticErrorType.UndefinedMethod, node.LeftMostChild!.Token!.Location, $"No declaration found for method '{node.LeftMostChild!.Token!.Lexeme}'."));
                else
                    return currentTable.Lookup(node.LeftMostChild!.Token!.Lexeme)!.Type;

                return "";

            case DotChain:

                // Check if the data member is already declared in the current scope
                string lhs = GetType(node.LeftMostChild!, currentTable, warnings, errors);
                
                if (lhs == "")
                    return "";

                // Set the current table to the class table
                currentTable = currentTable.Lookup(lhs)!.Link!;
                return GetType(node.LeftMostChild!.RightSibling!, currentTable, warnings, errors);

            default:
                return "";
        }


    }


    private static string GetVarType(IASTNode node, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        string varType = node.LeftMostChild!.RightSibling!.Token!.Lexeme;

        // Add any array dimensions to the variable type
        IASTNode? arraySizes = node.LeftMostChild!.RightSibling!.RightSibling!.LeftMostChild;

        while (arraySizes != null)
        {
            if (arraySizes.Operation == ArrayIndex)
                errors.Add(new SemanticError(SemanticErrorType.UndeclaredArraySize, node.LeftMostChild!.Token!.Location, "Array size must be declared."));
            else
            {
                if (int.Parse(arraySizes.Token!.Lexeme) <= 0)
                    errors.Add(new SemanticError(SemanticErrorType.ArraySizeOutOfRange, arraySizes.Token!.Location, "Array size must be greater than 0."));
                else
                    varType += "[" + arraySizes.Token!.Lexeme + "]";
            }

            arraySizes = arraySizes.RightSibling;
        }

        return varType;
    }


    private static string[] GetFunctionParams(IASTNode node, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {

        string[] parameters = Array.Empty<string>();

        // Perform the appropriate action based on the operation of the current node
        switch (node.Operation)
        {

            case FParamList:

                IASTNode? param = node.LeftMostChild;

                while (param != null)
                {
                    string paramType = param.LeftMostChild!.RightSibling!.Token!.Lexeme;

                    // Add any array dimensions to the parameter type
                    IASTNode? arraySize = param.LeftMostChild!.RightSibling!.RightSibling!.LeftMostChild;

                    while (arraySize != null)
                    {
                        if (arraySize.Operation == ArrayIndex)
                        {
                            paramType += "[]";
                            if (!warnings.Any(w => w.Line ==  arraySize.Parent!.Parent!.LeftMostChild!.Token!.Location && w.Type == SemanticWarningType.UndeclaredArraySize))
                                warnings.Add(new SemanticWarning(SemanticWarningType.UndeclaredArraySize, arraySize.Parent!.Parent!.LeftMostChild!.Token!.Location, "Array size not declared. This may lead to unexpected behavior."));
                        }
                        else
                        {
                            if (int.Parse(arraySize.Token!.Lexeme) <= 0 && !errors.Any(e => e.Line == arraySize.Token!.Location && e.Type == SemanticErrorType.ArraySizeOutOfRange))
                                errors.Add(new SemanticError(SemanticErrorType.ArraySizeOutOfRange, arraySize.Token!.Location, "Array size must be greater than 0."));
                            else
                                paramType += "[" + arraySize.Token!.Lexeme + "]";
                        }

                        arraySize = arraySize.RightSibling;
                    }

                    parameters = parameters.Append(paramType).ToArray();
                    param = param.RightSibling;
                }

                break;

            default:
                break;
        }

        return parameters;
    }

    private bool CheckReturn(IASTNode? node, string expectedReturnType, ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors, bool isReturnAllowed=true)
    {
        if (node == null)
            return false;

        bool containsReturn = false;

        while(node != null)
        {
            switch(node.Operation)
            {
                case IfStat:
                    bool firstPath = CheckReturn(node.LeftMostChild!.RightSibling!.LeftMostChild, expectedReturnType, currentTable, warnings, errors, isReturnAllowed);
                    bool secondPath = CheckReturn(node.LeftMostChild!.RightSibling!.RightSibling!.LeftMostChild, expectedReturnType, currentTable, warnings, errors, isReturnAllowed);

                    if ((firstPath || secondPath) && !isReturnAllowed)
                        return true;

                    containsReturn = firstPath && secondPath;

                    if(containsReturn)
                        return true;

                    break;
                case ReturnStat:

                    IASTNode? locationNode = node.LeftMostChild;

                    while(!locationNode!.IsLeaf())
                        locationNode = locationNode.LeftMostChild;

                    string returnType = GetType(node.LeftMostChild!,currentTable,warnings,errors);
                    if(returnType!=expectedReturnType)
                        errors.Add(new SemanticError(SemanticErrorType.InvalidType, locationNode!.Token!.Location, $"Return type '{returnType}' does not match the expected return type '{expectedReturnType}'."));

                    return true;
                default:
                    break;
            }
            node = node.RightSibling;
        }
        return containsReturn;
    }

    private void ValidateFuncReturnType(IASTNode node, string expectedReturnType, ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {   
        switch(expectedReturnType)
        {
            case "void":
                if(CheckReturn(node.LeftMostChild!.RightSibling!.LeftMostChild, expectedReturnType, currentTable, warnings, errors,false))
                    errors.Add(new SemanticError(SemanticErrorType.ReturnOnVoid, node.LeftMostChild!.LeftMostChild!.Token!.Location, "Function with return type 'void' must not contain a return statement."));
                break;
            default:
                if(!CheckReturn(node.LeftMostChild!.RightSibling!.LeftMostChild, expectedReturnType, currentTable, warnings, errors))
                    errors.Add(new SemanticError(SemanticErrorType.NotAllPathsReturn, node.LeftMostChild!.LeftMostChild!.Token!.Location, "All paths must return a value."));
                return;
        }
    } 

    public void Visit(ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        // Perform the following actions when entering a node based on its operation
        switch (Operation)
        {

            case ImplDef:
                /*
                    Handle class implementation.
                    The following steps are performed:
                    - Check if the class is already declared in the current scope
                    - Set the current table to the class table
                */

                // Check if the class is already declared in the current scope
                if (!currentTable.IsAlreadyDeclared(LeftMostChild!.Token!.Lexeme))
                {
                    errors.Add(new SemanticError(SemanticErrorType.UndeclaredClass, LeftMostChild!.Token!.Location, $"No declaration found for class '{LeftMostChild!.Token!.Lexeme}'."));
                    return;
                }

                // Set the current table to the class table
                currentTable = currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!;

                break;

            case StructDecl:
                /*
                    Handle class declaration.
                    The following steps are performed:
                    - Check if the class is already declared in the current scope
                    - Add the class to the global symbol table
                    - Set the current table to the class table
                */

                // Check if the class is already declared in the current scope
                if (currentTable.IsAlreadyDeclared(LeftMostChild!.Token!.Lexeme))
                    errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Class '{LeftMostChild!.Token!.Lexeme}' already declared."));

                // Add the the class declaration to the global symbol table
                currentTable.AddEntry(new SymbolTableEntry
                {
                    Name = LeftMostChild!.Token!.Lexeme,
                    Kind = SymbolEntryKind.ClassDeclaration,
                    Line = LeftMostChild!.Token!.Location,

                    // Create a new symbol table for the class
                    Link = new SymbolTable
                    {
                        Name = LeftMostChild!.Token!.Lexeme,
                        Entries = new LinkedList<ISymbolTableEntry>(),
                        Parent = currentTable
                    }
                });

                // Set the current table to the class table
                currentTable = currentTable.Entries.Last().Link!;

                break;


            case FuncDef:
                /*
                    A function can be a free function or a method.

                    The following steps are performed for a free function:
                    - Check if the function is already declared in the current scope
                    - Add the function to the global symbol table
                    - Set the current table to the function table

                    The following steps are performed for a method:
                    - Check if the method is declared in the class table
                    - If the method is a duplicate definition, add a warning
                    - Create a new symbol table for the methods
                */


                // Check if the function is a free function
                if (Parent!.Operation == Program)
                {
                    // Check if the function is already declared in the current scope
                    if (currentTable.IsAlreadyDeclared(LeftMostChild!.LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!,warnings,errors), SymbolEntryKind.Function))
                        errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.LeftMostChild!.Token!.Location, $"Function '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}' already declared."));
                    // Check if the function is overloading another function
                    else if (currentTable.IsAlreadyDeclared(LeftMostChild!.LeftMostChild!.Token!.Lexeme))
                        warnings.Add(new SemanticWarning(SemanticWarningType.OverloadedFunction, LeftMostChild!.LeftMostChild!.Token!.Location, $"Function '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}' overloads another function."));

                    // Add the function to the global symbol table
                    currentTable.AddEntry(new SymbolTableEntry
                    {
                        Name = LeftMostChild!.LeftMostChild!.Token!.Lexeme,
                        Kind = SymbolEntryKind.Function,
                        Type = LeftMostChild!.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme,
                        Line = LeftMostChild!.LeftMostChild!.Token!.Location,
                        Parameters = GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!,warnings,errors),

                        // Create a new symbol table for the function
                        Link = new SymbolTable
                        {
                            Name = LeftMostChild!.LeftMostChild!.Token!.Lexeme,
                            Entries = new LinkedList<ISymbolTableEntry>(),
                            Parent = currentTable
                        }
                    });

                    // Set the current table to the function table
                    currentTable = currentTable.Entries.Last().Link!;

                }
                // Check if the function is a method
                else if (Parent!.Operation == FuncDefList)
                {

                    // Check if the method is not declared in the class table
                    if (!currentTable.IsAlreadyDeclared(LeftMostChild!.LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!,warnings,errors), SymbolEntryKind.MethodDeclaration))
                    {
                        // Check if the method is a duplicate definition
                        if (currentTable.IsAlreadyDeclared(LeftMostChild!.LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!,warnings,errors), SymbolEntryKind.Method))
                        {
                            errors.Add(new SemanticError(SemanticErrorType.MultipleDefinition, LeftMostChild!.LeftMostChild!.Token!.Location, $"Method '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}' already defined."));
                            return;
                        }
                        // Check if the method is a inherited member
                        else if (currentTable.IsInheritedMethod(LeftMostChild!.LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, warnings,errors), LeftMostChild!.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme))
                        {
                            warnings.Add(new SemanticWarning(SemanticWarningType.ShadowedInheritedMember, LeftMostChild!.LeftMostChild!.Token!.Location, $"Method '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}' shadows the inherited method from a parent class."));

                            // Add a new entry to the class table
                            currentTable.AddEntry(new SymbolTableEntry
                            {
                                Name = LeftMostChild!.LeftMostChild!.Token!.Lexeme,
                                Kind = SymbolEntryKind.MethodDeclaration,
                                Type = LeftMostChild!.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme,
                                Line = LeftMostChild!.LeftMostChild!.Token!.Location,
                                Parameters = GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, warnings, errors),
                                Visibility = VisibilityType.Public,
                            });
                        }
                        else
                        {
                            errors.Add(new SemanticError(SemanticErrorType.UndeclaredMethod, LeftMostChild!.LeftMostChild!.Token!.Location, $"No declaration found for method '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}'."));
                            return;
                        }
                    }

                    // Check if the method is already declared in the current scope with the same parameters
                    if (!currentTable.IsAlreadyDeclared(LeftMostChild!.LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!,warnings,errors), SymbolEntryKind.MethodDeclaration))
                        errors.Add(new SemanticError(SemanticErrorType.UndeclaredMember, LeftMostChild!.LeftMostChild!.Token!.Location, $"No declaration found for method '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}'."));

                    // Create a new symbol table for the method
                    currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme)!.Link = new SymbolTable
                    {
                        Name = LeftMostChild!.LeftMostChild!.Token!.Lexeme,
                        Entries = new LinkedList<ISymbolTableEntry>(),
                        Parent = currentTable
                    };

                    // Update the Entry Kind to Method
                    currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme)!.Kind = SymbolEntryKind.Method;

                    // Set the current table to the method table
                    currentTable = currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme)!.Link!;
                }

                break;

            case FuncHead:
                /*
                    A function head can be for a function definition, a method definition or a method declaration.

                    The following steps are performed for a method declaration:
                    - Check if the method with the same params is already declared in the current scope
                    - Add the method to the class table

                    The following steps are performed for a method definition:
                    - Check if the method is already declared in the current scope with the same parameters. It should be declared in the class table, otherwise it is a duplicate definition.
                    - Create a new symbol table for the method
                    - Update the Entry Kind to Method
                    - Set the current table to the method table

                    The following steps are performed for a function definition:
                    - Add the function parameters to the function table

                */

                // Check if the function head is for a method declaration
                if (Parent!.Operation == StructMember)
                {
                    // Check if the method is already declared in the current scope with the same parameters
                    if (currentTable.IsAlreadyDeclared(LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.RightSibling!,warnings,errors), null))
                        errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Method '{LeftMostChild!.Token!.Lexeme}' already declared."));

                    // Add the method to the class table
                    currentTable.AddEntry(new SymbolTableEntry
                    {
                        Name = LeftMostChild!.Token!.Lexeme,
                        Kind = SymbolEntryKind.MethodDeclaration,
                        Type = LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme,
                        Line = LeftMostChild!.Token!.Location,
                        Parameters = GetFunctionParams(LeftMostChild!.RightSibling!,warnings,errors),
                        Visibility = Parent!.LeftMostChild!.Token!.Lexeme == "public" ? VisibilityType.Public : VisibilityType.Private,
                    });

                }

                // Check if the function head is for a function implementation
                if (Parent!.Operation == FuncDef)
                {
                    // Add the function parameters to the function table
                    IASTNode? param = LeftMostChild!.RightSibling!.LeftMostChild;

                    while (param != null)
                    {
                        string paramType = param.LeftMostChild!.RightSibling!.Token!.Lexeme;

                        // Add any array dimensions to the parameter type
                        IASTNode? arraySize = param.LeftMostChild!.RightSibling!.RightSibling!.LeftMostChild;

                        while (arraySize != null)
                        {
                            if (arraySize.Operation == ArrayIndex)
                                paramType += "[]";
                            else
                                paramType += "[" + arraySize.Token!.Lexeme + "]";

                            arraySize = arraySize.RightSibling;
                        }

                        // Add the parameter to the function table
                        currentTable.AddEntry(new SymbolTableEntry
                        {
                            Name = param.LeftMostChild!.Token!.Lexeme,
                            Kind = SymbolEntryKind.Parameter,
                            Type = paramType,
                            Line = param.LeftMostChild!.Token!.Location
                        });

                        param = param.RightSibling;
                    }


                }


                break;

            case VarDecl:
                /*
                    A variable declaration can be part of a function or a class.

                    The following steps are performed for a variable declaration in a function:
                */



                // Check if the variable is declared in a function or as part of a class
                if (Parent!.Operation == VarDeclOrStatList)
                {
                    // Check if the variable is already declared in the current scope
                    if (currentTable.IsAlreadyDeclared(LeftMostChild!.Token!.Lexeme))
                        errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Variable '{LeftMostChild!.Token!.Lexeme}' already declared."));

                    //WriteLine(currentTable.Name);

                    // Add the variable to the function table
                    currentTable.AddEntry(new SymbolTableEntry
                    {
                        Name = LeftMostChild!.Token!.Lexeme,
                        Kind = SymbolEntryKind.Variable,
                        Type = GetVarType(this, warnings, errors),    
                        Line = LeftMostChild!.Token!.Location
                    });
                }
                else if (Parent!.LeftMostChild!.Operation == Visibility)
                {
                    // Check if the data member is already declared in the current scope
                    if (currentTable.IsAlreadyDeclared(LeftMostChild!.Token!.Lexeme))
                        errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Data member '{LeftMostChild!.Token!.Lexeme}' already declared."));

                    // Add the data member to the class table
                    currentTable.AddEntry(new SymbolTableEntry
                    {
                        Name = LeftMostChild!.Token!.Lexeme,
                        Kind = SymbolEntryKind.Data,
                        Type = GetVarType(this, warnings, errors),
                        Line = LeftMostChild!.Token!.Location,
                        Visibility = Parent!.LeftMostChild!.Token!.Lexeme == "public" ? VisibilityType.Public : VisibilityType.Private,
                    });
                }


                break;


            case StructInheritList:
                /*

                    The following steps are performed for a class inheritance:
                    - Check if the class is already declared in the current scope
                    - Add the inherited class to the current class table and make sure it links to the inherited class table

                */

                // Get the inherited class if it exists
                IASTNode? inheritedClass = LeftMostChild;

                // While there are inherited classes
                while (inheritedClass != null)
                {
                    // Check if the class is already declared in the current scope
                    if (!currentTable.IsAlreadyDeclared(inheritedClass.Token!.Lexeme))
                        errors.Add(new SemanticError(SemanticErrorType.InheritedClassNotFound, inheritedClass.Token!.Location, $"Inherited class '{inheritedClass.Token!.Lexeme}' not found."));
                    else
                    {
                        // Add the inherited class to the current class table and make sure it links to the inherited class table
                        currentTable.AddEntry(new SymbolTableEntry
                        {
                            Name = inheritedClass.Token!.Lexeme,
                            Kind = SymbolEntryKind.Inherit,
                            Line = inheritedClass.Token!.Location,
                            Link = currentTable.Lookup(inheritedClass.Token!.Lexeme)!.Link
                        });
                    }

                    // Move to the next inherited class
                    inheritedClass = inheritedClass.RightSibling;
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

        // Perform the following actions when exiting a node based on its operation
        switch (Operation)
        {
            case ImplDef:
                /*
                    The following steps are performed for a class implementation:
                    - Check if all the methods in the class have been implemented
                    - Set the kind from ClassDeclaration to a Class.
                */

                currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!.Entries.ToList().ForEach(e =>
                {
                    if (e.Kind == SymbolEntryKind.MethodDeclaration)
                        errors.Add(new SemanticError(SemanticErrorType.MethodNotImplemented, e.Line, $"Method '{e.Name}' not implemented."));
                });

                currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Kind = SymbolEntryKind.Class;

                break;

            case Program:
                /*

                    The following steps are performed for the program:

                    - Make sure that all the classes have been implemented

                    - Make sure the program has a main function
                    - The main function should be a free function with the name "main" and return type "void" and no parameters
                */

                // Make sure that all the classes have been implemented
                currentTable.Entries.ToList().ForEach(e =>
                {
                    if (e.Kind == SymbolEntryKind.ClassDeclaration)
                        errors.Add(new SemanticError(SemanticErrorType.ClassNotImplemented, e.Line, $"Class '{e.Name}' not implemented."));
                });

                // Make sure the program has a main function
                if (!currentTable.Entries.Any(e => e.Name == "main" && e.Kind == SymbolEntryKind.Function))
                {
                    errors.Add(new SemanticError(SemanticErrorType.MainNotFound, 0, "Main function not found."));
                }
                else
                {
                    if (!currentTable.Entries.Any(e => e.Name == "main" && e.Kind == SymbolEntryKind.Function && e.Type == "void"))
                        errors.Add(new SemanticError(SemanticErrorType.MainReturnType, currentTable.Lookup("main")!.Line, "Main function must return void."));

                    if (!currentTable.Entries.Any(e => e.Name == "main" && e.Kind == SymbolEntryKind.Function && e.Parameters.Length == 0))
                        errors.Add(new SemanticError(SemanticErrorType.MainParameter, currentTable.Lookup("main")!.Line, "Main function must have no parameters."));
                }

                break;


            default:
                break;
        }
    }


    public void SemanticCheck(ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        switch (Operation)
        {
            case ImplDef:
                /*
                    The following steps are performed for a class implementation:
                    - Set the current table to the class table
                */

                // Set the current table to the class table
                currentTable = currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!;

                break;

            case FuncDef:
                /*

                    The following steps are performed for a function definition:
                    - Set the current table to the function table
                */

                string funcReturnType = currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme)!.Type;

                // Set the current table to the function table
                currentTable = currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme)!.Link!;

                WriteLine(currentTable.Name + " " + funcReturnType);
                ValidateFuncReturnType(this, funcReturnType, currentTable, warnings, errors);


                break;

            case AssignStat:
                /*
                    The following steps are performed for an assignment statement:
                    - Make sure that the variable used in the assignment are declared
                    - Make sure the type of the expression matches the type of the variable
                
                */

                // Get the left and right hand side types
                string lhsType = GetType(LeftMostChild!, currentTable, warnings, errors);
                string rhsType = GetType(LeftMostChild!.RightSibling!, currentTable, warnings, errors);
                IASTNode? node = LeftMostChild;

                while (node!.Token == null)
                    node = node.LeftMostChild;

                // Get the location of the assignment statement
                int location = node.Token!.Location;



                if (lhsType != rhsType && lhsType != "" && rhsType != "")
                    errors.Add(new SemanticError(SemanticErrorType.InvalidType, location, "Type mismatch in assignment statement. Expected " + lhsType + " but got " + rhsType + "."));

                break;

            case VarDecl:
                /*
                    The following steps are performed for a variable declaration:
                    - Make sure that the variable type is valid
                */

                if (LeftMostChild!.RightSibling!.Token!.Lexeme != "integer" && LeftMostChild!.RightSibling!.Token!.Lexeme != "float"
                    && !currentTable.IsAlreadyDeclared(LeftMostChild!.RightSibling!.Token!.Lexeme, Array.Empty<string>(), SymbolEntryKind.Class))
                    errors.Add(new SemanticError(SemanticErrorType.UndeclaredType, LeftMostChild!.RightSibling!.Token!.Location, $"No declaration found for type '{LeftMostChild!.RightSibling!.Token!.Lexeme}'."));


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