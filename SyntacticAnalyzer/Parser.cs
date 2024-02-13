using LexicalAnalyzer;
using static System.Console;
using static LexicalAnalyzer.TokenType;
    
namespace SyntacticAnalyzer;

/// <summary>
/// Parser class, used to parse a source file.
/// </summary>
public class Parser : IParser
{
    private const bool DEBUG = false;
    private IScanner Scanner { get; }
    private Token LookAhead { get; set; }
    private string Source {get;set;} = "";
    private string SourceName {get;set;} = "";
    private IParseTree ParseTree { get; set; }
    private const string OUT_SYNTAX_ERRORS_EXTENSION = ".outsyntaxerrors";
    private const string OUT_DERIVATION_EXTENSION = ".outderivation";
    private const string OUT_PRODUCTIONS_EXTENSION = ".outproductions";

    #region Constructor

    /// <summary>
    /// Constructor for the Parser class
    /// </summary>
    /// <param name="sourceFileName">The name of the source file to parse</param>
    public Parser(string sourceFileName)
    {
        // Create a new scanner and get the first token
        Scanner = new Scanner(sourceFileName);
        LookAhead = new Token();

        // Set the source file 
        Source = sourceFileName;
        SourceName = sourceFileName.Replace(".src", "");

        // Delete the output files if they exist
        if (File.Exists(SourceName + OUT_SYNTAX_ERRORS_EXTENSION))
            File.Delete(SourceName + OUT_SYNTAX_ERRORS_EXTENSION);
        if (File.Exists(SourceName + OUT_DERIVATION_EXTENSION))
            File.Delete(SourceName + OUT_DERIVATION_EXTENSION);
        if (File.Exists(SourceName + OUT_PRODUCTIONS_EXTENSION))
            File.Delete(SourceName + OUT_PRODUCTIONS_EXTENSION);

        ParseTree = new ParseTree(new ParseNode("<START>",false), SourceName + OUT_DERIVATION_EXTENSION, DEBUG);

        if (DEBUG)
            WriteLine("Parser initialized...");
    }

    #endregion Constructor

    #region Base Methods

    /// <summary>
    /// Parses the source file, returning true if the source file is parsed successfully, false otherwise
    /// </summary>
    /// <returns>True if the source file is parsed successfully, false otherwise</returns>
    public bool Parse()
    {
        // Get the next token and make sure it is not a Comment
        do
            LookAhead = Scanner.NextToken();
        while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt);

        if (DEBUG)
            WriteLine("Parsing...");

        // Parse the source file   
        bool isParsed = Start() && Match(Eof);
        
        // Close the parse tree resources
        ParseTree.Close();
        
        return isParsed;
    }

    /// <summary>
    /// Matches the current token with the expected token
    /// </summary>
    /// <param name="tokenType">The expected token type</param>
    /// <returns>True if the current token matches the expected token, false otherwise</returns>
    private bool Match(TokenType tokenType)
    {
        if (tokenType == Eof)
            while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt)
                LookAhead = Scanner.NextToken();

        // Check if the current token matches the expected token
        bool isMatch = LookAhead.Type == tokenType;

        if (!isMatch)
            WriteLine($"Syntax error: Unexpected {LookAhead.Lexeme} at line {LookAhead.Location}. Expected {tokenType}.");

        // Get the next token and make sure it is not a Comment
        do
            LookAhead = Scanner.NextToken();
        while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt);

        // Return the result
        return isMatch;
    }

    /// <summary>
    /// Skips errors in the source file
    /// </summary>
    /// <param name="firstSet">The first set of the production rule</param>
    /// <param name="followSet">The follow set of the production rule</param>
    /// <returns>True if there are no errors (or if the error is recovered from), false otherwise</returns>
    private bool SkipErrors(TokenType[] firstSet, TokenType[] followSet)
    {
        // Check if the current token is in the first set or the follow set (if it is an epsilon token)
        if (firstSet.Contains(LookAhead.Type) || (firstSet.Contains(Epsilon) && followSet.Contains(LookAhead.Type)))
            return true;
        else
        {
            // Output an error message, this error can be recovered from
            WriteLine($"Syntax error: Unexpected {LookAhead.Lexeme} at line {LookAhead.Location}.");
            using StreamWriter sw = new(SourceName + OUT_SYNTAX_ERRORS_EXTENSION, true);
            sw.WriteLine($"Syntax error: Unexpected {LookAhead.Lexeme} at line {LookAhead.Location}.");

            // Get the next token until it is in the first set or the follow set
            while (!firstSet.Contains(LookAhead.Type) && !followSet.Contains(LookAhead.Type))
            {
                // Get the next token and make sure it is not a Comment
                do
                    LookAhead = Scanner.NextToken();
                while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt);

                // If the end of the file is reached, return false
                if(LookAhead.Type == Eof)
                    return false;

                // If the current token contains epsilon and the follow set contains the current token, return false
                if (firstSet.Contains(Epsilon) && followSet.Contains(LookAhead.Type))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Outputs a derivation to the console and to the output file
    /// </summary>
    /// <param name="productionRuleStr">The production rule to output</param>
    private void OutputDerivation(string productionRuleStr)
    {
        // Add the production rule to the parse tree and print the parse tree
        //ParseTree.AddProduction(productionRuleStr);
        //ParseTree.Print();

        // Write the production rule to the output file
        using StreamWriter sw = new(SourceName + OUT_PRODUCTIONS_EXTENSION, true);
        sw.WriteLine(productionRuleStr);
    }

    /// <summary>
    /// Outputs a production rule to the console and to the output file
    /// </summary>
    /// <param name="productionRuleStr">The production rule to output</param>
    /// <returns>True</returns>
    private static bool OutputProductionRule(string productionRuleStr)
    {
        if (DEBUG)
            WriteLine(productionRuleStr);
        return true;
    }

    /// <summary>
    /// Outputs an error message to the console and to the output file
    /// </summary>
    /// <returns>False</returns>
    private bool OutputError()
    {
        // Define the error message
        string errorMsg = $"Syntax error: Unexpected {LookAhead.Lexeme} at line {LookAhead.Location}.";

        // Write the error message to the console and to the output file
        WriteLine(errorMsg);
        using StreamWriter sw = new(SourceName + OUT_SYNTAX_ERRORS_EXTENSION, true);
        sw.WriteLine(errorMsg);
        return false;
    }

    #endregion Base Methods

    #region First Sets

    
    private static readonly TokenType[] FIRST_ArithmeticExpression = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FIRST_MemberDeclaration = new TokenType[] { TokenType.Func, TokenType.Let }; 
    private static readonly TokenType[] FIRST_FuntionParameters = new TokenType[] { TokenType.Id, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_StructOrImplOrFunction = new TokenType[] { TokenType.Struct, TokenType.Impl, TokenType.Func }; 
    private static readonly TokenType[] FIRST_Statement = new TokenType[] { TokenType.Id, TokenType.If, TokenType.While, TokenType.Read, TokenType.Write, TokenType.Return }; 
    private static readonly TokenType[] FIRST_RepetitiveStructMemberDeclarations = new TokenType[] { TokenType.Public, TokenType.Private, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_IdnestRest = new TokenType[] { TokenType.Openpar, TokenType.Opensqbr, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveArgumentParametersTail = new TokenType[] { TokenType.Comma, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_FunctionHeader = new TokenType[] { TokenType.Func }; 
    private static readonly TokenType[] FIRST_Expression = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FIRST_VariableIdnest = new TokenType[] { TokenType.Dot }; 
    private static readonly TokenType[] FIRST_RelationalOperator = new TokenType[] { TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq }; 
    private static readonly TokenType[] FIRST_FunctionDefinition = new TokenType[] { TokenType.Func }; 
    private static readonly TokenType[] FIRST_ArraySizeContent = new TokenType[] { TokenType.Intnum, TokenType.Closesqbr }; 
    private static readonly TokenType[] FIRST_VariableRest = new TokenType[] { TokenType.Opensqbr, TokenType.Epsilon, TokenType.Openpar }; 
    private static readonly TokenType[] FIRST_RepetitiveFunctionDefinitions = new TokenType[] { TokenType.Func, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveFunctionParametersTails = new TokenType[] { TokenType.Comma, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_StatementStart = new TokenType[] { TokenType.Openpar, TokenType.Opensqbr, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_FunctionBody = new TokenType[] { TokenType.Opencubr }; 
    private static readonly TokenType[] FIRST_ArraySize = new TokenType[] { TokenType.Opensqbr }; 
    private static readonly TokenType[] FIRST_Variable = new TokenType[] { TokenType.Id }; 
    private static readonly TokenType[] FIRST_MultOperator = new TokenType[] { TokenType.Mult, TokenType.Div, TokenType.And }; 
    private static readonly TokenType[] FIRST_AddOperator = new TokenType[] { TokenType.Plus, TokenType.Minus, TokenType.Or }; 
    private static readonly TokenType[] FIRST_RepetitiveStructOptionalInheritances = new TokenType[] { TokenType.Comma, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveVariableDeclarationOrStatements = new TokenType[] { TokenType.Let, TokenType.Id, TokenType.If, TokenType.While, TokenType.Read, TokenType.Write, TokenType.Return, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_VariableDeclaration = new TokenType[] { TokenType.Let }; 
    private static readonly TokenType[] FIRST_RepetitiveIndices = new TokenType[] { TokenType.Opensqbr, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_FactorOpt = new TokenType[] { TokenType.Openpar, TokenType.Opensqbr, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_Term = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FIRST_Visibility = new TokenType[] { TokenType.Public, TokenType.Private }; 
    private static readonly TokenType[] FIRST_RepetitiveArraySizes = new TokenType[] { TokenType.Opensqbr, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_FactorAlt = new TokenType[] { TokenType.Openpar, TokenType.Opensqbr, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_VariableDeclarationOrStatement = new TokenType[] { TokenType.Let, TokenType.Id, TokenType.If, TokenType.While, TokenType.Read, TokenType.Write, TokenType.Return }; 
    private static readonly TokenType[] FIRST_StructOptionalInheritance = new TokenType[] { TokenType.Inherits, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_ArgumentParameters = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_Type = new TokenType[] { TokenType.Integer, TokenType.Float, TokenType.Id }; 
    private static readonly TokenType[] FIRST_FunctionParametersTail = new TokenType[] { TokenType.Comma }; 
    private static readonly TokenType[] FIRST_RecursiveArithmeticExpression = new TokenType[] { TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_Start = new TokenType[] { TokenType.Struct, TokenType.Impl, TokenType.Func, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_Sign = new TokenType[] { TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FIRST_OptionalAssignment = new TokenType[] { TokenType.Assign, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveVariables = new TokenType[] { TokenType.Dot, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_ArgumentParametersTail = new TokenType[] { TokenType.Comma }; 
    private static readonly TokenType[] FIRST_RepetitiveVariableOrFunctionCall = new TokenType[] { TokenType.Dot, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_FunctionDeclaration = new TokenType[] { TokenType.Func }; 
    private static readonly TokenType[] FIRST_ImplDefinition = new TokenType[] { TokenType.Impl }; 
    private static readonly TokenType[] FIRST_RepetitiveStatements = new TokenType[] { TokenType.Id, TokenType.If, TokenType.While, TokenType.Read, TokenType.Write, TokenType.Return, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_VariableIdnestRest = new TokenType[] { TokenType.Openpar, TokenType.Opensqbr, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_RecursiveTerms = new TokenType[] { TokenType.Mult, TokenType.Div, TokenType.And, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_Factor = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FIRST_Indice = new TokenType[] { TokenType.Opensqbr }; 
    private static readonly TokenType[] FIRST_OptionalRelationalExpression = new TokenType[] { TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Epsilon }; 
    private static readonly TokenType[] FIRST_RelationalExpression = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FIRST_AssignmentOperator = new TokenType[] { TokenType.Assign }; 
    private static readonly TokenType[] FIRST_ReturnType = new TokenType[] { TokenType.Integer, TokenType.Float, TokenType.Id, TokenType.Void }; 
    private static readonly TokenType[] FIRST_StructDeclaration = new TokenType[] { TokenType.Struct }; 
    private static readonly TokenType[] FIRST_Idnest = new TokenType[] { TokenType.Dot }; 
    private static readonly TokenType[] FIRST_StatementBlock = new TokenType[] { TokenType.Opencubr, TokenType.Id, TokenType.If, TokenType.While, TokenType.Read, TokenType.Write, TokenType.Return, TokenType.Epsilon };

    #endregion First Sets

    #region Follow Sets
    
    private static readonly TokenType[] FOLLOW_ArithmeticExpression = new TokenType[] { TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Semi, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_MemberDeclaration = new TokenType[] { TokenType.Public, TokenType.Private, TokenType.Closecubr }; 
    private static readonly TokenType[] FOLLOW_FuntionParameters = new TokenType[] { TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_StructOrImplOrFunction = new TokenType[] { TokenType.Struct, TokenType.Impl, TokenType.Func }; 
    private static readonly TokenType[] FOLLOW_Statement = new TokenType[] { TokenType.Let, TokenType.Id, TokenType.If, TokenType.While, TokenType.Read, TokenType.Write, TokenType.Return, TokenType.Closecubr, TokenType.Else, TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_RepetitiveStructMemberDeclarations = new TokenType[] { TokenType.Closecubr }; 
    private static readonly TokenType[] FOLLOW_IdnestRest = new TokenType[] { TokenType.Dot, TokenType.Assign, TokenType.Semi, TokenType.Mult, TokenType.Div, TokenType.And, TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveArgumentParametersTail = new TokenType[] { TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_FunctionHeader = new TokenType[] { TokenType.Opencubr, TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_Expression = new TokenType[] { TokenType.Closepar, TokenType.Comma, TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_VariableIdnest = new TokenType[] { TokenType.Closepar, TokenType.Dot }; 
    private static readonly TokenType[] FOLLOW_RelationalOperator = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FOLLOW_FunctionDefinition = new TokenType[] { TokenType.Struct, TokenType.Impl, TokenType.Func, TokenType.Closecubr }; 
    private static readonly TokenType[] FOLLOW_ArraySizeContent = new TokenType[] { TokenType.Opensqbr, TokenType.Comma, TokenType.Closepar, TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_VariableRest = new TokenType[] { TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_RepetitiveFunctionDefinitions = new TokenType[] { TokenType.Closecubr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveFunctionParametersTails = new TokenType[] { TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_StatementStart = new TokenType[] { TokenType.Assign, TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_FunctionBody = new TokenType[] { TokenType.Struct, TokenType.Impl, TokenType.Func, TokenType.Closecubr }; 
    private static readonly TokenType[] FOLLOW_ArraySize = new TokenType[] { TokenType.Opensqbr, TokenType.Comma, TokenType.Closepar, TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_Variable = new TokenType[] { TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_MultOperator = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FOLLOW_AddOperator = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FOLLOW_RepetitiveStructOptionalInheritances = new TokenType[] { TokenType.Opencubr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveVariableDeclarationOrStatements = new TokenType[] { TokenType.Closecubr }; 
    private static readonly TokenType[] FOLLOW_VariableDeclaration = new TokenType[] { TokenType.Public, TokenType.Private, TokenType.Closecubr, TokenType.Let, TokenType.Id, TokenType.If, TokenType.While, TokenType.Read, TokenType.Write, TokenType.Return }; 
    private static readonly TokenType[] FOLLOW_RepetitiveIndices = new TokenType[] { TokenType.Dot, TokenType.Assign, TokenType.Semi, TokenType.Mult, TokenType.Div, TokenType.And, TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_FactorOpt = new TokenType[] { TokenType.Mult, TokenType.Div, TokenType.And, TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Semi, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_Term = new TokenType[] { TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Semi, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_Visibility = new TokenType[] { TokenType.Func, TokenType.Let }; 
    private static readonly TokenType[] FOLLOW_RepetitiveArraySizes = new TokenType[] { TokenType.Comma, TokenType.Closepar, TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_FactorAlt = new TokenType[] { TokenType.Dot, TokenType.Assign, TokenType.Semi, TokenType.Mult, TokenType.Div, TokenType.And, TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_VariableDeclarationOrStatement = new TokenType[] { TokenType.Let, TokenType.Id, TokenType.If, TokenType.While, TokenType.Read, TokenType.Write, TokenType.Return, TokenType.Closecubr }; 
    private static readonly TokenType[] FOLLOW_StructOptionalInheritance = new TokenType[] { TokenType.Opencubr }; 
    private static readonly TokenType[] FOLLOW_ArgumentParameters = new TokenType[] { TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_Type = new TokenType[] { TokenType.Opensqbr, TokenType.Comma, TokenType.Closepar, TokenType.Semi, TokenType.Opencubr }; 
    private static readonly TokenType[] FOLLOW_FunctionParametersTail = new TokenType[] { TokenType.Comma, TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_RecursiveArithmeticExpression = new TokenType[] { TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Semi, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_Start = new TokenType[] { TokenType.Eof }; 
    private static readonly TokenType[] FOLLOW_Sign = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FOLLOW_OptionalAssignment = new TokenType[] { TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_RepetitiveVariables = new TokenType[] { TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_ArgumentParametersTail = new TokenType[] { TokenType.Comma, TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_RepetitiveVariableOrFunctionCall = new TokenType[] { TokenType.Assign, TokenType.Semi, TokenType.Mult, TokenType.Div, TokenType.And, TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_FunctionDeclaration = new TokenType[] { TokenType.Public, TokenType.Private, TokenType.Closecubr }; 
    private static readonly TokenType[] FOLLOW_ImplDefinition = new TokenType[] { TokenType.Struct, TokenType.Impl, TokenType.Func }; 
    private static readonly TokenType[] FOLLOW_RepetitiveStatements = new TokenType[] { TokenType.Closecubr }; 
    private static readonly TokenType[] FOLLOW_VariableIdnestRest = new TokenType[] { TokenType.Closepar, TokenType.Dot }; 
    private static readonly TokenType[] FOLLOW_RecursiveTerms = new TokenType[] { TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Semi, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_Factor = new TokenType[] { TokenType.Mult, TokenType.Div, TokenType.And, TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Semi, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_Indice = new TokenType[] { TokenType.Opensqbr, TokenType.Dot, TokenType.Assign, TokenType.Semi, TokenType.Mult, TokenType.Div, TokenType.And, TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_OptionalRelationalExpression = new TokenType[] { TokenType.Closepar, TokenType.Comma, TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_RelationalExpression = new TokenType[] { TokenType.Closepar }; 
    private static readonly TokenType[] FOLLOW_AssignmentOperator = new TokenType[] { TokenType.Id, TokenType.Intnum, TokenType.Floatnum, TokenType.Openpar, TokenType.Not, TokenType.Plus, TokenType.Minus }; 
    private static readonly TokenType[] FOLLOW_ReturnType = new TokenType[] { TokenType.Opencubr, TokenType.Semi }; 
    private static readonly TokenType[] FOLLOW_StructDeclaration = new TokenType[] { TokenType.Struct, TokenType.Impl, TokenType.Func }; 
    private static readonly TokenType[] FOLLOW_Idnest = new TokenType[] { TokenType.Dot, TokenType.Assign, TokenType.Semi, TokenType.Mult, TokenType.Div, TokenType.And, TokenType.Plus, TokenType.Minus, TokenType.Or, TokenType.Eq, TokenType.Noteq, TokenType.Lt, TokenType.Gt, TokenType.Leq, TokenType.Geq, TokenType.Closepar, TokenType.Comma, TokenType.Closesqbr }; 
    private static readonly TokenType[] FOLLOW_StatementBlock = new TokenType[] { TokenType.Else, TokenType.Semi };

    #endregion Follow Sets

    #region Productions

    

    /// <summary>
    /// ArithmeticExpression production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArithmeticExpression() 
    {
        if(!SkipErrors(FIRST_ArithmeticExpression, FOLLOW_ArithmeticExpression))
            return false;
        if (FIRST_Term.Contains(LookAhead.Type))
        {
            OutputDerivation("<arithmeticExpression> -> <term> <recursiveArithmeticExpression>");
            if(Term() && RecursiveArithmeticExpression())
                return OutputProductionRule("<arithmeticExpression> -> <term> <recursiveArithmeticExpression>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// MemberDeclaration production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool MemberDeclaration() 
    {
        if(!SkipErrors(FIRST_MemberDeclaration, FOLLOW_MemberDeclaration))
            return false;
        if (FIRST_FunctionDeclaration.Contains(LookAhead.Type))
        {
            OutputDerivation("<memberDeclaration> -> <functionDeclaration>");
            if(FunctionDeclaration())
                return OutputProductionRule("<memberDeclaration> -> <functionDeclaration>");
            else
                return false;
        }
        else if (FIRST_VariableDeclaration.Contains(LookAhead.Type))
        {
            OutputDerivation("<memberDeclaration> -> <variableDeclaration>");
            if(VariableDeclaration())
                return OutputProductionRule("<memberDeclaration> -> <variableDeclaration>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// FuntionParameters production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FuntionParameters() 
    {
        if(!SkipErrors(FIRST_FuntionParameters, FOLLOW_FuntionParameters))
            return false;
        if (TokenType.Id == LookAhead.Type)
        {
            OutputDerivation("<funtionParameters> -> 'id' ':' <type> <repetitiveArraySizes> <repetitiveFunctionParametersTails>");
            if(Match(TokenType.Id) && Match(TokenType.Colon) && Type() && RepetitiveArraySizes() && RepetitiveFunctionParametersTails())
                return OutputProductionRule("<funtionParameters> -> 'id' ':' <type> <repetitiveArraySizes> <repetitiveFunctionParametersTails>");
            else
                return false;
        }
        else if (FOLLOW_FuntionParameters.Contains(LookAhead.Type))
        {
            OutputDerivation("<funtionParameters> -> EPSILON");
            return OutputProductionRule("<funtionParameters> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// StructOrImplOrFunction production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StructOrImplOrFunction() 
    {
        if(!SkipErrors(FIRST_StructOrImplOrFunction, FOLLOW_StructOrImplOrFunction))
            return false;
        if (FIRST_StructDeclaration.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOrImplOrFunction> -> <structDeclaration>");
            if(StructDeclaration())
                return OutputProductionRule("<structOrImplOrFunction> -> <structDeclaration>");
            else
                return false;
        }
        else if (FIRST_ImplDefinition.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOrImplOrFunction> -> <implDefinition>");
            if(ImplDefinition())
                return OutputProductionRule("<structOrImplOrFunction> -> <implDefinition>");
            else
                return false;
        }
        else if (FIRST_FunctionDefinition.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOrImplOrFunction> -> <functionDefinition>");
            if(FunctionDefinition())
                return OutputProductionRule("<structOrImplOrFunction> -> <functionDefinition>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Statement production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Statement() 
    {
        if(!SkipErrors(FIRST_Statement, FOLLOW_Statement))
            return false;
        if (TokenType.Id == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'id' <statementStart> <optionalAssignment> ';'");
            if(Match(TokenType.Id) && StatementStart() && OptionalAssignment() && Match(TokenType.Semi))
                return OutputProductionRule("<statement> -> 'id' <statementStart> <optionalAssignment> ';'");
            else
                return false;
        }
        else if (TokenType.If == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'if' '(' <relationalExpression> ')' 'then' <statementBlock> 'else' <statementBlock> ';'");
            if(Match(TokenType.If) && Match(TokenType.Openpar) && RelationalExpression() && Match(TokenType.Closepar) && Match(TokenType.Then) && StatementBlock() && Match(TokenType.Else) && StatementBlock() && Match(TokenType.Semi))
                return OutputProductionRule("<statement> -> 'if' '(' <relationalExpression> ')' 'then' <statementBlock> 'else' <statementBlock> ';'");
            else
                return false;
        }
        else if (TokenType.While == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'while' '(' <relationalExpression> ')' <statementBlock> ';'");
            if(Match(TokenType.While) && Match(TokenType.Openpar) && RelationalExpression() && Match(TokenType.Closepar) && StatementBlock() && Match(TokenType.Semi))
                return OutputProductionRule("<statement> -> 'while' '(' <relationalExpression> ')' <statementBlock> ';'");
            else
                return false;
        }
        else if (TokenType.Read == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'read' '(' <variable> ')' ';'");
            if(Match(TokenType.Read) && Match(TokenType.Openpar) && Variable() && Match(TokenType.Closepar) && Match(TokenType.Semi))
                return OutputProductionRule("<statement> -> 'read' '(' <variable> ')' ';'");
            else
                return false;
        }
        else if (TokenType.Write == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'write' '(' <expression> ')' ';'");
            if(Match(TokenType.Write) && Match(TokenType.Openpar) && Expression() && Match(TokenType.Closepar) && Match(TokenType.Semi))
                return OutputProductionRule("<statement> -> 'write' '(' <expression> ')' ';'");
            else
                return false;
        }
        else if (TokenType.Return == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'return' '(' <expression> ')' ';'");
            if(Match(TokenType.Return) && Match(TokenType.Openpar) && Expression() && Match(TokenType.Closepar) && Match(TokenType.Semi))
                return OutputProductionRule("<statement> -> 'return' '(' <expression> ')' ';'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveStructMemberDeclarations production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveStructMemberDeclarations() 
    {
        if(!SkipErrors(FIRST_RepetitiveStructMemberDeclarations, FOLLOW_RepetitiveStructMemberDeclarations))
            return false;
        if (FIRST_Visibility.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveStructMemberDeclarations> -> <visibility> <memberDeclaration> <repetitiveStructMemberDeclarations>");
            if(Visibility() && MemberDeclaration() && RepetitiveStructMemberDeclarations())
                return OutputProductionRule("<repetitiveStructMemberDeclarations> -> <visibility> <memberDeclaration> <repetitiveStructMemberDeclarations>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveStructMemberDeclarations.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveStructMemberDeclarations> -> EPSILON");
            return OutputProductionRule("<repetitiveStructMemberDeclarations> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// IdnestRest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool IdnestRest() 
    {
        if(!SkipErrors(FIRST_IdnestRest, FOLLOW_IdnestRest))
            return false;
        if (TokenType.Openpar == LookAhead.Type)
        {
            OutputDerivation("<idnestRest> -> '(' <argumentParameters> ')'");
            if(Match(TokenType.Openpar) && ArgumentParameters() && Match(TokenType.Closepar))
                return OutputProductionRule("<idnestRest> -> '(' <argumentParameters> ')'");
            else
                return false;
        }
        else if (FIRST_RepetitiveIndices.Contains(LookAhead.Type))
        {
            OutputDerivation("<idnestRest> -> <repetitiveIndices>");
            if(RepetitiveIndices())
                return OutputProductionRule("<idnestRest> -> <repetitiveIndices>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveArgumentParametersTail production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveArgumentParametersTail() 
    {
        if(!SkipErrors(FIRST_RepetitiveArgumentParametersTail, FOLLOW_RepetitiveArgumentParametersTail))
            return false;
        if (FIRST_ArgumentParametersTail.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveArgumentParametersTail> -> <argumentParametersTail> <repetitiveArgumentParametersTail>");
            if(ArgumentParametersTail() && RepetitiveArgumentParametersTail())
                return OutputProductionRule("<repetitiveArgumentParametersTail> -> <argumentParametersTail> <repetitiveArgumentParametersTail>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveArgumentParametersTail.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveArgumentParametersTail> -> EPSILON");
            return OutputProductionRule("<repetitiveArgumentParametersTail> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// FunctionHeader production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FunctionHeader() 
    {
        if(!SkipErrors(FIRST_FunctionHeader, FOLLOW_FunctionHeader))
            return false;
        if (TokenType.Func == LookAhead.Type)
        {
            OutputDerivation("<functionHeader> -> 'func' 'id' '(' <funtionParameters> ')' '->' <returnType>");
            if(Match(TokenType.Func) && Match(TokenType.Id) && Match(TokenType.Openpar) && FuntionParameters() && Match(TokenType.Closepar) && Match(TokenType.Arrow) && ReturnType())
                return OutputProductionRule("<functionHeader> -> 'func' 'id' '(' <funtionParameters> ')' '->' <returnType>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Expression production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Expression() 
    {
        if(!SkipErrors(FIRST_Expression, FOLLOW_Expression))
            return false;
        if (FIRST_ArithmeticExpression.Contains(LookAhead.Type))
        {
            OutputDerivation("<expression> -> <arithmeticExpression> <optionalRelationalExpression>");
            if(ArithmeticExpression() && OptionalRelationalExpression())
                return OutputProductionRule("<expression> -> <arithmeticExpression> <optionalRelationalExpression>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// VariableIdnest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VariableIdnest() 
    {
        if(!SkipErrors(FIRST_VariableIdnest, FOLLOW_VariableIdnest))
            return false;
        if (TokenType.Dot == LookAhead.Type)
        {
            OutputDerivation("<variableIdnest> -> '.' 'id' <variableIdnestRest>");
            if(Match(TokenType.Dot) && Match(TokenType.Id) && VariableIdnestRest())
                return OutputProductionRule("<variableIdnest> -> '.' 'id' <variableIdnestRest>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RelationalOperator production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RelationalOperator() 
    {
        if(!SkipErrors(FIRST_RelationalOperator, FOLLOW_RelationalOperator))
            return false;
        if (TokenType.Eq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'eq'");
            if(Match(TokenType.Eq))
                return OutputProductionRule("<relationalOperator> -> 'eq'");
            else
                return false;
        }
        else if (TokenType.Noteq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'neq'");
            if(Match(TokenType.Noteq))
                return OutputProductionRule("<relationalOperator> -> 'neq'");
            else
                return false;
        }
        else if (TokenType.Lt == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'lt'");
            if(Match(TokenType.Lt))
                return OutputProductionRule("<relationalOperator> -> 'lt'");
            else
                return false;
        }
        else if (TokenType.Gt == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'gt'");
            if(Match(TokenType.Gt))
                return OutputProductionRule("<relationalOperator> -> 'gt'");
            else
                return false;
        }
        else if (TokenType.Leq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'leq'");
            if(Match(TokenType.Leq))
                return OutputProductionRule("<relationalOperator> -> 'leq'");
            else
                return false;
        }
        else if (TokenType.Geq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'geq'");
            if(Match(TokenType.Geq))
                return OutputProductionRule("<relationalOperator> -> 'geq'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// FunctionDefinition production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FunctionDefinition() 
    {
        if(!SkipErrors(FIRST_FunctionDefinition, FOLLOW_FunctionDefinition))
            return false;
        if (FIRST_FunctionHeader.Contains(LookAhead.Type))
        {
            OutputDerivation("<functionDefinition> -> <functionHeader> <functionBody>");
            if(FunctionHeader() && FunctionBody())
                return OutputProductionRule("<functionDefinition> -> <functionHeader> <functionBody>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// ArraySizeContent production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArraySizeContent() 
    {
        if(!SkipErrors(FIRST_ArraySizeContent, FOLLOW_ArraySizeContent))
            return false;
        if (TokenType.Intnum == LookAhead.Type)
        {
            OutputDerivation("<arraySizeContent> -> 'intNum' ']'");
            if(Match(TokenType.Intnum) && Match(TokenType.Closesqbr))
                return OutputProductionRule("<arraySizeContent> -> 'intNum' ']'");
            else
                return false;
        }
        else if (TokenType.Closesqbr == LookAhead.Type)
        {
            OutputDerivation("<arraySizeContent> -> ']'");
            if(Match(TokenType.Closesqbr))
                return OutputProductionRule("<arraySizeContent> -> ']'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// VariableRest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VariableRest() 
    {
        if(!SkipErrors(FIRST_VariableRest, FOLLOW_VariableRest))
            return false;
        if (FIRST_RepetitiveIndices.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableRest> -> <repetitiveIndices> <repetitiveVariables>");
            if(RepetitiveIndices() && RepetitiveVariables())
                return OutputProductionRule("<variableRest> -> <repetitiveIndices> <repetitiveVariables>");
            else
                return false;
        }
        else if (TokenType.Openpar == LookAhead.Type)
        {
            OutputDerivation("<variableRest> -> '(' <argumentParameters> ')' <variableIdnest>");
            if(Match(TokenType.Openpar) && ArgumentParameters() && Match(TokenType.Closepar) && VariableIdnest())
                return OutputProductionRule("<variableRest> -> '(' <argumentParameters> ')' <variableIdnest>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveFunctionDefinitions production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveFunctionDefinitions() 
    {
        if(!SkipErrors(FIRST_RepetitiveFunctionDefinitions, FOLLOW_RepetitiveFunctionDefinitions))
            return false;
        if (FIRST_FunctionDefinition.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveFunctionDefinitions> -> <functionDefinition> <repetitiveFunctionDefinitions>");
            if(FunctionDefinition() && RepetitiveFunctionDefinitions())
                return OutputProductionRule("<repetitiveFunctionDefinitions> -> <functionDefinition> <repetitiveFunctionDefinitions>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveFunctionDefinitions.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveFunctionDefinitions> -> EPSILON");
            return OutputProductionRule("<repetitiveFunctionDefinitions> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveFunctionParametersTails production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveFunctionParametersTails() 
    {
        if(!SkipErrors(FIRST_RepetitiveFunctionParametersTails, FOLLOW_RepetitiveFunctionParametersTails))
            return false;
        if (FIRST_FunctionParametersTail.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveFunctionParametersTails> -> <functionParametersTail> <repetitiveFunctionParametersTails>");
            if(FunctionParametersTail() && RepetitiveFunctionParametersTails())
                return OutputProductionRule("<repetitiveFunctionParametersTails> -> <functionParametersTail> <repetitiveFunctionParametersTails>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveFunctionParametersTails.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveFunctionParametersTails> -> EPSILON");
            return OutputProductionRule("<repetitiveFunctionParametersTails> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// StatementStart production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StatementStart() 
    {
        if(!SkipErrors(FIRST_StatementStart, FOLLOW_StatementStart))
            return false;
        if (FIRST_FactorAlt.Contains(LookAhead.Type))
        {
            OutputDerivation("<statementStart> -> <factorAlt> <repetitiveVariableOrFunctionCall>");
            if(FactorAlt() && RepetitiveVariableOrFunctionCall())
                return OutputProductionRule("<statementStart> -> <factorAlt> <repetitiveVariableOrFunctionCall>");
            else
                return false;
        }
        else if (FOLLOW_StatementStart.Contains(LookAhead.Type))
        {
            OutputDerivation("<statementStart> -> EPSILON");
            return OutputProductionRule("<statementStart> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// FunctionBody production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FunctionBody() 
    {
        if(!SkipErrors(FIRST_FunctionBody, FOLLOW_FunctionBody))
            return false;
        if (TokenType.Opencubr == LookAhead.Type)
        {
            OutputDerivation("<functionBody> -> '{' <repetitiveVariableDeclarationOrStatements> '}'");
            if(Match(TokenType.Opencubr) && RepetitiveVariableDeclarationOrStatements() && Match(TokenType.Closecubr))
                return OutputProductionRule("<functionBody> -> '{' <repetitiveVariableDeclarationOrStatements> '}'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// ArraySize production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArraySize() 
    {
        if(!SkipErrors(FIRST_ArraySize, FOLLOW_ArraySize))
            return false;
        if (TokenType.Opensqbr == LookAhead.Type)
        {
            OutputDerivation("<arraySize> -> '[' <arraySizeContent>");
            if(Match(TokenType.Opensqbr) && ArraySizeContent())
                return OutputProductionRule("<arraySize> -> '[' <arraySizeContent>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Variable production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Variable() 
    {
        if(!SkipErrors(FIRST_Variable, FOLLOW_Variable))
            return false;
        if (TokenType.Id == LookAhead.Type)
        {
            OutputDerivation("<variable> -> 'id' <variableRest>");
            if(Match(TokenType.Id) && VariableRest())
                return OutputProductionRule("<variable> -> 'id' <variableRest>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// MultOperator production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool MultOperator() 
    {
        if(!SkipErrors(FIRST_MultOperator, FOLLOW_MultOperator))
            return false;
        if (TokenType.Mult == LookAhead.Type)
        {
            OutputDerivation("<multOperator> -> '*'");
            if(Match(TokenType.Mult))
                return OutputProductionRule("<multOperator> -> '*'");
            else
                return false;
        }
        else if (TokenType.Div == LookAhead.Type)
        {
            OutputDerivation("<multOperator> -> '/'");
            if(Match(TokenType.Div))
                return OutputProductionRule("<multOperator> -> '/'");
            else
                return false;
        }
        else if (TokenType.And == LookAhead.Type)
        {
            OutputDerivation("<multOperator> -> 'and'");
            if(Match(TokenType.And))
                return OutputProductionRule("<multOperator> -> 'and'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// AddOperator production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool AddOperator() 
    {
        if(!SkipErrors(FIRST_AddOperator, FOLLOW_AddOperator))
            return false;
        if (TokenType.Plus == LookAhead.Type)
        {
            OutputDerivation("<addOperator> -> '+'");
            if(Match(TokenType.Plus))
                return OutputProductionRule("<addOperator> -> '+'");
            else
                return false;
        }
        else if (TokenType.Minus == LookAhead.Type)
        {
            OutputDerivation("<addOperator> -> '-'");
            if(Match(TokenType.Minus))
                return OutputProductionRule("<addOperator> -> '-'");
            else
                return false;
        }
        else if (TokenType.Or == LookAhead.Type)
        {
            OutputDerivation("<addOperator> -> 'or'");
            if(Match(TokenType.Or))
                return OutputProductionRule("<addOperator> -> 'or'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveStructOptionalInheritances production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveStructOptionalInheritances() 
    {
        if(!SkipErrors(FIRST_RepetitiveStructOptionalInheritances, FOLLOW_RepetitiveStructOptionalInheritances))
            return false;
        if (TokenType.Comma == LookAhead.Type)
        {
            OutputDerivation("<repetitiveStructOptionalInheritances> -> ',' 'id' <repetitiveStructOptionalInheritances>");
            if(Match(TokenType.Comma) && Match(TokenType.Id) && RepetitiveStructOptionalInheritances())
                return OutputProductionRule("<repetitiveStructOptionalInheritances> -> ',' 'id' <repetitiveStructOptionalInheritances>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveStructOptionalInheritances.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveStructOptionalInheritances> -> EPSILON");
            return OutputProductionRule("<repetitiveStructOptionalInheritances> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveVariableDeclarationOrStatements production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveVariableDeclarationOrStatements() 
    {
        if(!SkipErrors(FIRST_RepetitiveVariableDeclarationOrStatements, FOLLOW_RepetitiveVariableDeclarationOrStatements))
            return false;
        if (FIRST_VariableDeclarationOrStatement.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableDeclarationOrStatements> -> <variableDeclarationOrStatement> <repetitiveVariableDeclarationOrStatements>");
            if(VariableDeclarationOrStatement() && RepetitiveVariableDeclarationOrStatements())
                return OutputProductionRule("<repetitiveVariableDeclarationOrStatements> -> <variableDeclarationOrStatement> <repetitiveVariableDeclarationOrStatements>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveVariableDeclarationOrStatements.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableDeclarationOrStatements> -> EPSILON");
            return OutputProductionRule("<repetitiveVariableDeclarationOrStatements> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// VariableDeclaration production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VariableDeclaration() 
    {
        if(!SkipErrors(FIRST_VariableDeclaration, FOLLOW_VariableDeclaration))
            return false;
        if (TokenType.Let == LookAhead.Type)
        {
            OutputDerivation("<variableDeclaration> -> 'let' 'id' ':' <type> <repetitiveArraySizes> ';'");
            if(Match(TokenType.Let) && Match(TokenType.Id) && Match(TokenType.Colon) && Type() && RepetitiveArraySizes() && Match(TokenType.Semi))
                return OutputProductionRule("<variableDeclaration> -> 'let' 'id' ':' <type> <repetitiveArraySizes> ';'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveIndices production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveIndices() 
    {
        if(!SkipErrors(FIRST_RepetitiveIndices, FOLLOW_RepetitiveIndices))
            return false;
        if (FIRST_Indice.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveIndices> -> <indice> <repetitiveIndices>");
            if(Indice() && RepetitiveIndices())
                return OutputProductionRule("<repetitiveIndices> -> <indice> <repetitiveIndices>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveIndices.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveIndices> -> EPSILON");
            return OutputProductionRule("<repetitiveIndices> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// FactorOpt production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FactorOpt() 
    {
        if(!SkipErrors(FIRST_FactorOpt, FOLLOW_FactorOpt))
            return false;
        if (FIRST_FactorAlt.Contains(LookAhead.Type))
        {
            OutputDerivation("<factorOpt> -> <factorAlt> <repetitiveVariableOrFunctionCall>");
            if(FactorAlt() && RepetitiveVariableOrFunctionCall())
                return OutputProductionRule("<factorOpt> -> <factorAlt> <repetitiveVariableOrFunctionCall>");
            else
                return false;
        }
        else if (FOLLOW_FactorOpt.Contains(LookAhead.Type))
        {
            OutputDerivation("<factorOpt> -> EPSILON");
            return OutputProductionRule("<factorOpt> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Term production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Term() 
    {
        if(!SkipErrors(FIRST_Term, FOLLOW_Term))
            return false;
        if (FIRST_Factor.Contains(LookAhead.Type))
        {
            OutputDerivation("<term> -> <factor> <recursiveTerms>");
            if(Factor() && RecursiveTerms())
                return OutputProductionRule("<term> -> <factor> <recursiveTerms>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Visibility production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Visibility() 
    {
        if(!SkipErrors(FIRST_Visibility, FOLLOW_Visibility))
            return false;
        if (TokenType.Public == LookAhead.Type)
        {
            OutputDerivation("<visibility> -> 'public'");
            if(Match(TokenType.Public))
                return OutputProductionRule("<visibility> -> 'public'");
            else
                return false;
        }
        else if (TokenType.Private == LookAhead.Type)
        {
            OutputDerivation("<visibility> -> 'private'");
            if(Match(TokenType.Private))
                return OutputProductionRule("<visibility> -> 'private'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveArraySizes production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveArraySizes() 
    {
        if(!SkipErrors(FIRST_RepetitiveArraySizes, FOLLOW_RepetitiveArraySizes))
            return false;
        if (FIRST_ArraySize.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveArraySizes> -> <arraySize> <repetitiveArraySizes>");
            if(ArraySize() && RepetitiveArraySizes())
                return OutputProductionRule("<repetitiveArraySizes> -> <arraySize> <repetitiveArraySizes>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveArraySizes.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveArraySizes> -> EPSILON");
            return OutputProductionRule("<repetitiveArraySizes> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// FactorAlt production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FactorAlt() 
    {
        if(!SkipErrors(FIRST_FactorAlt, FOLLOW_FactorAlt))
            return false;
        if (TokenType.Openpar == LookAhead.Type)
        {
            OutputDerivation("<factorAlt> -> '(' <argumentParameters> ')'");
            if(Match(TokenType.Openpar) && ArgumentParameters() && Match(TokenType.Closepar))
                return OutputProductionRule("<factorAlt> -> '(' <argumentParameters> ')'");
            else
                return false;
        }
        else if (FIRST_RepetitiveIndices.Contains(LookAhead.Type))
        {
            OutputDerivation("<factorAlt> -> <repetitiveIndices>");
            if(RepetitiveIndices())
                return OutputProductionRule("<factorAlt> -> <repetitiveIndices>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// VariableDeclarationOrStatement production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VariableDeclarationOrStatement() 
    {
        if(!SkipErrors(FIRST_VariableDeclarationOrStatement, FOLLOW_VariableDeclarationOrStatement))
            return false;
        if (FIRST_VariableDeclaration.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableDeclarationOrStatement> -> <variableDeclaration>");
            if(VariableDeclaration())
                return OutputProductionRule("<variableDeclarationOrStatement> -> <variableDeclaration>");
            else
                return false;
        }
        else if (FIRST_Statement.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableDeclarationOrStatement> -> <statement>");
            if(Statement())
                return OutputProductionRule("<variableDeclarationOrStatement> -> <statement>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// StructOptionalInheritance production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StructOptionalInheritance() 
    {
        if(!SkipErrors(FIRST_StructOptionalInheritance, FOLLOW_StructOptionalInheritance))
            return false;
        if (TokenType.Inherits == LookAhead.Type)
        {
            OutputDerivation("<structOptionalInheritance> -> 'inherits' 'id' <repetitiveStructOptionalInheritances>");
            if(Match(TokenType.Inherits) && Match(TokenType.Id) && RepetitiveStructOptionalInheritances())
                return OutputProductionRule("<structOptionalInheritance> -> 'inherits' 'id' <repetitiveStructOptionalInheritances>");
            else
                return false;
        }
        else if (FOLLOW_StructOptionalInheritance.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOptionalInheritance> -> EPSILON");
            return OutputProductionRule("<structOptionalInheritance> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// ArgumentParameters production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArgumentParameters() 
    {
        if(!SkipErrors(FIRST_ArgumentParameters, FOLLOW_ArgumentParameters))
            return false;
        if (FIRST_Expression.Contains(LookAhead.Type))
        {
            OutputDerivation("<argumentParameters> -> <expression> <repetitiveArgumentParametersTail>");
            if(Expression() && RepetitiveArgumentParametersTail())
                return OutputProductionRule("<argumentParameters> -> <expression> <repetitiveArgumentParametersTail>");
            else
                return false;
        }
        else if (FOLLOW_ArgumentParameters.Contains(LookAhead.Type))
        {
            OutputDerivation("<argumentParameters> -> EPSILON");
            return OutputProductionRule("<argumentParameters> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Type production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Type() 
    {
        if(!SkipErrors(FIRST_Type, FOLLOW_Type))
            return false;
        if (TokenType.Integer == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'integer'");
            if(Match(TokenType.Integer))
                return OutputProductionRule("<type> -> 'integer'");
            else
                return false;
        }
        else if (TokenType.Float == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'float'");
            if(Match(TokenType.Float))
                return OutputProductionRule("<type> -> 'float'");
            else
                return false;
        }
        else if (TokenType.Id == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'id'");
            if(Match(TokenType.Id))
                return OutputProductionRule("<type> -> 'id'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// FunctionParametersTail production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FunctionParametersTail() 
    {
        if(!SkipErrors(FIRST_FunctionParametersTail, FOLLOW_FunctionParametersTail))
            return false;
        if (TokenType.Comma == LookAhead.Type)
        {
            OutputDerivation("<functionParametersTail> -> ',' 'id' ':' <type> <repetitiveArraySizes>");
            if(Match(TokenType.Comma) && Match(TokenType.Id) && Match(TokenType.Colon) && Type() && RepetitiveArraySizes())
                return OutputProductionRule("<functionParametersTail> -> ',' 'id' ':' <type> <repetitiveArraySizes>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RecursiveArithmeticExpression production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RecursiveArithmeticExpression() 
    {
        if(!SkipErrors(FIRST_RecursiveArithmeticExpression, FOLLOW_RecursiveArithmeticExpression))
            return false;
        if (FIRST_AddOperator.Contains(LookAhead.Type))
        {
            OutputDerivation("<recursiveArithmeticExpression> -> <addOperator> <term> <recursiveArithmeticExpression>");
            if(AddOperator() && Term() && RecursiveArithmeticExpression())
                return OutputProductionRule("<recursiveArithmeticExpression> -> <addOperator> <term> <recursiveArithmeticExpression>");
            else
                return false;
        }
        else if (FOLLOW_RecursiveArithmeticExpression.Contains(LookAhead.Type))
        {
            OutputDerivation("<recursiveArithmeticExpression> -> EPSILON");
            return OutputProductionRule("<recursiveArithmeticExpression> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Start production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Start() 
    {
        if(!SkipErrors(FIRST_Start, FOLLOW_Start))
            return false;
        if (FIRST_StructOrImplOrFunction.Contains(LookAhead.Type))
        {
            OutputDerivation("<START> -> <structOrImplOrFunction> <START>");
            if(StructOrImplOrFunction() && Start())
                return OutputProductionRule("<START> -> <structOrImplOrFunction> <START>");
            else
                return false;
        }
        else if (FOLLOW_Start.Contains(LookAhead.Type))
        {
            OutputDerivation("<START> -> EPSILON");
            return OutputProductionRule("<START> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Sign production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Sign() 
    {
        if(!SkipErrors(FIRST_Sign, FOLLOW_Sign))
            return false;
        if (TokenType.Plus == LookAhead.Type)
        {
            OutputDerivation("<sign> -> '+'");
            if(Match(TokenType.Plus))
                return OutputProductionRule("<sign> -> '+'");
            else
                return false;
        }
        else if (TokenType.Minus == LookAhead.Type)
        {
            OutputDerivation("<sign> -> '-'");
            if(Match(TokenType.Minus))
                return OutputProductionRule("<sign> -> '-'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// OptionalAssignment production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool OptionalAssignment() 
    {
        if(!SkipErrors(FIRST_OptionalAssignment, FOLLOW_OptionalAssignment))
            return false;
        if (FIRST_AssignmentOperator.Contains(LookAhead.Type))
        {
            OutputDerivation("<optionalAssignment> -> <assignmentOperator> <expression>");
            if(AssignmentOperator() && Expression())
                return OutputProductionRule("<optionalAssignment> -> <assignmentOperator> <expression>");
            else
                return false;
        }
        else if (FOLLOW_OptionalAssignment.Contains(LookAhead.Type))
        {
            OutputDerivation("<optionalAssignment> -> EPSILON");
            return OutputProductionRule("<optionalAssignment> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveVariables production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveVariables() 
    {
        if(!SkipErrors(FIRST_RepetitiveVariables, FOLLOW_RepetitiveVariables))
            return false;
        if (FIRST_VariableIdnest.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariables> -> <variableIdnest> <repetitiveVariables>");
            if(VariableIdnest() && RepetitiveVariables())
                return OutputProductionRule("<repetitiveVariables> -> <variableIdnest> <repetitiveVariables>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveVariables.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariables> -> EPSILON");
            return OutputProductionRule("<repetitiveVariables> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// ArgumentParametersTail production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArgumentParametersTail() 
    {
        if(!SkipErrors(FIRST_ArgumentParametersTail, FOLLOW_ArgumentParametersTail))
            return false;
        if (TokenType.Comma == LookAhead.Type)
        {
            OutputDerivation("<argumentParametersTail> -> ',' <expression>");
            if(Match(TokenType.Comma) && Expression())
                return OutputProductionRule("<argumentParametersTail> -> ',' <expression>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveVariableOrFunctionCall production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveVariableOrFunctionCall() 
    {
        if(!SkipErrors(FIRST_RepetitiveVariableOrFunctionCall, FOLLOW_RepetitiveVariableOrFunctionCall))
            return false;
        if (FIRST_Idnest.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableOrFunctionCall> -> <idnest> <repetitiveVariableOrFunctionCall>");
            if(Idnest() && RepetitiveVariableOrFunctionCall())
                return OutputProductionRule("<repetitiveVariableOrFunctionCall> -> <idnest> <repetitiveVariableOrFunctionCall>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveVariableOrFunctionCall.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableOrFunctionCall> -> EPSILON");
            return OutputProductionRule("<repetitiveVariableOrFunctionCall> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// FunctionDeclaration production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FunctionDeclaration() 
    {
        if(!SkipErrors(FIRST_FunctionDeclaration, FOLLOW_FunctionDeclaration))
            return false;
        if (FIRST_FunctionHeader.Contains(LookAhead.Type))
        {
            OutputDerivation("<functionDeclaration> -> <functionHeader> ';'");
            if(FunctionHeader() && Match(TokenType.Semi))
                return OutputProductionRule("<functionDeclaration> -> <functionHeader> ';'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// ImplDefinition production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ImplDefinition() 
    {
        if(!SkipErrors(FIRST_ImplDefinition, FOLLOW_ImplDefinition))
            return false;
        if (TokenType.Impl == LookAhead.Type)
        {
            OutputDerivation("<implDefinition> -> 'impl' 'id' '{' <repetitiveFunctionDefinitions> '}'");
            if(Match(TokenType.Impl) && Match(TokenType.Id) && Match(TokenType.Opencubr) && RepetitiveFunctionDefinitions() && Match(TokenType.Closecubr))
                return OutputProductionRule("<implDefinition> -> 'impl' 'id' '{' <repetitiveFunctionDefinitions> '}'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RepetitiveStatements production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveStatements() 
    {
        if(!SkipErrors(FIRST_RepetitiveStatements, FOLLOW_RepetitiveStatements))
            return false;
        if (FIRST_Statement.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveStatements> -> <statement> <repetitiveStatements>");
            if(Statement() && RepetitiveStatements())
                return OutputProductionRule("<repetitiveStatements> -> <statement> <repetitiveStatements>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveStatements.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveStatements> -> EPSILON");
            return OutputProductionRule("<repetitiveStatements> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// VariableIdnestRest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VariableIdnestRest() 
    {
        if(!SkipErrors(FIRST_VariableIdnestRest, FOLLOW_VariableIdnestRest))
            return false;
        if (TokenType.Openpar == LookAhead.Type)
        {
            OutputDerivation("<variableIdnestRest> -> '(' <argumentParameters> ')' <variableIdnest>");
            if(Match(TokenType.Openpar) && ArgumentParameters() && Match(TokenType.Closepar) && VariableIdnest())
                return OutputProductionRule("<variableIdnestRest> -> '(' <argumentParameters> ')' <variableIdnest>");
            else
                return false;
        }
        else if (FIRST_RepetitiveIndices.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableIdnestRest> -> <repetitiveIndices>");
            if(RepetitiveIndices())
                return OutputProductionRule("<variableIdnestRest> -> <repetitiveIndices>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RecursiveTerms production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RecursiveTerms() 
    {
        if(!SkipErrors(FIRST_RecursiveTerms, FOLLOW_RecursiveTerms))
            return false;
        if (FIRST_MultOperator.Contains(LookAhead.Type))
        {
            OutputDerivation("<recursiveTerms> -> <multOperator> <factor> <recursiveTerms>");
            if(MultOperator() && Factor() && RecursiveTerms())
                return OutputProductionRule("<recursiveTerms> -> <multOperator> <factor> <recursiveTerms>");
            else
                return false;
        }
        else if (FOLLOW_RecursiveTerms.Contains(LookAhead.Type))
        {
            OutputDerivation("<recursiveTerms> -> EPSILON");
            return OutputProductionRule("<recursiveTerms> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Factor production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Factor() 
    {
        if(!SkipErrors(FIRST_Factor, FOLLOW_Factor))
            return false;
        if (TokenType.Id == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'id' <factorOpt>");
            if(Match(TokenType.Id) && FactorOpt())
                return OutputProductionRule("<factor> -> 'id' <factorOpt>");
            else
                return false;
        }
        else if (TokenType.Intnum == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'intLit'");
            if(Match(TokenType.Intnum))
                return OutputProductionRule("<factor> -> 'intLit'");
            else
                return false;
        }
        else if (TokenType.Floatnum == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'floatLit'");
            if(Match(TokenType.Floatnum))
                return OutputProductionRule("<factor> -> 'floatLit'");
            else
                return false;
        }
        else if (TokenType.Openpar == LookAhead.Type)
        {
            OutputDerivation("<factor> -> '(' <arithmeticExpression> ')'");
            if(Match(TokenType.Openpar) && ArithmeticExpression() && Match(TokenType.Closepar))
                return OutputProductionRule("<factor> -> '(' <arithmeticExpression> ')'");
            else
                return false;
        }
        else if (TokenType.Not == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'not' <factor>");
            if(Match(TokenType.Not) && Factor())
                return OutputProductionRule("<factor> -> 'not' <factor>");
            else
                return false;
        }
        else if (FIRST_Sign.Contains(LookAhead.Type))
        {
            OutputDerivation("<factor> -> <sign> <factor>");
            if(Sign() && Factor())
                return OutputProductionRule("<factor> -> <sign> <factor>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Indice production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Indice() 
    {
        if(!SkipErrors(FIRST_Indice, FOLLOW_Indice))
            return false;
        if (TokenType.Opensqbr == LookAhead.Type)
        {
            OutputDerivation("<indice> -> '[' <arithmeticExpression> ']'");
            if(Match(TokenType.Opensqbr) && ArithmeticExpression() && Match(TokenType.Closesqbr))
                return OutputProductionRule("<indice> -> '[' <arithmeticExpression> ']'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// OptionalRelationalExpression production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool OptionalRelationalExpression() 
    {
        if(!SkipErrors(FIRST_OptionalRelationalExpression, FOLLOW_OptionalRelationalExpression))
            return false;
        if (FIRST_RelationalOperator.Contains(LookAhead.Type))
        {
            OutputDerivation("<optionalRelationalExpression> -> <relationalOperator> <arithmeticExpression>");
            if(RelationalOperator() && ArithmeticExpression())
                return OutputProductionRule("<optionalRelationalExpression> -> <relationalOperator> <arithmeticExpression>");
            else
                return false;
        }
        else if (FOLLOW_OptionalRelationalExpression.Contains(LookAhead.Type))
        {
            OutputDerivation("<optionalRelationalExpression> -> EPSILON");
            return OutputProductionRule("<optionalRelationalExpression> -> EPSILON");
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// RelationalExpression production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RelationalExpression() 
    {
        if(!SkipErrors(FIRST_RelationalExpression, FOLLOW_RelationalExpression))
            return false;
        if (FIRST_ArithmeticExpression.Contains(LookAhead.Type))
        {
            OutputDerivation("<relationalExpression> -> <arithmeticExpression> <relationalOperator> <arithmeticExpression>");
            if(ArithmeticExpression() && RelationalOperator() && ArithmeticExpression())
                return OutputProductionRule("<relationalExpression> -> <arithmeticExpression> <relationalOperator> <arithmeticExpression>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// AssignmentOperator production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool AssignmentOperator() 
    {
        if(!SkipErrors(FIRST_AssignmentOperator, FOLLOW_AssignmentOperator))
            return false;
        if (TokenType.Assign == LookAhead.Type)
        {
            OutputDerivation("<assignmentOperator> -> '='");
            if(Match(TokenType.Assign))
                return OutputProductionRule("<assignmentOperator> -> '='");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// ReturnType production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ReturnType() 
    {
        if(!SkipErrors(FIRST_ReturnType, FOLLOW_ReturnType))
            return false;
        if (FIRST_Type.Contains(LookAhead.Type))
        {
            OutputDerivation("<returnType> -> <type>");
            if(Type())
                return OutputProductionRule("<returnType> -> <type>");
            else
                return false;
        }
        else if (TokenType.Void == LookAhead.Type)
        {
            OutputDerivation("<returnType> -> 'void'");
            if(Match(TokenType.Void))
                return OutputProductionRule("<returnType> -> 'void'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// StructDeclaration production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StructDeclaration() 
    {
        if(!SkipErrors(FIRST_StructDeclaration, FOLLOW_StructDeclaration))
            return false;
        if (TokenType.Struct == LookAhead.Type)
        {
            OutputDerivation("<structDeclaration> -> 'struct' 'id' <structOptionalInheritance> '{' <repetitiveStructMemberDeclarations> '}' ';'");
            if(Match(TokenType.Struct) && Match(TokenType.Id) && StructOptionalInheritance() && Match(TokenType.Opencubr) && RepetitiveStructMemberDeclarations() && Match(TokenType.Closecubr) && Match(TokenType.Semi))
                return OutputProductionRule("<structDeclaration> -> 'struct' 'id' <structOptionalInheritance> '{' <repetitiveStructMemberDeclarations> '}' ';'");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// Idnest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Idnest() 
    {
        if(!SkipErrors(FIRST_Idnest, FOLLOW_Idnest))
            return false;
        if (TokenType.Dot == LookAhead.Type)
        {
            OutputDerivation("<idnest> -> '.' 'id' <idnestRest>");
            if(Match(TokenType.Dot) && Match(TokenType.Id) && IdnestRest())
                return OutputProductionRule("<idnest> -> '.' 'id' <idnestRest>");
            else
                return false;
        }
        else
            return OutputError();
    } 

    /// <summary>
    /// StatementBlock production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StatementBlock() 
    {
        if(!SkipErrors(FIRST_StatementBlock, FOLLOW_StatementBlock))
            return false;
        if (TokenType.Opencubr == LookAhead.Type)
        {
            OutputDerivation("<statementBlock> -> '{' <repetitiveStatements> '}'");
            if(Match(TokenType.Opencubr) && RepetitiveStatements() && Match(TokenType.Closecubr))
                return OutputProductionRule("<statementBlock> -> '{' <repetitiveStatements> '}'");
            else
                return false;
        }
        else if (FIRST_Statement.Contains(LookAhead.Type))
        {
            OutputDerivation("<statementBlock> -> <statement>");
            if(Statement())
                return OutputProductionRule("<statementBlock> -> <statement>");
            else
                return false;
        }
        else if (FOLLOW_StatementBlock.Contains(LookAhead.Type))
        {
            OutputDerivation("<statementBlock> -> EPSILON");
            return OutputProductionRule("<statementBlock> -> EPSILON");
        }
        else
            return OutputError();
    }

    #endregion Productions
}
