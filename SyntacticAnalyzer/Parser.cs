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
            WriteLine($"Syntax error: Unexpected '{LookAhead.Lexeme}' at line {LookAhead.Location}. Expected {Token.TokenTypeToString(tokenType)}.");

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
            string[] expectedTokens = firstSet.Concat(followSet).Distinct().Where(x => x != Epsilon && x!=Eof).Select(x=>Token.TokenTypeToString(x)).ToArray();
            WriteLine($"Syntax error: Unexpected token '{LookAhead.Lexeme}' at line {LookAhead.Location}. Expected any of the following: {string.Join(", ", expectedTokens)}.");
            using StreamWriter sw = new(SourceName + OUT_SYNTAX_ERRORS_EXTENSION, true);
            sw.WriteLine($"Syntax error: Unexpected token '{LookAhead.Lexeme}' at line {LookAhead.Location}. Expected any of the following: {string.Join(", ", expectedTokens)}.");

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
    private bool OutputError(TokenType[] firstSet, TokenType[] followSet)
    {
        // Define the error message
        string[] expectedTokens = firstSet.Concat(followSet).Distinct().Where(x => x != Epsilon && x!=Eof).Select(x=>Token.TokenTypeToString(x)).ToArray();
        if (firstSet.Contains(Epsilon))
            expectedTokens = firstSet.Concat(followSet).Distinct().Where(x => x != Epsilon && x!=Eof).Select(x=>Token.TokenTypeToString(x)).ToArray();
        else
            expectedTokens = firstSet.Distinct().Where(x => x != Epsilon && x!=Eof).Select(x=>Token.TokenTypeToString(x)).ToArray();

        string errorMsg = $"Syntax error: Unexpected token '{LookAhead.Lexeme}' at line {LookAhead.Location}. Expected any of the following: {string.Join(", ", expectedTokens)}.";

        // Write the error message to the console and to the output file
        WriteLine(errorMsg);
        using StreamWriter sw = new(SourceName + OUT_SYNTAX_ERRORS_EXTENSION, true);
        sw.WriteLine(errorMsg);
        return false;
    }

    #endregion Base Methods

    #region First Sets
    
    private static readonly TokenType[] FIRST_AddOperator = new TokenType[] { Plus, Minus, Or }; 
    private static readonly TokenType[] FIRST_ArgumentParameters = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus, Epsilon }; 
    private static readonly TokenType[] FIRST_ArgumentParametersTail = new TokenType[] { Comma }; 
    private static readonly TokenType[] FIRST_ArithmeticExpression = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_ArraySize = new TokenType[] { Opensqbr }; 
    private static readonly TokenType[] FIRST_ArraySizeContent = new TokenType[] { Intnum, Closesqbr }; 
    private static readonly TokenType[] FIRST_AssignmentOperator = new TokenType[] { Assign }; 
    private static readonly TokenType[] FIRST_Expression = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_Factor = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_FactorAlt = new TokenType[] { Openpar, Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_FunctionBody = new TokenType[] { Opencubr }; 
    private static readonly TokenType[] FIRST_FunctionDeclaration = new TokenType[] { Func }; 
    private static readonly TokenType[] FIRST_FunctionDefinition = new TokenType[] { Func }; 
    private static readonly TokenType[] FIRST_FunctionHeader = new TokenType[] { Func }; 
    private static readonly TokenType[] FIRST_FunctionParametersTail = new TokenType[] { Comma }; 
    private static readonly TokenType[] FIRST_FuntionParameters = new TokenType[] { Id, Epsilon }; 
    private static readonly TokenType[] FIRST_Idnest = new TokenType[] { Dot }; 
    private static readonly TokenType[] FIRST_IdnestRest = new TokenType[] { Openpar, Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_IdnestRestStat = new TokenType[] { Openpar, Opensqbr, Dot, Assign }; 
    private static readonly TokenType[] FIRST_IdnestStat = new TokenType[] { Dot }; 
    private static readonly TokenType[] FIRST_ImplDefinition = new TokenType[] { Impl }; 
    private static readonly TokenType[] FIRST_Indice = new TokenType[] { Opensqbr }; 
    private static readonly TokenType[] FIRST_MemberDeclaration = new TokenType[] { Func, Let }; 
    private static readonly TokenType[] FIRST_MultOperator = new TokenType[] { Mult, Div, And }; 
    private static readonly TokenType[] FIRST_OptionalRelationalExpression = new TokenType[] { Eq, Noteq, Lt, Gt, Leq, Geq, Epsilon }; 
    private static readonly TokenType[] FIRST_RecursiveArithmeticExpression = new TokenType[] { Plus, Minus, Or, Epsilon }; 
    private static readonly TokenType[] FIRST_RecursiveTerms = new TokenType[] { Mult, Div, And, Epsilon }; 
    private static readonly TokenType[] FIRST_RelationalExpression = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_RelationalOperator = new TokenType[] { Eq, Noteq, Lt, Gt, Leq, Geq }; 
    private static readonly TokenType[] FIRST_RepetitiveArgumentParametersTail = new TokenType[] { Comma, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveArraySizes = new TokenType[] { Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveFunctionDefinitions = new TokenType[] { Func, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveFunctionParametersTails = new TokenType[] { Comma, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveIndices = new TokenType[] { Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveIndicesStat = new TokenType[] { Opensqbr, Dot, Assign }; 
    private static readonly TokenType[] FIRST_RepetitiveStatements = new TokenType[] { Id, If, While, TokenType.Read, TokenType.Write, Return, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveStructMemberDeclarations = new TokenType[] { Public, Private, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveStructOptionalInheritances = new TokenType[] { Comma, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveVariableDeclarationOrStatements = new TokenType[] { Let, Id, If, While, TokenType.Read, TokenType.Write, Return, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveVariableOrFunctionCall = new TokenType[] { Dot, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveVariableOrFunctionCallStat_Function = new TokenType[] { Dot, Epsilon }; 
    private static readonly TokenType[] FIRST_RepetitiveVariableOrFunctionCallStat_Var = new TokenType[] { Dot, Assign }; 
    private static readonly TokenType[] FIRST_RepetitiveVariables = new TokenType[] { Dot, Epsilon }; 
    private static readonly TokenType[] FIRST_ReturnType = new TokenType[] { Integer, Float, Id, TokenType.Void }; 
    private static readonly TokenType[] FIRST_Sign = new TokenType[] { Plus, Minus }; 
    private static readonly TokenType[] FIRST_Start = new TokenType[] { Struct, Impl, Func, Epsilon }; 
    private static readonly TokenType[] FIRST_Statement = new TokenType[] { Id, If, While, TokenType.Read, TokenType.Write, Return }; 
    private static readonly TokenType[] FIRST_StatementAlt = new TokenType[] { Openpar, Opensqbr, Dot, Assign }; 
    private static readonly TokenType[] FIRST_StatementBlock = new TokenType[] { Opencubr, Id, If, While, TokenType.Read, TokenType.Write, Return, Epsilon }; 
    private static readonly TokenType[] FIRST_StructDeclaration = new TokenType[] { Struct }; 
    private static readonly TokenType[] FIRST_StructOptionalInheritance = new TokenType[] { Inherits, Epsilon }; 
    private static readonly TokenType[] FIRST_StructOrImplOrFunction = new TokenType[] { Struct, Impl, Func }; 
    private static readonly TokenType[] FIRST_Term = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_Type = new TokenType[] { Integer, Float, Id }; 
    private static readonly TokenType[] FIRST_Variable = new TokenType[] { Id }; 
    private static readonly TokenType[] FIRST_VariableDeclaration = new TokenType[] { Let }; 
    private static readonly TokenType[] FIRST_VariableDeclarationOrStatement = new TokenType[] { Let, Id, If, While, TokenType.Read, TokenType.Write, Return }; 
    private static readonly TokenType[] FIRST_VariableIdnest = new TokenType[] { Dot }; 
    private static readonly TokenType[] FIRST_VariableIdnestRest = new TokenType[] { Openpar, Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_VariableRest = new TokenType[] { Opensqbr, Epsilon, Openpar, Dot }; 
    private static readonly TokenType[] FIRST_Visibility = new TokenType[] { Public, Private };

    #endregion First Sets

    #region Follow Sets
    
    private static readonly TokenType[] FOLLOW_AddOperator = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_ArgumentParameters = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_ArgumentParametersTail = new TokenType[] { Comma, Closepar }; 
    private static readonly TokenType[] FOLLOW_ArithmeticExpression = new TokenType[] { Comma, Closepar, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_ArraySize = new TokenType[] { Opensqbr, Comma, Closepar, Semi }; 
    private static readonly TokenType[] FOLLOW_ArraySizeContent = new TokenType[] { Opensqbr, Comma, Closepar, Semi }; 
    private static readonly TokenType[] FOLLOW_AssignmentOperator = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_Expression = new TokenType[] { Comma, Closepar, Semi }; 
    private static readonly TokenType[] FOLLOW_Factor = new TokenType[] { Mult, Div, And, Plus, Minus, Or, Comma, Closepar, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_FactorAlt = new TokenType[] { Dot, Mult, Div, And, Plus, Minus, Or, Comma, Closepar, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_FunctionBody = new TokenType[] { Func, Closecubr, Struct, Impl }; 
    private static readonly TokenType[] FOLLOW_FunctionDeclaration = new TokenType[] { Public, Private, Closecubr }; 
    private static readonly TokenType[] FOLLOW_FunctionDefinition = new TokenType[] { Func, Closecubr, Struct, Impl }; 
    private static readonly TokenType[] FOLLOW_FunctionHeader = new TokenType[] { Opencubr, Semi }; 
    private static readonly TokenType[] FOLLOW_FunctionParametersTail = new TokenType[] { Comma, Closepar }; 
    private static readonly TokenType[] FOLLOW_FuntionParameters = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_Idnest = new TokenType[] { Dot, Mult, Div, And, Plus, Minus, Or, Comma, Closepar, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_IdnestRest = new TokenType[] { Dot, Mult, Div, And, Plus, Minus, Or, Comma, Closepar, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_IdnestRestStat = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_IdnestStat = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_ImplDefinition = new TokenType[] { Struct, Impl, Func }; 
    private static readonly TokenType[] FOLLOW_Indice = new TokenType[] { Opensqbr, Dot, Assign, Closepar, Mult, Div, And, Plus, Minus, Or, Comma, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_MemberDeclaration = new TokenType[] { Public, Private, Closecubr }; 
    private static readonly TokenType[] FOLLOW_MultOperator = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_OptionalRelationalExpression = new TokenType[] { Comma, Closepar, Semi }; 
    private static readonly TokenType[] FOLLOW_RecursiveArithmeticExpression = new TokenType[] { Comma, Closepar, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_RecursiveTerms = new TokenType[] { Plus, Minus, Or, Comma, Closepar, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_RelationalExpression = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_RelationalOperator = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_RepetitiveArgumentParametersTail = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_RepetitiveArraySizes = new TokenType[] { Comma, Closepar, Semi }; 
    private static readonly TokenType[] FOLLOW_RepetitiveFunctionDefinitions = new TokenType[] { Closecubr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveFunctionParametersTails = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_RepetitiveIndices = new TokenType[] { Dot, Closepar, Mult, Div, And, Plus, Minus, Or, Comma, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveIndicesStat = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_RepetitiveStatements = new TokenType[] { Closecubr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveStructMemberDeclarations = new TokenType[] { Closecubr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveStructOptionalInheritances = new TokenType[] { Opencubr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveVariableDeclarationOrStatements = new TokenType[] { Closecubr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveVariableOrFunctionCall = new TokenType[] { Mult, Div, And, Plus, Minus, Or, Comma, Closepar, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_RepetitiveVariableOrFunctionCallStat_Function = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_RepetitiveVariableOrFunctionCallStat_Var = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_RepetitiveVariables = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_ReturnType = new TokenType[] { Opencubr, Semi }; 
    private static readonly TokenType[] FOLLOW_Sign = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_Start = new TokenType[] { Eof }; 
    private static readonly TokenType[] FOLLOW_Statement = new TokenType[] { Id, If, While, TokenType.Read, TokenType.Write, Return, Closecubr, Let, Else, Semi }; 
    private static readonly TokenType[] FOLLOW_StatementAlt = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_StatementBlock = new TokenType[] { Else, Semi }; 
    private static readonly TokenType[] FOLLOW_StructDeclaration = new TokenType[] { Struct, Impl, Func }; 
    private static readonly TokenType[] FOLLOW_StructOptionalInheritance = new TokenType[] { Opencubr }; 
    private static readonly TokenType[] FOLLOW_StructOrImplOrFunction = new TokenType[] { Struct, Impl, Func }; 
    private static readonly TokenType[] FOLLOW_Term = new TokenType[] { Plus, Minus, Or, Comma, Closepar, Semi, Eq, Noteq, Lt, Gt, Leq, Geq, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_Type = new TokenType[] { Opensqbr, Comma, Closepar, Semi, Opencubr }; 
    private static readonly TokenType[] FOLLOW_Variable = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_VariableDeclaration = new TokenType[] { Public, Private, Closecubr, Let, Id, If, While, TokenType.Read, TokenType.Write, Return }; 
    private static readonly TokenType[] FOLLOW_VariableDeclarationOrStatement = new TokenType[] { Let, Id, If, While, TokenType.Read, TokenType.Write, Return, Closecubr }; 
    private static readonly TokenType[] FOLLOW_VariableIdnest = new TokenType[] { Closepar, Dot }; 
    private static readonly TokenType[] FOLLOW_VariableIdnestRest = new TokenType[] { Closepar, Dot }; 
    private static readonly TokenType[] FOLLOW_VariableRest = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_Visibility = new TokenType[] { Func, Let };

    #endregion Follow Sets

    #region Productions
    

    /// <summary>
    /// AddOperator production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool AddOperator() 
    {
        if(!SkipErrors(FIRST_AddOperator, FOLLOW_AddOperator))
            return false;
        if (Plus == LookAhead.Type)
        {
            OutputDerivation("<addOperator> -> '+'");
            if(Match(Plus))
                return OutputProductionRule("<addOperator> -> '+'");
            else
                return false;
        }
        else if (Minus == LookAhead.Type)
        {
            OutputDerivation("<addOperator> -> '-'");
            if(Match(Minus))
                return OutputProductionRule("<addOperator> -> '-'");
            else
                return false;
        }
        else if (Or == LookAhead.Type)
        {
            OutputDerivation("<addOperator> -> 'or'");
            if(Match(Or))
                return OutputProductionRule("<addOperator> -> 'or'");
            else
                return false;
        }
        else
            return OutputError(FIRST_AddOperator, FOLLOW_AddOperator);
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
            return OutputError(FIRST_ArgumentParameters, FOLLOW_ArgumentParameters);
    } 

    /// <summary>
    /// ArgumentParametersTail production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArgumentParametersTail() 
    {
        if(!SkipErrors(FIRST_ArgumentParametersTail, FOLLOW_ArgumentParametersTail))
            return false;
        if (Comma == LookAhead.Type)
        {
            OutputDerivation("<argumentParametersTail> -> ',' <expression>");
            if(Match(Comma) && Expression())
                return OutputProductionRule("<argumentParametersTail> -> ',' <expression>");
            else
                return false;
        }
        else
            return OutputError(FIRST_ArgumentParametersTail, FOLLOW_ArgumentParametersTail);
    } 

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
            return OutputError(FIRST_ArithmeticExpression, FOLLOW_ArithmeticExpression);
    } 

    /// <summary>
    /// ArraySize production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArraySize() 
    {
        if(!SkipErrors(FIRST_ArraySize, FOLLOW_ArraySize))
            return false;
        if (Opensqbr == LookAhead.Type)
        {
            OutputDerivation("<arraySize> -> '[' <arraySizeContent>");
            if(Match(Opensqbr) && ArraySizeContent())
                return OutputProductionRule("<arraySize> -> '[' <arraySizeContent>");
            else
                return false;
        }
        else
            return OutputError(FIRST_ArraySize, FOLLOW_ArraySize);
    } 

    /// <summary>
    /// ArraySizeContent production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArraySizeContent() 
    {
        if(!SkipErrors(FIRST_ArraySizeContent, FOLLOW_ArraySizeContent))
            return false;
        if (Intnum == LookAhead.Type)
        {
            OutputDerivation("<arraySizeContent> -> 'intNum' ']'");
            if(Match(Intnum) && Match(Closesqbr))
                return OutputProductionRule("<arraySizeContent> -> 'intNum' ']'");
            else
                return false;
        }
        else if (Closesqbr == LookAhead.Type)
        {
            OutputDerivation("<arraySizeContent> -> ']'");
            if(Match(Closesqbr))
                return OutputProductionRule("<arraySizeContent> -> ']'");
            else
                return false;
        }
        else
            return OutputError(FIRST_ArraySizeContent, FOLLOW_ArraySizeContent);
    } 

    /// <summary>
    /// AssignmentOperator production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool AssignmentOperator() 
    {
        if(!SkipErrors(FIRST_AssignmentOperator, FOLLOW_AssignmentOperator))
            return false;
        if (Assign == LookAhead.Type)
        {
            OutputDerivation("<assignmentOperator> -> '='");
            if(Match(Assign))
                return OutputProductionRule("<assignmentOperator> -> '='");
            else
                return false;
        }
        else
            return OutputError(FIRST_AssignmentOperator, FOLLOW_AssignmentOperator);
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
            return OutputError(FIRST_Expression, FOLLOW_Expression);
    } 

    /// <summary>
    /// Factor production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Factor() 
    {
        if(!SkipErrors(FIRST_Factor, FOLLOW_Factor))
            return false;
        if (Id == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'id' <factorAlt> <repetitiveVariableOrFunctionCall>");
            if(Match(Id) && FactorAlt() && RepetitiveVariableOrFunctionCall())
                return OutputProductionRule("<factor> -> 'id' <factorAlt> <repetitiveVariableOrFunctionCall>");
            else
                return false;
        }
        else if (Intnum == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'intLit'");
            if(Match(Intnum))
                return OutputProductionRule("<factor> -> 'intLit'");
            else
                return false;
        }
        else if (Floatnum == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'floatLit'");
            if(Match(Floatnum))
                return OutputProductionRule("<factor> -> 'floatLit'");
            else
                return false;
        }
        else if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<factor> -> '(' <arithmeticExpression> ')'");
            if(Match(Openpar) && ArithmeticExpression() && Match(Closepar))
                return OutputProductionRule("<factor> -> '(' <arithmeticExpression> ')'");
            else
                return false;
        }
        else if (Not == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'not' <factor>");
            if(Match(Not) && Factor())
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
            return OutputError(FIRST_Factor, FOLLOW_Factor);
    } 

    /// <summary>
    /// FactorAlt production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FactorAlt() 
    {
        if(!SkipErrors(FIRST_FactorAlt, FOLLOW_FactorAlt))
            return false;
        if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<factorAlt> -> '(' <argumentParameters> ')'");
            if(Match(Openpar) && ArgumentParameters() && Match(Closepar))
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
        else if (FOLLOW_FactorAlt.Contains(LookAhead.Type))
        {
            OutputDerivation("<factorAlt> -> EPSILON");
            return OutputProductionRule("<factorAlt> -> EPSILON");
        }
        else
            return OutputError(FIRST_FactorAlt, FOLLOW_FactorAlt);
    } 

    /// <summary>
    /// FunctionBody production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FunctionBody() 
    {
        if(!SkipErrors(FIRST_FunctionBody, FOLLOW_FunctionBody))
            return false;
        if (Opencubr == LookAhead.Type)
        {
            OutputDerivation("<functionBody> -> '{' <repetitiveVariableDeclarationOrStatements> '}'");
            if(Match(Opencubr) && RepetitiveVariableDeclarationOrStatements() && Match(Closecubr))
                return OutputProductionRule("<functionBody> -> '{' <repetitiveVariableDeclarationOrStatements> '}'");
            else
                return false;
        }
        else
            return OutputError(FIRST_FunctionBody, FOLLOW_FunctionBody);
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
            if(FunctionHeader() && Match(Semi))
                return OutputProductionRule("<functionDeclaration> -> <functionHeader> ';'");
            else
                return false;
        }
        else
            return OutputError(FIRST_FunctionDeclaration, FOLLOW_FunctionDeclaration);
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
            return OutputError(FIRST_FunctionDefinition, FOLLOW_FunctionDefinition);
    } 

    /// <summary>
    /// FunctionHeader production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FunctionHeader() 
    {
        if(!SkipErrors(FIRST_FunctionHeader, FOLLOW_FunctionHeader))
            return false;
        if (Func == LookAhead.Type)
        {
            OutputDerivation("<functionHeader> -> 'func' 'id' '(' <funtionParameters> ')' '->' <returnType>");
            if(Match(Func) && Match(Id) && Match(Openpar) && FuntionParameters() && Match(Closepar) && Match(Arrow) && ReturnType())
                return OutputProductionRule("<functionHeader> -> 'func' 'id' '(' <funtionParameters> ')' '->' <returnType>");
            else
                return false;
        }
        else
            return OutputError(FIRST_FunctionHeader, FOLLOW_FunctionHeader);
    } 

    /// <summary>
    /// FunctionParametersTail production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FunctionParametersTail() 
    {
        if(!SkipErrors(FIRST_FunctionParametersTail, FOLLOW_FunctionParametersTail))
            return false;
        if (Comma == LookAhead.Type)
        {
            OutputDerivation("<functionParametersTail> -> ',' 'id' ':' <type> <repetitiveArraySizes>");
            if(Match(Comma) && Match(Id) && Match(Colon) && Type() && RepetitiveArraySizes())
                return OutputProductionRule("<functionParametersTail> -> ',' 'id' ':' <type> <repetitiveArraySizes>");
            else
                return false;
        }
        else
            return OutputError(FIRST_FunctionParametersTail, FOLLOW_FunctionParametersTail);
    } 

    /// <summary>
    /// FuntionParameters production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FuntionParameters() 
    {
        if(!SkipErrors(FIRST_FuntionParameters, FOLLOW_FuntionParameters))
            return false;
        if (Id == LookAhead.Type)
        {
            OutputDerivation("<funtionParameters> -> 'id' ':' <type> <repetitiveArraySizes> <repetitiveFunctionParametersTails>");
            if(Match(Id) && Match(Colon) && Type() && RepetitiveArraySizes() && RepetitiveFunctionParametersTails())
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
            return OutputError(FIRST_FuntionParameters, FOLLOW_FuntionParameters);
    } 

    /// <summary>
    /// Idnest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Idnest() 
    {
        if(!SkipErrors(FIRST_Idnest, FOLLOW_Idnest))
            return false;
        if (Dot == LookAhead.Type)
        {
            OutputDerivation("<idnest> -> '.' 'id' <idnestRest>");
            if(Match(Dot) && Match(Id) && IdnestRest())
                return OutputProductionRule("<idnest> -> '.' 'id' <idnestRest>");
            else
                return false;
        }
        else
            return OutputError(FIRST_Idnest, FOLLOW_Idnest);
    } 

    /// <summary>
    /// IdnestRest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool IdnestRest() 
    {
        if(!SkipErrors(FIRST_IdnestRest, FOLLOW_IdnestRest))
            return false;
        if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<idnestRest> -> '(' <argumentParameters> ')'");
            if(Match(Openpar) && ArgumentParameters() && Match(Closepar))
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
        else if (FOLLOW_IdnestRest.Contains(LookAhead.Type))
        {
            OutputDerivation("<idnestRest> -> EPSILON");
            return OutputProductionRule("<idnestRest> -> EPSILON");
        }
        else
            return OutputError(FIRST_IdnestRest, FOLLOW_IdnestRest);
    } 

    /// <summary>
    /// IdnestRestStat production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool IdnestRestStat() 
    {
        if(!SkipErrors(FIRST_IdnestRestStat, FOLLOW_IdnestRestStat))
            return false;
        if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<idnestRestStat> -> '(' <argumentParameters> ')' <repetitiveVariableOrFunctionCallStat_Function>");
            if(Match(Openpar) && ArgumentParameters() && Match(Closepar) && RepetitiveVariableOrFunctionCallStat_Function())
                return OutputProductionRule("<idnestRestStat> -> '(' <argumentParameters> ')' <repetitiveVariableOrFunctionCallStat_Function>");
            else
                return false;
        }
        else if (FIRST_RepetitiveIndicesStat.Contains(LookAhead.Type))
        {
            OutputDerivation("<idnestRestStat> -> <repetitiveIndicesStat>");
            if(RepetitiveIndicesStat())
                return OutputProductionRule("<idnestRestStat> -> <repetitiveIndicesStat>");
            else
                return false;
        }
        else
            return OutputError(FIRST_IdnestRestStat, FOLLOW_IdnestRestStat);
    } 

    /// <summary>
    /// IdnestStat production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool IdnestStat() 
    {
        if(!SkipErrors(FIRST_IdnestStat, FOLLOW_IdnestStat))
            return false;
        if (Dot == LookAhead.Type)
        {
            OutputDerivation("<idnestStat> -> '.' 'id' <idnestRestStat>");
            if(Match(Dot) && Match(Id) && IdnestRestStat())
                return OutputProductionRule("<idnestStat> -> '.' 'id' <idnestRestStat>");
            else
                return false;
        }
        else
            return OutputError(FIRST_IdnestStat, FOLLOW_IdnestStat);
    } 

    /// <summary>
    /// ImplDefinition production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ImplDefinition() 
    {
        if(!SkipErrors(FIRST_ImplDefinition, FOLLOW_ImplDefinition))
            return false;
        if (Impl == LookAhead.Type)
        {
            OutputDerivation("<implDefinition> -> 'impl' 'id' '{' <repetitiveFunctionDefinitions> '}'");
            if(Match(Impl) && Match(Id) && Match(Opencubr) && RepetitiveFunctionDefinitions() && Match(Closecubr))
                return OutputProductionRule("<implDefinition> -> 'impl' 'id' '{' <repetitiveFunctionDefinitions> '}'");
            else
                return false;
        }
        else
            return OutputError(FIRST_ImplDefinition, FOLLOW_ImplDefinition);
    } 

    /// <summary>
    /// Indice production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Indice() 
    {
        if(!SkipErrors(FIRST_Indice, FOLLOW_Indice))
            return false;
        if (Opensqbr == LookAhead.Type)
        {
            OutputDerivation("<indice> -> '[' <arithmeticExpression> ']'");
            if(Match(Opensqbr) && ArithmeticExpression() && Match(Closesqbr))
                return OutputProductionRule("<indice> -> '[' <arithmeticExpression> ']'");
            else
                return false;
        }
        else
            return OutputError(FIRST_Indice, FOLLOW_Indice);
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
            return OutputError(FIRST_MemberDeclaration, FOLLOW_MemberDeclaration);
    } 

    /// <summary>
    /// MultOperator production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool MultOperator() 
    {
        if(!SkipErrors(FIRST_MultOperator, FOLLOW_MultOperator))
            return false;
        if (Mult == LookAhead.Type)
        {
            OutputDerivation("<multOperator> -> '*'");
            if(Match(Mult))
                return OutputProductionRule("<multOperator> -> '*'");
            else
                return false;
        }
        else if (Div == LookAhead.Type)
        {
            OutputDerivation("<multOperator> -> '/'");
            if(Match(Div))
                return OutputProductionRule("<multOperator> -> '/'");
            else
                return false;
        }
        else if (And == LookAhead.Type)
        {
            OutputDerivation("<multOperator> -> 'and'");
            if(Match(And))
                return OutputProductionRule("<multOperator> -> 'and'");
            else
                return false;
        }
        else
            return OutputError(FIRST_MultOperator, FOLLOW_MultOperator);
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
            return OutputError(FIRST_OptionalRelationalExpression, FOLLOW_OptionalRelationalExpression);
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
            return OutputError(FIRST_RecursiveArithmeticExpression, FOLLOW_RecursiveArithmeticExpression);
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
            return OutputError(FIRST_RecursiveTerms, FOLLOW_RecursiveTerms);
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
            return OutputError(FIRST_RelationalExpression, FOLLOW_RelationalExpression);
    } 

    /// <summary>
    /// RelationalOperator production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RelationalOperator() 
    {
        if(!SkipErrors(FIRST_RelationalOperator, FOLLOW_RelationalOperator))
            return false;
        if (Eq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'eq'");
            if(Match(Eq))
                return OutputProductionRule("<relationalOperator> -> 'eq'");
            else
                return false;
        }
        else if (Noteq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'neq'");
            if(Match(Noteq))
                return OutputProductionRule("<relationalOperator> -> 'neq'");
            else
                return false;
        }
        else if (Lt == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'lt'");
            if(Match(Lt))
                return OutputProductionRule("<relationalOperator> -> 'lt'");
            else
                return false;
        }
        else if (Gt == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'gt'");
            if(Match(Gt))
                return OutputProductionRule("<relationalOperator> -> 'gt'");
            else
                return false;
        }
        else if (Leq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'leq'");
            if(Match(Leq))
                return OutputProductionRule("<relationalOperator> -> 'leq'");
            else
                return false;
        }
        else if (Geq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'geq'");
            if(Match(Geq))
                return OutputProductionRule("<relationalOperator> -> 'geq'");
            else
                return false;
        }
        else
            return OutputError(FIRST_RelationalOperator, FOLLOW_RelationalOperator);
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
            return OutputError(FIRST_RepetitiveArgumentParametersTail, FOLLOW_RepetitiveArgumentParametersTail);
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
            return OutputError(FIRST_RepetitiveArraySizes, FOLLOW_RepetitiveArraySizes);
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
            return OutputError(FIRST_RepetitiveFunctionDefinitions, FOLLOW_RepetitiveFunctionDefinitions);
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
            return OutputError(FIRST_RepetitiveFunctionParametersTails, FOLLOW_RepetitiveFunctionParametersTails);
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
            return OutputError(FIRST_RepetitiveIndices, FOLLOW_RepetitiveIndices);
    } 

    /// <summary>
    /// RepetitiveIndicesStat production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveIndicesStat() 
    {
        if(!SkipErrors(FIRST_RepetitiveIndicesStat, FOLLOW_RepetitiveIndicesStat))
            return false;
        if (FIRST_Indice.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveIndicesStat> -> <indice> <repetitiveIndicesStat>");
            if(Indice() && RepetitiveIndicesStat())
                return OutputProductionRule("<repetitiveIndicesStat> -> <indice> <repetitiveIndicesStat>");
            else
                return false;
        }
        else if (FIRST_RepetitiveVariableOrFunctionCallStat_Var.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveIndicesStat> -> <repetitiveVariableOrFunctionCallStat_Var>");
            if(RepetitiveVariableOrFunctionCallStat_Var())
                return OutputProductionRule("<repetitiveIndicesStat> -> <repetitiveVariableOrFunctionCallStat_Var>");
            else
                return false;
        }
        else
            return OutputError(FIRST_RepetitiveIndicesStat, FOLLOW_RepetitiveIndicesStat);
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
            return OutputError(FIRST_RepetitiveStatements, FOLLOW_RepetitiveStatements);
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
            return OutputError(FIRST_RepetitiveStructMemberDeclarations, FOLLOW_RepetitiveStructMemberDeclarations);
    } 

    /// <summary>
    /// RepetitiveStructOptionalInheritances production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveStructOptionalInheritances() 
    {
        if(!SkipErrors(FIRST_RepetitiveStructOptionalInheritances, FOLLOW_RepetitiveStructOptionalInheritances))
            return false;
        if (Comma == LookAhead.Type)
        {
            OutputDerivation("<repetitiveStructOptionalInheritances> -> ',' 'id' <repetitiveStructOptionalInheritances>");
            if(Match(Comma) && Match(Id) && RepetitiveStructOptionalInheritances())
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
            return OutputError(FIRST_RepetitiveStructOptionalInheritances, FOLLOW_RepetitiveStructOptionalInheritances);
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
            return OutputError(FIRST_RepetitiveVariableDeclarationOrStatements, FOLLOW_RepetitiveVariableDeclarationOrStatements);
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
            return OutputError(FIRST_RepetitiveVariableOrFunctionCall, FOLLOW_RepetitiveVariableOrFunctionCall);
    } 

    /// <summary>
    /// RepetitiveVariableOrFunctionCallStat_Function production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveVariableOrFunctionCallStat_Function() 
    {
        if(!SkipErrors(FIRST_RepetitiveVariableOrFunctionCallStat_Function, FOLLOW_RepetitiveVariableOrFunctionCallStat_Function))
            return false;
        if (FIRST_IdnestStat.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableOrFunctionCallStat_Function> -> <idnestStat>");
            if(IdnestStat())
                return OutputProductionRule("<repetitiveVariableOrFunctionCallStat_Function> -> <idnestStat>");
            else
                return false;
        }
        else if (FOLLOW_RepetitiveVariableOrFunctionCallStat_Function.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableOrFunctionCallStat_Function> -> EPSILON");
            return OutputProductionRule("<repetitiveVariableOrFunctionCallStat_Function> -> EPSILON");
        }
        else
            return OutputError(FIRST_RepetitiveVariableOrFunctionCallStat_Function, FOLLOW_RepetitiveVariableOrFunctionCallStat_Function);
    } 

    /// <summary>
    /// RepetitiveVariableOrFunctionCallStat_Var production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RepetitiveVariableOrFunctionCallStat_Var() 
    {
        if(!SkipErrors(FIRST_RepetitiveVariableOrFunctionCallStat_Var, FOLLOW_RepetitiveVariableOrFunctionCallStat_Var))
            return false;
        if (FIRST_IdnestStat.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableOrFunctionCallStat_Var> -> <idnestStat>");
            if(IdnestStat())
                return OutputProductionRule("<repetitiveVariableOrFunctionCallStat_Var> -> <idnestStat>");
            else
                return false;
        }
        else if (FIRST_AssignmentOperator.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableOrFunctionCallStat_Var> -> <assignmentOperator> <expression>");
            if(AssignmentOperator() && Expression())
                return OutputProductionRule("<repetitiveVariableOrFunctionCallStat_Var> -> <assignmentOperator> <expression>");
            else
                return false;
        }
        else
            return OutputError(FIRST_RepetitiveVariableOrFunctionCallStat_Var, FOLLOW_RepetitiveVariableOrFunctionCallStat_Var);
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
            return OutputError(FIRST_RepetitiveVariables, FOLLOW_RepetitiveVariables);
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
            return OutputError(FIRST_ReturnType, FOLLOW_ReturnType);
    } 

    /// <summary>
    /// Sign production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Sign() 
    {
        if(!SkipErrors(FIRST_Sign, FOLLOW_Sign))
            return false;
        if (Plus == LookAhead.Type)
        {
            OutputDerivation("<sign> -> '+'");
            if(Match(Plus))
                return OutputProductionRule("<sign> -> '+'");
            else
                return false;
        }
        else if (Minus == LookAhead.Type)
        {
            OutputDerivation("<sign> -> '-'");
            if(Match(Minus))
                return OutputProductionRule("<sign> -> '-'");
            else
                return false;
        }
        else
            return OutputError(FIRST_Sign, FOLLOW_Sign);
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
            return OutputError(FIRST_Start, FOLLOW_Start);
    } 

    /// <summary>
    /// Statement production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Statement() 
    {
        if(!SkipErrors(FIRST_Statement, FOLLOW_Statement))
            return false;
        if (Id == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'id' <statementAlt> ';'");
            if(Match(Id) && StatementAlt() && Match(Semi))
                return OutputProductionRule("<statement> -> 'id' <statementAlt> ';'");
            else
                return false;
        }
        else if (If == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'if' '(' <relationalExpression> ')' 'then' <statementBlock> 'else' <statementBlock> ';'");
            if(Match(If) && Match(Openpar) && RelationalExpression() && Match(Closepar) && Match(Then) && StatementBlock() && Match(Else) && StatementBlock() && Match(Semi))
                return OutputProductionRule("<statement> -> 'if' '(' <relationalExpression> ')' 'then' <statementBlock> 'else' <statementBlock> ';'");
            else
                return false;
        }
        else if (While == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'while' '(' <relationalExpression> ')' <statementBlock> ';'");
            if(Match(While) && Match(Openpar) && RelationalExpression() && Match(Closepar) && StatementBlock() && Match(Semi))
                return OutputProductionRule("<statement> -> 'while' '(' <relationalExpression> ')' <statementBlock> ';'");
            else
                return false;
        }
        else if (TokenType.Read == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'read' '(' <variable> ')' ';'");
            if(Match(TokenType.Read) && Match(Openpar) && Variable() && Match(Closepar) && Match(Semi))
                return OutputProductionRule("<statement> -> 'read' '(' <variable> ')' ';'");
            else
                return false;
        }
        else if (TokenType.Write == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'write' '(' <expression> ')' ';'");
            if(Match(TokenType.Write) && Match(Openpar) && Expression() && Match(Closepar) && Match(Semi))
                return OutputProductionRule("<statement> -> 'write' '(' <expression> ')' ';'");
            else
                return false;
        }
        else if (Return == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'return' '(' <expression> ')' ';'");
            if(Match(Return) && Match(Openpar) && Expression() && Match(Closepar) && Match(Semi))
                return OutputProductionRule("<statement> -> 'return' '(' <expression> ')' ';'");
            else
                return false;
        }
        else
            return OutputError(FIRST_Statement, FOLLOW_Statement);
    } 

    /// <summary>
    /// StatementAlt production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StatementAlt() 
    {
        if(!SkipErrors(FIRST_StatementAlt, FOLLOW_StatementAlt))
            return false;
        if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<statementAlt> -> '(' <argumentParameters> ')' <repetitiveVariableOrFunctionCallStat_Function>");
            if(Match(Openpar) && ArgumentParameters() && Match(Closepar) && RepetitiveVariableOrFunctionCallStat_Function())
                return OutputProductionRule("<statementAlt> -> '(' <argumentParameters> ')' <repetitiveVariableOrFunctionCallStat_Function>");
            else
                return false;
        }
        else if (FIRST_RepetitiveIndicesStat.Contains(LookAhead.Type))
        {
            OutputDerivation("<statementAlt> -> <repetitiveIndicesStat>");
            if(RepetitiveIndicesStat())
                return OutputProductionRule("<statementAlt> -> <repetitiveIndicesStat>");
            else
                return false;
        }
        else
            return OutputError(FIRST_StatementAlt, FOLLOW_StatementAlt);
    } 

    /// <summary>
    /// StatementBlock production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StatementBlock() 
    {
        if(!SkipErrors(FIRST_StatementBlock, FOLLOW_StatementBlock))
            return false;
        if (Opencubr == LookAhead.Type)
        {
            OutputDerivation("<statementBlock> -> '{' <repetitiveStatements> '}'");
            if(Match(Opencubr) && RepetitiveStatements() && Match(Closecubr))
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
            return OutputError(FIRST_StatementBlock, FOLLOW_StatementBlock);
    } 

    /// <summary>
    /// StructDeclaration production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StructDeclaration() 
    {
        if(!SkipErrors(FIRST_StructDeclaration, FOLLOW_StructDeclaration))
            return false;
        if (Struct == LookAhead.Type)
        {
            OutputDerivation("<structDeclaration> -> 'struct' 'id' <structOptionalInheritance> '{' <repetitiveStructMemberDeclarations> '}' ';'");
            if(Match(Struct) && Match(Id) && StructOptionalInheritance() && Match(Opencubr) && RepetitiveStructMemberDeclarations() && Match(Closecubr) && Match(Semi))
                return OutputProductionRule("<structDeclaration> -> 'struct' 'id' <structOptionalInheritance> '{' <repetitiveStructMemberDeclarations> '}' ';'");
            else
                return false;
        }
        else
            return OutputError(FIRST_StructDeclaration, FOLLOW_StructDeclaration);
    } 

    /// <summary>
    /// StructOptionalInheritance production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StructOptionalInheritance() 
    {
        if(!SkipErrors(FIRST_StructOptionalInheritance, FOLLOW_StructOptionalInheritance))
            return false;
        if (Inherits == LookAhead.Type)
        {
            OutputDerivation("<structOptionalInheritance> -> 'inherits' 'id' <repetitiveStructOptionalInheritances>");
            if(Match(Inherits) && Match(Id) && RepetitiveStructOptionalInheritances())
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
            return OutputError(FIRST_StructOptionalInheritance, FOLLOW_StructOptionalInheritance);
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
            return OutputError(FIRST_StructOrImplOrFunction, FOLLOW_StructOrImplOrFunction);
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
            return OutputError(FIRST_Term, FOLLOW_Term);
    } 

    /// <summary>
    /// Type production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Type() 
    {
        if(!SkipErrors(FIRST_Type, FOLLOW_Type))
            return false;
        if (Integer == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'integer'");
            if(Match(Integer))
                return OutputProductionRule("<type> -> 'integer'");
            else
                return false;
        }
        else if (Float == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'float'");
            if(Match(Float))
                return OutputProductionRule("<type> -> 'float'");
            else
                return false;
        }
        else if (Id == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'id'");
            if(Match(Id))
                return OutputProductionRule("<type> -> 'id'");
            else
                return false;
        }
        else
            return OutputError(FIRST_Type, FOLLOW_Type);
    } 

    /// <summary>
    /// Variable production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Variable() 
    {
        if(!SkipErrors(FIRST_Variable, FOLLOW_Variable))
            return false;
        if (Id == LookAhead.Type)
        {
            OutputDerivation("<variable> -> 'id' <variableRest>");
            if(Match(Id) && VariableRest())
                return OutputProductionRule("<variable> -> 'id' <variableRest>");
            else
                return false;
        }
        else
            return OutputError(FIRST_Variable, FOLLOW_Variable);
    } 

    /// <summary>
    /// VariableDeclaration production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VariableDeclaration() 
    {
        if(!SkipErrors(FIRST_VariableDeclaration, FOLLOW_VariableDeclaration))
            return false;
        if (Let == LookAhead.Type)
        {
            OutputDerivation("<variableDeclaration> -> 'let' 'id' ':' <type> <repetitiveArraySizes> ';'");
            if(Match(Let) && Match(Id) && Match(Colon) && Type() && RepetitiveArraySizes() && Match(Semi))
                return OutputProductionRule("<variableDeclaration> -> 'let' 'id' ':' <type> <repetitiveArraySizes> ';'");
            else
                return false;
        }
        else
            return OutputError(FIRST_VariableDeclaration, FOLLOW_VariableDeclaration);
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
            return OutputError(FIRST_VariableDeclarationOrStatement, FOLLOW_VariableDeclarationOrStatement);
    } 

    /// <summary>
    /// VariableIdnest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VariableIdnest() 
    {
        if(!SkipErrors(FIRST_VariableIdnest, FOLLOW_VariableIdnest))
            return false;
        if (Dot == LookAhead.Type)
        {
            OutputDerivation("<variableIdnest> -> '.' 'id' <variableIdnestRest>");
            if(Match(Dot) && Match(Id) && VariableIdnestRest())
                return OutputProductionRule("<variableIdnest> -> '.' 'id' <variableIdnestRest>");
            else
                return false;
        }
        else
            return OutputError(FIRST_VariableIdnest, FOLLOW_VariableIdnest);
    } 

    /// <summary>
    /// VariableIdnestRest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VariableIdnestRest() 
    {
        if(!SkipErrors(FIRST_VariableIdnestRest, FOLLOW_VariableIdnestRest))
            return false;
        if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<variableIdnestRest> -> '(' <argumentParameters> ')' <variableIdnest>");
            if(Match(Openpar) && ArgumentParameters() && Match(Closepar) && VariableIdnest())
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
        else if (FOLLOW_VariableIdnestRest.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableIdnestRest> -> EPSILON");
            return OutputProductionRule("<variableIdnestRest> -> EPSILON");
        }
        else
            return OutputError(FIRST_VariableIdnestRest, FOLLOW_VariableIdnestRest);
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
        else if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<variableRest> -> '(' <argumentParameters> ')' <variableIdnest>");
            if(Match(Openpar) && ArgumentParameters() && Match(Closepar) && VariableIdnest())
                return OutputProductionRule("<variableRest> -> '(' <argumentParameters> ')' <variableIdnest>");
            else
                return false;
        }
        else if (FIRST_RepetitiveVariables.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableRest> -> <repetitiveVariables>");
            if(RepetitiveVariables())
                return OutputProductionRule("<variableRest> -> <repetitiveVariables>");
            else
                return false;
        }
        else if (FOLLOW_VariableRest.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableRest> -> EPSILON");
            return OutputProductionRule("<variableRest> -> EPSILON");
        }
        else
            return OutputError(FIRST_VariableRest, FOLLOW_VariableRest);
    } 

    /// <summary>
    /// Visibility production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Visibility() 
    {
        if(!SkipErrors(FIRST_Visibility, FOLLOW_Visibility))
            return false;
        if (Public == LookAhead.Type)
        {
            OutputDerivation("<visibility> -> 'public'");
            if(Match(Public))
                return OutputProductionRule("<visibility> -> 'public'");
            else
                return false;
        }
        else if (Private == LookAhead.Type)
        {
            OutputDerivation("<visibility> -> 'private'");
            if(Match(Private))
                return OutputProductionRule("<visibility> -> 'private'");
            else
                return false;
        }
        else
            return OutputError(FIRST_Visibility, FOLLOW_Visibility);
    }

    #endregion Productions
}
