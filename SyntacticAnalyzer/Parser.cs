using LexicalAnalyzer;
using static System.Console;
using static LexicalAnalyzer.TokenType;
    
namespace SyntacticAnalyzer;

/// <summary>
/// Parser class, used to parse a source file.
/// </summary>
public class Parser : IParser
{
    /// <summary>
    /// The scanner used to scan and tokenize the source file
    /// </summary>
    private IScanner Scanner { get; }

    /// <summary>
    /// The current token being looked at
    /// </summary>
    private Token LookAhead { get; set; }

    /// <summary>
    /// The source file to parse (with the file extension)
    /// </summary>
    private string Source {get;} = "";

    /// <summary>
    /// The name of the source file to parse
    /// </summary>
    private string SourceName {get;} = "";
    
    /// <summary>
    /// The parse list used to track the derivations
    /// </summary>
    private IParseList ParseList { get; set; } = new ParseList();

    #region Constants

    /// <summary>
    /// Extension for the output syntax errors file
    /// </summary>
    private const string OUT_SYNTAX_ERRORS_EXTENSION = ".outsyntaxerrors";

    /// <summary>
    /// Extension for the output derivation file
    /// </summary>
    private const string OUT_DERIVATION_EXTENSION = ".outderivation";

    /// <summary>
    /// Extension for the output productions file
    /// </summary>
    private const string OUT_PRODUCTIONS_EXTENSION = ".outproductions";

    #endregion Constants

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

        // Output the derivation to the output file
        using StreamWriter sw = new(SourceName + OUT_DERIVATION_EXTENSION, true);
        sw.WriteLine(ParseList.GetDerivation());
        sw.Close();
        
        // Parse the source file   
        return Start() && Match(Eof);
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
            string[] expectedTokens = firstSet.Contains(Epsilon) ? 
            firstSet.Concat(followSet).Distinct().Where(x => x != Epsilon && x!=Eof).Select(Token.TokenTypeToString).ToArray() :
            firstSet.Distinct().Where(x => x != Epsilon && x!=Eof).Select(Token.TokenTypeToString).ToArray();

            // Define the error message
            string errorMsg = $"Syntax error: Unexpected token '{LookAhead.Lexeme}' at line {LookAhead.Location}. Expected any of the following: {string.Join(", ", expectedTokens)}.";

            // Write the error message to the console and to the output file
            WriteLine(errorMsg);
        
            using StreamWriter sw = new(SourceName + OUT_SYNTAX_ERRORS_EXTENSION, true);
            sw.WriteLine(errorMsg);

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
        // Add the production rule to the parse list
        ParseList.Add(productionRuleStr);

        // Write the derivation to the output file
        using StreamWriter sw = new(SourceName + OUT_DERIVATION_EXTENSION, true);
        sw.WriteLine(ParseList.GetDerivation());

        // Write the production rule to the output file
        using StreamWriter sw1 = new(SourceName + OUT_PRODUCTIONS_EXTENSION, true);
        sw1.WriteLine(productionRuleStr);
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
            return Match(Plus);
        }
        else if (Minus == LookAhead.Type)
        {
            OutputDerivation("<addOperator> -> '-'");
            return Match(Minus);
        }
        else if (Or == LookAhead.Type)
        {
            OutputDerivation("<addOperator> -> 'or'");
            return Match(Or);
        }
        else
            return false;
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
            return Expression() && RepetitiveArgumentParametersTail();
        }
        else if (FOLLOW_ArgumentParameters.Contains(LookAhead.Type))
        {
            OutputDerivation("<argumentParameters> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Match(Comma) && Expression();
        }
        else
            return false;
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
            return Term() && RecursiveArithmeticExpression();
        }
        else
            return false;
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
            return Match(Opensqbr) && ArraySizeContent();
        }
        else
            return false;
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
            return Match(Intnum) && Match(Closesqbr);
        }
        else if (Closesqbr == LookAhead.Type)
        {
            OutputDerivation("<arraySizeContent> -> ']'");
            return Match(Closesqbr);
        }
        else
            return false;
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
            return Match(Assign);
        }
        else
            return false;
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
            return ArithmeticExpression() && OptionalRelationalExpression();
        }
        else
            return false;
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
            return Match(Id) && FactorAlt() && RepetitiveVariableOrFunctionCall();
        }
        else if (Intnum == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'intLit'");
            return Match(Intnum);
        }
        else if (Floatnum == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'floatLit'");
            return Match(Floatnum);
        }
        else if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<factor> -> '(' <arithmeticExpression> ')'");
            return Match(Openpar) && ArithmeticExpression() && Match(Closepar);
        }
        else if (Not == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'not' <factor>");
            return Match(Not) && Factor();
        }
        else if (FIRST_Sign.Contains(LookAhead.Type))
        {
            OutputDerivation("<factor> -> <sign> <factor>");
            return Sign() && Factor();
        }
        else
            return false;
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
            return Match(Openpar) && ArgumentParameters() && Match(Closepar);
        }
        else if (FIRST_RepetitiveIndices.Contains(LookAhead.Type))
        {
            OutputDerivation("<factorAlt> -> <repetitiveIndices>");
            return RepetitiveIndices();
        }
        else if (FOLLOW_FactorAlt.Contains(LookAhead.Type))
        {
            OutputDerivation("<factorAlt> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Match(Opencubr) && RepetitiveVariableDeclarationOrStatements() && Match(Closecubr);
        }
        else
            return false;
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
            return FunctionHeader() && Match(Semi);
        }
        else
            return false;
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
            return FunctionHeader() && FunctionBody();
        }
        else
            return false;
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
            return Match(Func) && Match(Id) && Match(Openpar) && FuntionParameters() && Match(Closepar) && Match(Arrow) && ReturnType();
        }
        else
            return false;
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
            return Match(Comma) && Match(Id) && Match(Colon) && Type() && RepetitiveArraySizes();
        }
        else
            return false;
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
            return Match(Id) && Match(Colon) && Type() && RepetitiveArraySizes() && RepetitiveFunctionParametersTails();
        }
        else if (FOLLOW_FuntionParameters.Contains(LookAhead.Type))
        {
            OutputDerivation("<funtionParameters> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Match(Dot) && Match(Id) && IdnestRest();
        }
        else
            return false;
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
            return Match(Openpar) && ArgumentParameters() && Match(Closepar);
        }
        else if (FIRST_RepetitiveIndices.Contains(LookAhead.Type))
        {
            OutputDerivation("<idnestRest> -> <repetitiveIndices>");
            return RepetitiveIndices();
        }
        else if (FOLLOW_IdnestRest.Contains(LookAhead.Type))
        {
            OutputDerivation("<idnestRest> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Match(Openpar) && ArgumentParameters() && Match(Closepar) && RepetitiveVariableOrFunctionCallStat_Function();
        }
        else if (FIRST_RepetitiveIndicesStat.Contains(LookAhead.Type))
        {
            OutputDerivation("<idnestRestStat> -> <repetitiveIndicesStat>");
            return RepetitiveIndicesStat();
        }
        else
            return false;
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
            return Match(Dot) && Match(Id) && IdnestRestStat();
        }
        else
            return false;
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
            return Match(Impl) && Match(Id) && Match(Opencubr) && RepetitiveFunctionDefinitions() && Match(Closecubr);
        }
        else
            return false;
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
            return Match(Opensqbr) && ArithmeticExpression() && Match(Closesqbr);
        }
        else
            return false;
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
            return FunctionDeclaration();
        }
        else if (FIRST_VariableDeclaration.Contains(LookAhead.Type))
        {
            OutputDerivation("<memberDeclaration> -> <variableDeclaration>");
            return VariableDeclaration();
        }
        else
            return false;
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
            return Match(Mult);
        }
        else if (Div == LookAhead.Type)
        {
            OutputDerivation("<multOperator> -> '/'");
            return Match(Div);
        }
        else if (And == LookAhead.Type)
        {
            OutputDerivation("<multOperator> -> 'and'");
            return Match(And);
        }
        else
            return false;
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
            return RelationalOperator() && ArithmeticExpression();
        }
        else if (FOLLOW_OptionalRelationalExpression.Contains(LookAhead.Type))
        {
            OutputDerivation("<optionalRelationalExpression> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return AddOperator() && Term() && RecursiveArithmeticExpression();
        }
        else if (FOLLOW_RecursiveArithmeticExpression.Contains(LookAhead.Type))
        {
            OutputDerivation("<recursiveArithmeticExpression> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return MultOperator() && Factor() && RecursiveTerms();
        }
        else if (FOLLOW_RecursiveTerms.Contains(LookAhead.Type))
        {
            OutputDerivation("<recursiveTerms> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return ArithmeticExpression() && RelationalOperator() && ArithmeticExpression();
        }
        else
            return false;
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
            return Match(Eq);
        }
        else if (Noteq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'neq'");
            return Match(Noteq);
        }
        else if (Lt == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'lt'");
            return Match(Lt);
        }
        else if (Gt == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'gt'");
            return Match(Gt);
        }
        else if (Leq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'leq'");
            return Match(Leq);
        }
        else if (Geq == LookAhead.Type)
        {
            OutputDerivation("<relationalOperator> -> 'geq'");
            return Match(Geq);
        }
        else
            return false;
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
            return ArgumentParametersTail() && RepetitiveArgumentParametersTail();
        }
        else if (FOLLOW_RepetitiveArgumentParametersTail.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveArgumentParametersTail> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return ArraySize() && RepetitiveArraySizes();
        }
        else if (FOLLOW_RepetitiveArraySizes.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveArraySizes> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return FunctionDefinition() && RepetitiveFunctionDefinitions();
        }
        else if (FOLLOW_RepetitiveFunctionDefinitions.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveFunctionDefinitions> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return FunctionParametersTail() && RepetitiveFunctionParametersTails();
        }
        else if (FOLLOW_RepetitiveFunctionParametersTails.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveFunctionParametersTails> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Indice() && RepetitiveIndices();
        }
        else if (FOLLOW_RepetitiveIndices.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveIndices> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Indice() && RepetitiveIndicesStat();
        }
        else if (FIRST_RepetitiveVariableOrFunctionCallStat_Var.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveIndicesStat> -> <repetitiveVariableOrFunctionCallStat_Var>");
            return RepetitiveVariableOrFunctionCallStat_Var();
        }
        else
            return false;
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
            return Statement() && RepetitiveStatements();
        }
        else if (FOLLOW_RepetitiveStatements.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveStatements> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Visibility() && MemberDeclaration() && RepetitiveStructMemberDeclarations();
        }
        else if (FOLLOW_RepetitiveStructMemberDeclarations.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveStructMemberDeclarations> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Match(Comma) && Match(Id) && RepetitiveStructOptionalInheritances();
        }
        else if (FOLLOW_RepetitiveStructOptionalInheritances.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveStructOptionalInheritances> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return VariableDeclarationOrStatement() && RepetitiveVariableDeclarationOrStatements();
        }
        else if (FOLLOW_RepetitiveVariableDeclarationOrStatements.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableDeclarationOrStatements> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Idnest() && RepetitiveVariableOrFunctionCall();
        }
        else if (FOLLOW_RepetitiveVariableOrFunctionCall.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableOrFunctionCall> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return IdnestStat();
        }
        else if (FOLLOW_RepetitiveVariableOrFunctionCallStat_Function.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableOrFunctionCallStat_Function> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return IdnestStat();
        }
        else if (FIRST_AssignmentOperator.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariableOrFunctionCallStat_Var> -> <assignmentOperator> <expression>");
            return AssignmentOperator() && Expression();
        }
        else
            return false;
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
            return VariableIdnest() && RepetitiveVariables();
        }
        else if (FOLLOW_RepetitiveVariables.Contains(LookAhead.Type))
        {
            OutputDerivation("<repetitiveVariables> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Type();
        }
        else if (TokenType.Void == LookAhead.Type)
        {
            OutputDerivation("<returnType> -> 'void'");
            return Match(TokenType.Void);
        }
        else
            return false;
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
            return Match(Plus);
        }
        else if (Minus == LookAhead.Type)
        {
            OutputDerivation("<sign> -> '-'");
            return Match(Minus);
        }
        else
            return false;
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
            return StructOrImplOrFunction() && Start();
        }
        else if (FOLLOW_Start.Contains(LookAhead.Type))
        {
            OutputDerivation("<START> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Match(Id) && StatementAlt() && Match(Semi);
        }
        else if (If == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'if' '(' <relationalExpression> ')' 'then' <statementBlock> 'else' <statementBlock> ';'");
            return Match(If) && Match(Openpar) && RelationalExpression() && Match(Closepar) && Match(Then) && StatementBlock() && Match(Else) && StatementBlock() && Match(Semi);
        }
        else if (While == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'while' '(' <relationalExpression> ')' <statementBlock> ';'");
            return Match(While) && Match(Openpar) && RelationalExpression() && Match(Closepar) && StatementBlock() && Match(Semi);
        }
        else if (TokenType.Read == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'read' '(' <variable> ')' ';'");
            return Match(TokenType.Read) && Match(Openpar) && Variable() && Match(Closepar) && Match(Semi);
        }
        else if (TokenType.Write == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'write' '(' <expression> ')' ';'");
            return Match(TokenType.Write) && Match(Openpar) && Expression() && Match(Closepar) && Match(Semi);
        }
        else if (Return == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'return' '(' <expression> ')' ';'");
            return Match(Return) && Match(Openpar) && Expression() && Match(Closepar) && Match(Semi);
        }
        else
            return false;
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
            return Match(Openpar) && ArgumentParameters() && Match(Closepar) && RepetitiveVariableOrFunctionCallStat_Function();
        }
        else if (FIRST_RepetitiveIndicesStat.Contains(LookAhead.Type))
        {
            OutputDerivation("<statementAlt> -> <repetitiveIndicesStat>");
            return RepetitiveIndicesStat();
        }
        else
            return false;
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
            return Match(Opencubr) && RepetitiveStatements() && Match(Closecubr);
        }
        else if (FIRST_Statement.Contains(LookAhead.Type))
        {
            OutputDerivation("<statementBlock> -> <statement>");
            return Statement();
        }
        else if (FOLLOW_StatementBlock.Contains(LookAhead.Type))
        {
            OutputDerivation("<statementBlock> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Match(Struct) && Match(Id) && StructOptionalInheritance() && Match(Opencubr) && RepetitiveStructMemberDeclarations() && Match(Closecubr) && Match(Semi);
        }
        else
            return false;
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
            return Match(Inherits) && Match(Id) && RepetitiveStructOptionalInheritances();
        }
        else if (FOLLOW_StructOptionalInheritance.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOptionalInheritance> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return StructDeclaration();
        }
        else if (FIRST_ImplDefinition.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOrImplOrFunction> -> <implDefinition>");
            return ImplDefinition();
        }
        else if (FIRST_FunctionDefinition.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOrImplOrFunction> -> <functionDefinition>");
            return FunctionDefinition();
        }
        else
            return false;
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
            return Factor() && RecursiveTerms();
        }
        else
            return false;
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
            return Match(Integer);
        }
        else if (Float == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'float'");
            return Match(Float);
        }
        else if (Id == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'id'");
            return Match(Id);
        }
        else
            return false;
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
            return Match(Id) && VariableRest();
        }
        else
            return false;
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
            return Match(Let) && Match(Id) && Match(Colon) && Type() && RepetitiveArraySizes() && Match(Semi);
        }
        else
            return false;
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
            return VariableDeclaration();
        }
        else if (FIRST_Statement.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableDeclarationOrStatement> -> <statement>");
            return Statement();
        }
        else
            return false;
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
            return Match(Dot) && Match(Id) && VariableIdnestRest();
        }
        else
            return false;
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
            return Match(Openpar) && ArgumentParameters() && Match(Closepar) && VariableIdnest();
        }
        else if (FIRST_RepetitiveIndices.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableIdnestRest> -> <repetitiveIndices>");
            return RepetitiveIndices();
        }
        else if (FOLLOW_VariableIdnestRest.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableIdnestRest> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return RepetitiveIndices() && RepetitiveVariables();
        }
        else if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<variableRest> -> '(' <argumentParameters> ')' <variableIdnest>");
            return Match(Openpar) && ArgumentParameters() && Match(Closepar) && VariableIdnest();
        }
        else if (FIRST_RepetitiveVariables.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableRest> -> <repetitiveVariables>");
            return RepetitiveVariables();
        }
        else if (FOLLOW_VariableRest.Contains(LookAhead.Type))
        {
            OutputDerivation("<variableRest> -> EPSILON");
            return true;
        }
        else
            return false;
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
            return Match(Public);
        }
        else if (Private == LookAhead.Type)
        {
            OutputDerivation("<visibility> -> 'private'");
            return Match(Private);
        }
        else
            return false;
    }

    #endregion Productions
}
