using System.Transactions;
using LexicalAnalyzer;
using SemanticAnalyzer;
using static AbstractSyntaxTreeGeneration.SemanticOperation;
using static System.Console;
using System.Data;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using CodeGenerator;

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

    #region Methods for Semantic Analysiss

    /// <summary>
    /// Gets the type of the given node.
    /// </summary>
    /// <param name="node"> The node to get the type of. </param>
    /// <param name="currentTable"> The current symbol table. </param>
    /// <param name="warnings"> The list of semantic warnings. </param>
    /// <param name="errors"> The list of semantic errors. </param>
    /// <returns> The type of the node. If the node does not have  </returns>
    private string GetType(IASTNode node, ISymbolTable currentTable, ISymbolTable callScope, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        // Perform the appropriate action based on the operation of the current node
        switch (node.Operation)
        {
            case IntLit:
                // Return the type of the integer literal, which is obviously an integer
                return "integer";

            case FloatLit:
                // Return the type of the float literal, which is obviously a float
                return "float";

            case SignFactor:
                // The type of the sign factor is the same as the type of the factor
                return GetType(node.LeftMostChild!.RightSibling!, currentTable, callScope, warnings, errors);

            case AddExpr:
            case MultExpr:
            case RelExpr:
                // When the operation is an addition or multiplication expression, the type is determined by the types of the operands
                // The float type takes precedence over the integer type
                return GetType(node.LeftMostChild!, currentTable, callScope, warnings, errors) == "float" || GetType(node.LeftMostChild!.RightSibling!.RightSibling!, currentTable, callScope, warnings, errors) == "float" ?
                    "float" : "integer";

            case DataMember:

                // Check if the data member is already declared in the current scope
                if (!currentTable.IsAccessibleWithinScope(node.LeftMostChild!.Token!.Lexeme, node.LeftMostChild!.Token!.Location, callScope, warnings, errors, Array.Empty<string>(), false, null))
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
                        string type1 = GetType(index, currentTable, callScope, warnings, errors);

                        if (GetType(index, currentTable, callScope, warnings, errors) != "integer")
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

                    if (!type.Contains('['))
                        return type;

                    return string.Concat(type.AsSpan(0, type.IndexOf('[')), dims); ;
                }

                return "";

            case FuncCall:
                // Check if the function is already declared in the current scope
                if (!currentTable.IsAccessibleWithinScope(node.LeftMostChild!.Token!.Lexeme, node.LeftMostChild!.Token!.Location, callScope, warnings, errors, GetFunctionParams(node.LeftMostChild!.RightSibling!, callScope, warnings, errors), true, SymbolEntryKind.Function)
                && !currentTable.IsAccessibleWithinScope(node.LeftMostChild!.Token!.Lexeme, node.LeftMostChild!.Token!.Location, callScope, warnings, errors, GetFunctionParams(node.LeftMostChild!.RightSibling!, callScope, warnings, errors), true, SymbolEntryKind.Method))
                {
                    string parameters = string.Join(", ", GetFunctionParams(node.LeftMostChild!.RightSibling!, callScope, warnings, errors));
                    errors.Add(new SemanticError(SemanticErrorType.UndeclaredFunction, node.LeftMostChild!.Token!.Location, $"No declaration found for function/method '{node.LeftMostChild!.Token!.Lexeme}({parameters})'."));
                }
                else
                    return currentTable.Lookup(node.LeftMostChild!.Token!.Lexeme)!.Type;

                return "";

            case DotChain:

                // Check if the data member is already declared in the current scope
                string lhs = GetType(node.LeftMostChild!, currentTable, callScope, warnings, errors);

                if (lhs == "")
                    return "";

                if (lhs == "integer" || lhs == "float")
                {
                    // Find the location of the dot operator
                    IASTNode? locationNode = node.LeftMostChild;
                    while (!locationNode!.IsLeaf())
                        locationNode = locationNode.LeftMostChild;

                    errors.Add(new SemanticError(SemanticErrorType.IllegalChaining, locationNode.Token!.Location, $"'{lhs}' cannot be chained. Only class types can be chained using the dot operator."));
                    return "";
                }

                // Set the current table to the class table
                currentTable = currentTable.Lookup(lhs)!.Link!;
                return GetType(node.LeftMostChild!.RightSibling!, currentTable, callScope, warnings, errors);

            default:
                return "";
        }
    }

    /// <summary>
    /// Get the type of the variable declaration. 
    /// </summary>
    /// <param name="node"> The node to get the type of. </param>
    /// <param name="warnings"> The list of semantic warnings. </param>
    /// <param name="errors"> The list of semantic errors. </param>
    /// <returns> The type of the variable declaration. </returns>
    /// <remarks>
    /// The type of the variable declaration is determined by the type of the variable and any array dimensions.
    /// </remarks>
    private static string GetVarType(IASTNode node, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        // Get the type of the variable
        string varType = node.LeftMostChild!.RightSibling!.Token!.Lexeme;

        // Add any array dimensions to the variable type
        IASTNode? arraySizes = node.LeftMostChild!.RightSibling!.RightSibling!.LeftMostChild;

        // Loop through the array dimensions
        while (arraySizes != null)
        {
            // Check if the array size is declared or not
            if (arraySizes.Operation == ArrayIndex)
                errors.Add(new SemanticError(SemanticErrorType.UndeclaredArraySize, node.LeftMostChild!.Token!.Location, "Array size must be declared."));
            // Add any array dimensions to the variable type
            else
            {
                if (int.Parse(arraySizes.Token!.Lexeme) <= 0)
                    errors.Add(new SemanticError(SemanticErrorType.ArraySizeOutOfRange, arraySizes.Token!.Location, "Array size must be greater than 0."));
                else
                    varType += "[" + arraySizes.Token!.Lexeme + "]";
            }

            // Move to the next array dimension
            arraySizes = arraySizes.RightSibling;
        }

        return varType;
    }

    /// <summary>
    /// Gets the parameters/arguments of the given function call or function definition.
    /// </summary>
    /// <param name="node"> The node to get the parameters/arguments of. </param>
    /// <param name="currentTable"> The current symbol table. </param>
    /// <param name="warnings"> The list of semantic warnings. </param>
    /// <param name="errors"> The list of semantic errors. </param>
    /// <returns> The parameters/arguments of the function call or function definition. </returns>
    /// <remarks>
    /// The parameters/arguments are returned as an array of strings, where each string represents the type of a parameter/argument.
    /// </remarks>
    private string[] GetFunctionParams(IASTNode node, ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        // Initialize the parameters
        string[] parameters = Array.Empty<string>();

        // Perform the appropriate action based on the operation of the current node
        switch (node.Operation)
        {
            case FParamList:
                /*
                    Handle function parameters.

                    The following steps are performed:
                    - Get the type of each parameter
                    - Add any array dimensions to the parameter type
                */

                IASTNode? param = node.LeftMostChild;

                // Loop through the parameters
                while (param != null)
                {
                    // Get the type of the parameter
                    string paramType = param.LeftMostChild!.RightSibling!.Token!.Lexeme;

                    // Add any array dimensions to the parameter type
                    IASTNode? arraySize = param.LeftMostChild!.RightSibling!.RightSibling!.LeftMostChild;

                    // Loop through the array dimensions
                    while (arraySize != null)
                    {
                        // Check if the array size is declared or not
                        if (arraySize.Operation == ArrayIndex)
                        {
                            // Add a warning if the array size is not declared
                            paramType += "[]";
                            if (!warnings.Any(w => w.Line == arraySize.Parent!.Parent!.LeftMostChild!.Token!.Location && w.Type == SemanticWarningType.UndeclaredArraySize))
                                warnings.Add(new SemanticWarning(SemanticWarningType.UndeclaredArraySize, arraySize.Parent!.Parent!.LeftMostChild!.Token!.Location, "Array size not declared. This may lead to unexpected behavior."));
                        }
                        else
                        {
                            // Add any array dimensions to the parameter type
                            // Add an error if the array size is less than or equal to 0
                            if (int.Parse(arraySize.Token!.Lexeme) <= 0 && !errors.Any(e => e.Line == arraySize.Token!.Location && e.Type == SemanticErrorType.ArraySizeOutOfRange))
                                errors.Add(new SemanticError(SemanticErrorType.ArraySizeOutOfRange, arraySize.Token!.Location, "Array size must be greater than 0."));
                            else
                                paramType += "[" + arraySize.Token!.Lexeme + "]";
                        }

                        // Move to the next array dimension
                        arraySize = arraySize.RightSibling;
                    }

                    // Add the parameter type to the parameters array
                    parameters = parameters.Append(paramType).ToArray();

                    // Move to the next parameter
                    param = param.RightSibling;
                }

                break;

            case AParamList:
                /*
                    Handle function arguments.

                    The following steps are performed:
                    - Get the type of each argument
                    - Add any array dimensions to the argument type
                */

                IASTNode? arg = node.LeftMostChild;

                // Loop through the arguments
                while (arg != null)
                {
                    // Check if the argument is a function call, or any other type of argument
                    if (arg.Operation == FuncCall)
                        parameters = parameters.Append(GetType(arg.LeftMostChild!, currentTable, currentTable, warnings, errors)).ToArray();
                    else
                        parameters = parameters.Append(GetType(arg, currentTable, currentTable, warnings, errors)).ToArray();

                    // Move to the next argument
                    arg = arg.RightSibling;
                }

                break;

            default:
                break;
        }

        return parameters;
    }

    /// <summary>
    /// Checks if a return statement is present in the function.
    /// </summary>
    /// <param name="node"> The node to check for a return statement. </param>
    /// <param name="expectedReturnType"> The expected return type of the function. </param>
    /// <param name="currentTable"> The current symbol table. </param>
    /// <param name="warnings"> The list of semantic warnings. </param>
    /// <param name="errors"> The list of semantic errors. </param>
    /// <param name="isReturnAllowed"> A flag indicating if a return statement is allowed. </param>
    /// <returns> A flag indicating if a return statement is present in the function. </returns>
    /// <remarks>
    /// The function checks if a return statement is present in the function. If a return statement is present, the function checks if the return type matches the expected return type.
    /// </remarks>
    private bool CheckReturn(IASTNode? node, string expectedReturnType, ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors, bool isReturnAllowed = true)
    {
        // If the node is null, return false
        if (node == null)
            return false;

        // Initialize a flag indicating if a return statement is present in the function
        bool containsReturn = false;

        // Loop through the nodes
        while (node != null)
        {
            // Perform the appropriate action based on the operation of the current node
            switch (node.Operation)
            {
                case IfStat:
                    /*
                        Handle if statement.

                        The following steps are performed:
                        - Check if the if statement contains a return statement in both paths
                        - Check if the return type of the if statement matches the expected return type
                    */

                    // Check if the if statement contains a return statement in both paths
                    bool firstPath = CheckReturn(node.LeftMostChild!.RightSibling!.LeftMostChild, expectedReturnType, currentTable, warnings, errors, isReturnAllowed);
                    bool secondPath = CheckReturn(node.LeftMostChild!.RightSibling!.RightSibling!.LeftMostChild, expectedReturnType, currentTable, warnings, errors, isReturnAllowed);

                    // Check if any path contains a return statement when a return statement is not allowed
                    if ((firstPath || secondPath) && !isReturnAllowed)
                        return true;

                    // Check if the if statement contains a return statement in both paths
                    containsReturn = firstPath && secondPath;

                    // Return true if the if statement contains a return statement in both paths
                    if (containsReturn)
                        return true;

                    break;

                case ReturnStat:
                    /*
                        Handle return statement.

                        The following steps are performed:
                        - Check if the return type matches the expected return type
                    */

                    // Get the location of the return statement
                    IASTNode? locationNode = node.LeftMostChild;

                    while (!locationNode!.IsLeaf())
                        locationNode = locationNode.LeftMostChild;

                    // Get the return type of the return statement
                    string returnType = GetType(node.LeftMostChild!, currentTable, currentTable, warnings, errors);

                    // Check if the return type matches the expected return type
                    if (returnType != expectedReturnType && (returnType.Replace("integer", "float") != expectedReturnType))
                        errors.Add(new SemanticError(SemanticErrorType.InvalidType, locationNode!.Token!.Location, $"Return type '{returnType}' does not match the expected return type '{expectedReturnType}'."));

                    // Return true, indicating that a return statement is present in this path
                    return true;
                default:
                    break;
            }

            // Move to the next node
            node = node.RightSibling;
        }
        return containsReturn;
    }

    /// <summary>
    /// Validates the return type of a function.
    /// </summary>
    /// <param name="node"> The node to validate. </param>
    /// <param name="expectedReturnType"> The expected return type of the function. </param>
    /// <param name="currentTable"> The current symbol table. </param>
    /// <param name="warnings"> The list of semantic warnings. </param>
    /// <param name="errors"> The list of semantic errors. </param>
    private void ValidateFuncReturnType(IASTNode node, string expectedReturnType, ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {
        // Perform the appropriate action based on the operation of the current node
        switch (expectedReturnType)
        {
            // Check if the return type is void
            case "void":

                // Check if the function contains a return statement, which is not allowed for functions with return type void
                if (CheckReturn(node.LeftMostChild!.RightSibling!.LeftMostChild, expectedReturnType, currentTable, warnings, errors, false))
                    errors.Add(new SemanticError(SemanticErrorType.ReturnOnVoid, node.LeftMostChild!.LeftMostChild!.Token!.Location, "Function with return type 'void' must not contain a return statement."));
                break;

            // Check if the return type is not void
            default:
                // Check if the function contains a return statement, which is required for functions with a return type other than void
                if (!CheckReturn(node.LeftMostChild!.RightSibling!.LeftMostChild, expectedReturnType, currentTable, warnings, errors))
                    errors.Add(new SemanticError(SemanticErrorType.NotAllPathsReturn, node.LeftMostChild!.LeftMostChild!.Token!.Location, "All paths must return a value."));
                return;
        }
    }

    /// <summary>
    /// Visits the abstract syntax tree node.
    /// </summary>
    /// <param name="currentTable"> The current symbol table. </param>
    /// <param name="warnings"> The list of semantic warnings. </param>
    /// <param name="errors"> The list of semantic errors. </param>
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
                if (!currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), false, SymbolEntryKind.ClassDeclaration))
                {
                    errors.Add(new SemanticError(SemanticErrorType.UndeclaredClass, LeftMostChild!.Token!.Location, $"No declaration found for class '{LeftMostChild!.Token!.Lexeme}'."));
                    return;
                }
                // Check if the class is already defined in the current scope
                else if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), false, SymbolEntryKind.Class))
                {
                    errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Class '{LeftMostChild!.Token!.Lexeme}' already defined."));
                    return;
                }
                // Otherwise, set the current table to the class table
                else
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
                if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), false, SymbolEntryKind.ClassDeclaration))
                {
                    errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Class '{LeftMostChild!.Token!.Lexeme}' already declared."));

                    // Set the current table to the class table
                    currentTable = currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!;
                }
                // Check if the class is already declared
                else if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), false, SymbolEntryKind.Class))
                {
                    errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Class '{LeftMostChild!.Token!.Lexeme}' already declared."));
                    return;
                }
                // Check if the identifier is already used by a free function
                else if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>()))
                {
                    errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Identifier '{LeftMostChild!.Token!.Lexeme}' already used by a free function."));
                    return;
                }
                // If the class is not declared, add the class to the global symbol table
                else
                {
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
                }

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
                    if (currentTable.IsAccessibleWithinScope(LeftMostChild!.LeftMostChild!.Token!.Lexeme, LeftMostChild!.LeftMostChild!.Token!.Location, currentTable, warnings, errors, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors), true, SymbolEntryKind.Function))
                        errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.LeftMostChild!.Token!.Location, $"Function '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}' already declared."));
                    // Check if the function is overloading another function
                    else if (currentTable.IsAccessibleWithinScope(LeftMostChild!.LeftMostChild!.Token!.Lexeme, LeftMostChild!.LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), true, SymbolEntryKind.Function))
                        warnings.Add(new SemanticWarning(SemanticWarningType.OverloadedFunction, LeftMostChild!.LeftMostChild!.Token!.Location, $"Function '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}' overloads another function."));

                    // Add the function to the global symbol table
                    currentTable.AddEntry(new SymbolTableEntry
                    {
                        Name = LeftMostChild!.LeftMostChild!.Token!.Lexeme,
                        Kind = SymbolEntryKind.Function,
                        Type = LeftMostChild!.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme,
                        Line = LeftMostChild!.LeftMostChild!.Token!.Location,
                        Parameters = GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors),

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
                    // Check if the method is declared in the class table
                    if (currentTable.IsAccessibleWithinScope(LeftMostChild!.LeftMostChild!.Token!.Lexeme, LeftMostChild!.LeftMostChild!.Token!.Location, currentTable, warnings, errors, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors), true, SymbolEntryKind.MethodDeclaration))
                    {
                        // Check if the method is declared in the inherited class table
                        if (warnings.Any(w => w.Line == LeftMostChild!.LeftMostChild!.Token!.Location && w.Type == SemanticWarningType.ShadowedInheritedMember))
                        {
                            // If so create a new entry in the current class table
                            currentTable.AddEntry(new SymbolTableEntry
                            {
                                Name = LeftMostChild!.LeftMostChild!.Token!.Lexeme,
                                Kind = SymbolEntryKind.Method,
                                Type = LeftMostChild!.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme,
                                Line = LeftMostChild!.LeftMostChild!.Token!.Location,
                                Parameters = GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors),
                                Visibility = VisibilityType.Public,
                                Link = new SymbolTable
                                {
                                    Name = LeftMostChild!.LeftMostChild!.Token!.Lexeme,
                                    Entries = new LinkedList<ISymbolTableEntry>(),
                                    Parent = currentTable
                                }
                            });

                            // Set the current table to the method table
                            currentTable = currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors), SymbolEntryKind.Method)!.Link!;
                        }
                        else
                        {
                            // If the method is already declared in the current class table, update the entry kind to method
                            // Create a new symbol table for the method
                            // and set the current table to the method table

                            currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors), null)!.Kind = SymbolEntryKind.Method;
                            currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors), SymbolEntryKind.Method)!.Link = new SymbolTable
                            {
                                Name = LeftMostChild!.LeftMostChild!.Token!.Lexeme,
                                Entries = new LinkedList<ISymbolTableEntry>(),
                                Parent = currentTable
                            };

                            currentTable = currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors), SymbolEntryKind.Method)!.Link!;

                        }
                    }
                    else
                    {
                        // Check if the method is a duplicate definition
                        if (currentTable.IsAccessibleWithinScope(LeftMostChild!.LeftMostChild!.Token!.Lexeme, LeftMostChild!.LeftMostChild!.Token!.Location, currentTable, warnings, errors, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors), true, SymbolEntryKind.Method))
                        {
                            // Allow the method to be a duplicate definition if it is a inherited member 
                            // otherwise add an error
                            if (!warnings.Any(w => w.Line == LeftMostChild!.LeftMostChild!.Token!.Location && w.Type == SemanticWarningType.ShadowedInheritedMember))
                            {
                                string parameters = string.Join(", ", GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors));
                                errors.Add(new SemanticError(SemanticErrorType.MultipleDefinition, LeftMostChild!.LeftMostChild!.Token!.Location, $"Method '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}({parameters})' already defined."));
                                return;
                            }
                        }
                        // Otherwise add an error
                        else
                        {
                            string parameters = string.Join(", ", GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors));
                            errors.Add(new SemanticError(SemanticErrorType.UndeclaredMethod, LeftMostChild!.LeftMostChild!.Token!.Location, $"No declaration found for method '{LeftMostChild!.LeftMostChild!.Token!.Lexeme}({parameters})'."));
                            return;
                        }
                    }
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
                    if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, GetFunctionParams(LeftMostChild!.RightSibling!, currentTable, warnings, errors), true, SymbolEntryKind.MethodDeclaration))
                    {
                        // Make sure a ShadowedInheritedMember warning is not already present
                        if (!warnings.Any(w => w.Line == LeftMostChild!.Token!.Location && w.Type == SemanticWarningType.ShadowedInheritedMember))
                        {
                            string parameters = string.Join(", ", GetFunctionParams(LeftMostChild!.RightSibling!, currentTable, warnings, errors));
                            errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Method '{LeftMostChild!.Token!.Lexeme}({parameters})' already declared."));
                        }
                    }
                    else if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), false, SymbolEntryKind.MethodDeclaration))
                    {
                        string parameters = string.Join(", ", GetFunctionParams(LeftMostChild!.RightSibling!, currentTable, warnings, errors));
                        warnings.Add(new SemanticWarning(SemanticWarningType.OverloadedMethod, LeftMostChild!.Token!.Location, $"Method '{LeftMostChild!.Token!.Lexeme}({parameters})' overloads another method."));
                    }
                    else if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>()))
                        errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Identifier '{LeftMostChild!.Token!.Lexeme}' already used within the scope."));

                    // Add the method to the class table
                    currentTable.AddEntry(new SymbolTableEntry
                    {
                        Name = LeftMostChild!.Token!.Lexeme,
                        Kind = SymbolEntryKind.MethodDeclaration,
                        Type = LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme,
                        Line = LeftMostChild!.Token!.Location,
                        Parameters = GetFunctionParams(LeftMostChild!.RightSibling!, currentTable, warnings, errors),
                        Visibility = Parent!.LeftMostChild!.Token!.Lexeme == "public" ? VisibilityType.Public : VisibilityType.Private,
                    });
                }

                // Check if the function head is for a function implementation, if so add the parameters to the function table
                if (Parent!.Operation == FuncDef)
                {
                    // Add the function parameters to the function table
                    IASTNode? param = LeftMostChild!.RightSibling!.LeftMostChild;

                    // Loop through the parameters
                    while (param != null)
                    {
                        // Get the type of the parameter
                        string paramType = param.LeftMostChild!.RightSibling!.Token!.Lexeme;

                        // Add any array dimensions to the parameter type
                        IASTNode? arraySize = param.LeftMostChild!.RightSibling!.RightSibling!.LeftMostChild;

                        // Loop through the array dimensions
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

                        // Move to the next parameter
                        param = param.RightSibling;
                    }
                }

                break;

            case VarDecl:
                /*
                    A variable declaration can be part of a function or a class.

                    The following steps are performed for a variable declaration in a function:
                    - Check if the variable is already declared in the current scope
                    - Add the variable to the function table

                    The following steps are performed for a variable declaration in a class:
                    - Check if the data member is already declared in the current scope
                    - Add the data member to the class table
                */

                // Check if the variable is declared in a function
                if (Parent!.Operation == VarDeclOrStatList)
                {
                    // Check if the variable is already declared in the current scope
                    if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>()))
                        errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Cannot declare variable '{LeftMostChild!.Token!.Lexeme}'. The identifier is already in use."));

                    // Add the variable to the function table
                    currentTable.AddEntry(new SymbolTableEntry
                    {
                        Name = LeftMostChild!.Token!.Lexeme,
                        Kind = SymbolEntryKind.Variable,
                        Type = GetVarType(this, warnings, errors),
                        Line = LeftMostChild!.Token!.Location
                    });
                }
                // Check if the variable is declared as part of a class
                else if (Parent!.LeftMostChild!.Operation == Visibility)
                {
                    // Check if the data member is already declared in the current scope
                    if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>()))
                        errors.Add(new SemanticError(SemanticErrorType.MultipleDeclaration, LeftMostChild!.Token!.Location, $"Cannot declare data member '{LeftMostChild!.Token!.Lexeme}'. The identifier is already in use."));
                    // Check if the data member is shadowing an inherited member
                    else if (errors.Any(e => e.Line == LeftMostChild!.Token!.Location && e.Type == SemanticErrorType.UndeclaredMember))
                    {
                        errors.Remove(errors.First(e => e.Line == LeftMostChild!.Token!.Location && e.Type == SemanticErrorType.UndeclaredMember));
                        warnings.Add(new SemanticWarning(SemanticWarningType.ShadowedInheritedMember, LeftMostChild!.Token!.Location, $"Data member '{LeftMostChild!.Token!.Lexeme}' shadows an inherited member."));
                    }

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
                    - The inherited class must be declared or defined prior to being inherited. This prevents circular dependencies.

                */

                // Get the inherited class if it exists
                IASTNode? inheritedClass = LeftMostChild;

                // While there are inherited classes
                while (inheritedClass != null)
                {
                    // Check if the class is already declared in the current scope
                    if (!currentTable.IsAccessibleWithinScope(inheritedClass.Token!.Lexeme, inheritedClass.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), false, SymbolEntryKind.ClassDeclaration)
                    && !currentTable.IsAccessibleWithinScope(inheritedClass.Token!.Lexeme, inheritedClass.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), false, SymbolEntryKind.Class))
                        errors.Add(new SemanticError(SemanticErrorType.InheritedClassNotFound, inheritedClass.Token!.Location, $"No declaration found for inherited class '{inheritedClass.Token!.Lexeme}'."));
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

                // Check if all the methods in the class have been implemented
                currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!.Entries.ToList().ForEach(e =>
                {
                    if (e.Kind == SymbolEntryKind.MethodDeclaration)
                    {
                        string parameters = string.Join(", ", e.Parameters);
                        errors.Add(new SemanticError(SemanticErrorType.MethodNotImplemented, e.Line, $"Method '{e.Name}({parameters})' not implemented."));
                    }
                });

                // Set the kind from ClassDeclaration to a Class
                currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Kind = SymbolEntryKind.Class;

                // Set the size of the class in the symbol table.
                // The size of the class is the sum of the sizes of its data members.
                //currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Size = currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!.Entries.Where(e => e.Kind == SymbolEntryKind.Data).Sum(e => e.Size);

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
                    // Make sure the main function is a free function with the name "main" and return type "void" and no parameters
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

    /// <summary>
    /// Performs semantic analysis on the abstract syntax tree node.
    /// </summary>
    /// <param name="currentTable"> The current symbol table. </param>
    /// <param name="warnings"> The list of semantic warnings. </param>
    /// <param name="errors"> The list of semantic errors. </param>
    public void SemanticCheck(ISymbolTable currentTable, List<ISemanticWarning> warnings, List<ISemanticError> errors)
    {

        // Perform the semantic checks based on the operation of the current node
        switch (Operation)
        {
            case ImplDef:
                /*
                    The following steps are performed for a class implementation:
                    - Set the current table to the class table
                */

                // Set the current table to the class table if it is already declared
                if (currentTable.IsAccessibleWithinScope(LeftMostChild!.Token!.Lexeme, LeftMostChild!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), false, SymbolEntryKind.Class))
                    currentTable = currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!;

                break;

            case FuncDef:
                /*
                    The following steps are performed for a function definition:
                    
                    - Get the return type of the function
                    - Set the current table to the function table
                    - Validate the return type of the function
                */

                // Check if the function or method is already declared in the current scope
                if (!currentTable.IsAccessibleWithinScope(LeftMostChild!.LeftMostChild!.Token!.Lexeme, LeftMostChild!.LeftMostChild!.Token!.Location, currentTable, warnings, errors, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors), true, SymbolEntryKind.Function)
                && !currentTable.IsAccessibleWithinScope(LeftMostChild!.LeftMostChild!.Token!.Lexeme, LeftMostChild!.LeftMostChild!.Token!.Location, currentTable, warnings, errors, GetFunctionParams(LeftMostChild!.LeftMostChild!.RightSibling!, currentTable, warnings, errors), true, SymbolEntryKind.Method))
                    return;

                // Get the return type of the function
                string funcReturnType = LeftMostChild!.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme;

                // Set the current table to the function table
                currentTable = currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme)!.Link!;

                // Validate the return type of the function
                ValidateFuncReturnType(this, funcReturnType, currentTable, warnings, errors);

                break;

            case AssignStat:
                /*
                    The following steps are performed for an assignment statement:
                    - Make sure that the variable used in the assignment are declared
                    - Make sure the type of the expression matches the type of the variable
                
                */

                // Get the left and right hand side types
                string lhsType = GetType(LeftMostChild!, currentTable, currentTable, warnings, errors);
                string rhsType = GetType(LeftMostChild!.RightSibling!, currentTable, currentTable, warnings, errors);
                IASTNode? node = LeftMostChild;

                while (node!.Token == null)
                    node = node.LeftMostChild;

                // Get the location of the assignment statement
                int location = node.Token!.Location;

                // Add an error if the type of the expression does not match the type of the variable
                if (lhsType != rhsType && lhsType != "" && rhsType != "")
                    errors.Add(new SemanticError(SemanticErrorType.InvalidType, location, "Type mismatch in assignment statement. Expected " + lhsType + " but got " + rhsType + "."));

                break;

            case VarDecl:
                /*
                    The following steps are performed for a variable declaration:
                    - Make sure that the variable type is valid
                */

                // Check if the variable type is valid (i.e. declared in the current scope or a built-in type)
                if (LeftMostChild!.RightSibling!.Token!.Lexeme != "integer" && LeftMostChild!.RightSibling!.Token!.Lexeme != "float"
                    && !currentTable.IsAccessibleWithinScope(LeftMostChild!.RightSibling!.Token!.Lexeme, LeftMostChild!.RightSibling!.Token!.Location, currentTable, warnings, errors, Array.Empty<string>(), false, SymbolEntryKind.Class))
                {
                    errors.Add(new SemanticError(SemanticErrorType.UndeclaredType, LeftMostChild!.RightSibling!.Token!.Location, $"No declaration found for type '{LeftMostChild!.RightSibling!.Token!.Lexeme}'."));
                    return;
                }

                // If the variable type is a class, set the size of the variable to the size of the class
                if (LeftMostChild!.RightSibling!.Token!.Lexeme != "integer" && LeftMostChild!.RightSibling!.Token!.Lexeme != "float")
                {
                    int arraySize = 1;

                    // Get the array size of the variable
                    IASTNode? arraySizeNode = LeftMostChild!.RightSibling!.RightSibling!.LeftMostChild;

                    while (arraySizeNode != null)
                    {
                        arraySize *= int.Parse(arraySizeNode.Token!.Lexeme);
                        arraySizeNode = arraySizeNode.RightSibling;
                    }

                    try
                    {
                        currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Size = currentTable.Lookup(LeftMostChild!.RightSibling!.Token!.Lexeme, Array.Empty<string>(), SymbolEntryKind.Class)!.Size * arraySize;
                        currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link = currentTable.Lookup(LeftMostChild!.RightSibling!.Token!.Lexeme, Array.Empty<string>(), SymbolEntryKind.Class)!.Link;
                    }
                    catch (Exception)
                    {
                        errors.Add(new SemanticError(SemanticErrorType.UndeclaredType, LeftMostChild!.RightSibling!.Token!.Location, $"Type '{LeftMostChild!.RightSibling!.Token!.Lexeme}' cannot be used as a data member within its declaration scope."));
                        return;
                    }
                }

                break;

            case RelExpr:
            case AddExpr:
            case MultExpr:
                /*
                    The following steps are performed for a relational, addition or multiplication expression:
                    - Make sure that the types of the operands are valid
                */

                // Get the left and right hand side types
                string leftType = GetType(LeftMostChild!, currentTable, currentTable, warnings, errors);
                string rightType = GetType(LeftMostChild!.RightSibling!.RightSibling!, currentTable, currentTable, warnings, errors);

                // Get the location of the expression
                int loc = LeftMostChild!.RightSibling!.Token!.Location;

                // Check if the types are valid
                if (leftType != rightType && leftType != "" && rightType != "")
                    errors.Add(new SemanticError(SemanticErrorType.InvalidType, loc, "Type mismatch in expression. Expected " + leftType + " but got " + rightType + "."));


                break;

            case FuncCall:
            case DotChain:
            case DataMember:
                /*
                    The following steps are performed for a function call, dot chain or data member:
                    - Make sure that the reference is not part of a chain (i.e. not a child of a dot chain or function call)
                    - Get the type of the reference (this will also check if the reference is valid)
                */

                // Make sure that the reference is not part of a chain
                if (Parent!.Operation != DotChain && Parent!.Operation != FuncCall)
                {
                    if (Operation == FuncCall)
                        GetType(LeftMostChild!, currentTable, currentTable, warnings, errors);
                    else
                        GetType(this, currentTable, currentTable, warnings, errors);
                }

                break;

            case Identifier:
                /*
                    The following steps are performed for an identifier:
                    - Check if the identifier is declared
                    - Add a reference count to the identifier
                */

                // Get the Operation of the parent node
                SemanticOperation parentOp = Parent!.Operation;

                switch (parentOp)
                {
                    case FuncHead:
                    case VarDecl:
                    case FParam:
                        return;
                }

                // Look up the identifier in the symbol table
                if (currentTable.Lookup(Token!.Lexeme) != null)
                    // Add a reference count to the identifier
                    currentTable.Lookup(Token!.Lexeme)!.ReferencesCount++;

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

        switch (Operation)
        {
            case Program:
                if (errors.Count == 0)
                    currentTable.CurateTable();
                break;

            case RelExpr:
            case AddExpr:
            case MultExpr:
                /*
                    The following steps are performed for a relational, addition or multiplication expression:
                    - Make sure that the types of the operands are valid
                */

                // Get the left and right hand side types
                string leftType = GetType(LeftMostChild!, currentTable, currentTable, warnings, errors);
                string rightType = GetType(LeftMostChild!.RightSibling!.RightSibling!, currentTable, currentTable, warnings, errors);

                // Get the location of the expression
                int loc = LeftMostChild!.RightSibling!.Token!.Location;




                break;

            case FuncDef:

                // Unless the function is main, add an entry to store the jump address location
                if (LeftMostChild!.LeftMostChild!.Token!.Lexeme != "main")
                {
                    currentTable.AddEntry(new SymbolTableEntry
                    {
                        Name = "jumpAddr",
                        Kind = SymbolEntryKind.JumpAddress,
                        Line = LeftMostChild!.LeftMostChild!.Token!.Location,
                        Size = 4
                    });

                    // Add an entry to store the return value of the function
                    currentTable.AddEntry(new SymbolTableEntry
                    {
                        Name = "returnVal",
                        Kind = SymbolEntryKind.ReturnVal,
                        Type = LeftMostChild!.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme,
                        Line = LeftMostChild!.LeftMostChild!.Token!.Location,
                        Size = LeftMostChild!.LeftMostChild!.RightSibling!.RightSibling!.Token!.Lexeme == "float" ? 8 : 4
                    });
                }

                break;
        }

    }

    #endregion Methods for Semantic Analysis

    #region Methods for Code Generation


    public void GenerateCode(ISymbolTable currentTable, IMoonCodeGenerator moonCodeGenerator, ref int currentScopeSize, ref ISymbolTable chainedTable, ref bool isArray)
    {

        switch (Operation)
        {
            case DotChain:
                break;
            case ArraySize:
                return;
            case DataMember:

                //moonCodeGenerator.Code.AppendLine($"\t\tLOAD scopeSize: {currentScopeSize}");
                moonCodeGenerator.LoadDataMember(currentTable, currentTable.Lookup(LeftMostChild!.Token!.Lexeme) != null ? currentTable.Lookup(LeftMostChild!.Token!.Lexeme)! : chainedTable.Lookup(LeftMostChild!.Token!.Lexeme)!, ref isArray);

                // Check if the data member is a class, if so set the chained table to the class table
                if (currentTable.Lookup(LeftMostChild!.Token!.Lexeme)?.Link != null)
                    chainedTable = currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!;

                if (Parent!.Operation == DotChain)
                    currentScopeSize += currentTable.Lookup(LeftMostChild!.Token!.Lexeme) != null ? currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Offset : chainedTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Offset;

                break;
            case FuncCall:
                //moonCodeGenerator.Code.AppendLine("Function Call PRE");
                break;
            case AParamList:
                if (LeftMostChild is null)
                    return;
                moonCodeGenerator.Code.AppendLine($"\t\tsubi r14,r14,{currentScopeSize}");
                break;
            case StructDecl:
                return;
            case ImplDef:
                currentTable = currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!;
                break;
            case FuncDef:
                currentTable = currentTable.Lookup(LeftMostChild!.LeftMostChild!.Token!.Lexeme)!.Link!;
                moonCodeGenerator.FunctionDeclaration(currentTable);
                //WriteLine("Function Declaration: " + currentTable.Name + " Scope Size: " + currentTable.ScopeSize);
                break;
            case WhileStat:
                int whileCount = 0;
                moonCodeGenerator.WhileCond(currentTable, ref whileCount);
                LeftMostChild!.GenerateCode(currentTable, moonCodeGenerator, ref currentScopeSize, ref chainedTable, ref isArray);
                moonCodeGenerator.While(currentTable, ref whileCount);
                LeftMostChild!.RightSibling!.GenerateCode(currentTable, moonCodeGenerator, ref currentScopeSize, ref chainedTable, ref isArray);
                moonCodeGenerator.EndWhile(currentTable, ref whileCount);
                return;
            case IfStat:
                LeftMostChild!.GenerateCode(currentTable, moonCodeGenerator, ref currentScopeSize, ref chainedTable, ref isArray);
                int labelCount = 0;
                moonCodeGenerator.If(currentTable,ref labelCount);
                LeftMostChild!.RightSibling!.GenerateCode(currentTable, moonCodeGenerator, ref currentScopeSize, ref chainedTable, ref isArray);
                moonCodeGenerator.Else(currentTable,ref labelCount);
                LeftMostChild!.RightSibling!.RightSibling!.GenerateCode(currentTable, moonCodeGenerator, ref currentScopeSize, ref chainedTable, ref isArray);
                moonCodeGenerator.EndIf(currentTable,ref labelCount);
                return;

            default:
                break;
        }

        // Loop through the children of the current node in depth-first order
        IASTNode? child = LeftMostChild;
        while (child != null)
        {
            child.GenerateCode(currentTable, moonCodeGenerator, ref currentScopeSize, ref chainedTable, ref isArray);
            child = child.RightSibling;
        }

        // Perform the following actions when exiting a node based on its operation
        switch (Operation)
        {
            case DotChain:
                moonCodeGenerator.UnloadDataMember(currentTable, currentScopeSize);

                currentScopeSize = 0;
                chainedTable = currentTable;
                break;
            case DataMember:
                if (Parent!.Parent!.Operation != DotChain && Parent!.Operation == DotChain && RightSibling is null)
                    moonCodeGenerator.LoadVariableFromDataMember(chainedTable, chainedTable.Lookup(LeftMostChild!.Token!.Lexeme)!,ref isArray);
                else if (Parent!.Operation != DotChain)
                {
                    // Load the variable location into a register and unload the data member from the stack
                    moonCodeGenerator.LoadVariableFromDataMember(currentTable, currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!,ref isArray);
                    moonCodeGenerator.UnloadDataMember(currentTable, currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Offset);
                }

                moonCodeGenerator.LoadClassIndex(currentTable, currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!, ref isArray);

                break;
            case FuncCall:
                if (LeftMostChild!.Operation == FuncCall)
                    return;

                // If the function call is not part of a chain, generate the function call code
                if (LeftMostChild!.Operation == DotChain)
                    return;

                // Check if the function is a method or a free function
                if (Parent!.Operation == DotChain)
                    moonCodeGenerator.FunctionCall(currentTable, currentTable.Lookup(LeftMostChild!.Token!.Lexeme) == null ? chainedTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link! : currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!,ref isArray, currentScopeSize);
                else
                    moonCodeGenerator.FunctionCall(currentTable, currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Link!,ref isArray);
                break;
            case AParamList:
                moonCodeGenerator.Code.AppendLine($"\t\taddi r14,r14,{currentScopeSize}");
                break;
            case VarDecl:
                //WriteLine("Var Declaration: " +currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!.Name+" Scope Size: "+currentTable.ScopeSize);
                moonCodeGenerator.VarDeclaration(currentTable, currentTable.Lookup(LeftMostChild!.Token!.Lexeme)!);
                break;
            case ImplDef:
                break;
            case FuncDef:
                moonCodeGenerator.FunctionDeclarationEnd(currentTable);
                //WriteLine("Function Declaration End: " +currentTable.Name+" Scope Size: "+currentTable.ScopeSize);
                break;
            case IntLit:
                moonCodeGenerator.LoadIntegerValue(Token!.Lexeme);
                break;
            case FloatLit:
                moonCodeGenerator.LoadFloatValue(Token!.Lexeme);
                break;
            case AssignStat:
                moonCodeGenerator.AssignDataMember(currentTable, null, GetType(LeftMostChild!.RightSibling!, currentTable, currentTable, new List<ISemanticWarning>(), new List<ISemanticError>()));
                break;
            case WriteStat:
                moonCodeGenerator.Write(currentTable, GetType(LeftMostChild!, currentTable, currentTable, new List<ISemanticWarning>(), new List<ISemanticError>()));
                break;
            case ReturnStat:
                moonCodeGenerator.Return(currentTable, GetType(LeftMostChild!, currentTable, currentTable, new List<ISemanticWarning>(), new List<ISemanticError>()));
                break;
            case AddExpr:
                moonCodeGenerator.AddExpression(currentTable, LeftMostChild!.RightSibling!.Token!.Lexeme!, GetType(LeftMostChild!, currentTable, currentTable, new List<ISemanticWarning>(), new List<ISemanticError>()));
                break;
            case MultExpr:
                moonCodeGenerator.MultExpression(currentTable, LeftMostChild!.RightSibling!.Token!.Lexeme!, GetType(LeftMostChild!, currentTable, currentTable, new List<ISemanticWarning>(), new List<ISemanticError>()));
                break;
            case RelExpr:
                moonCodeGenerator.RelExpression(currentTable, LeftMostChild!.RightSibling!.Token!.Lexeme!, GetType(LeftMostChild!, currentTable, currentTable, new List<ISemanticWarning>(), new List<ISemanticError>()));
                break;
            case NotFactor:
                moonCodeGenerator.NotExpression(currentTable, GetType(LeftMostChild!, currentTable, currentTable, new List<ISemanticWarning>(), new List<ISemanticError>()));
                break;
            case SignFactor:
                moonCodeGenerator.NegExpression(currentTable, GetType(LeftMostChild!.RightSibling!, currentTable, currentTable, new List<ISemanticWarning>(), new List<ISemanticError>()));
                break;
            case ReadStat:
                moonCodeGenerator.Read(currentTable, GetType(LeftMostChild!.LeftMostChild!, currentTable, currentTable, new List<ISemanticWarning>(), new List<ISemanticError>()));
                break;



            default:
                break;
        }
    }

    #endregion Methods for Code Generation

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

    public bool IsEmpty()
    {
        return _stack.Count == 0;
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