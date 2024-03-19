using AbstractSyntaxTreeGeneration;
using LexicalAnalyzer;
using static System.Console;
using static LexicalAnalyzer.TokenType;
using static AbstractSyntaxTreeGeneration.SemanticOperation;
    
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
    /// The token that was looked at before the current token
    /// </summary>
    private Token LookBehind { get; set; }

    /// <summary>
    /// The name of the source file to parse
    /// </summary>
    private string SourceName {get;} = "";

    /// <summary>
    /// The parse list used to track the derivations
    /// </summary>
    private IParseList ParseList { get; set; } = new ParseList();

    /// <summary>
    /// The sementic stack used to track the AST nodes
    /// </summary>
    private ISemanticStack SemStack { get; set; } = new SemanticStack();

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

    /// <summary>
    /// Extension for the output AST file
    /// </summary>
    private const string OUT_AST_EXTENSION = ".ast.outast";

    /// <summary>
    /// Extension for the output DOT AST file
    /// </summary>
    private const string OUT_DOT_AST_EXTENSION = ".dot.outast";

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
        LookBehind = new Token();

        // Set the source file 
        SourceName = sourceFileName.Replace(".src", "");

        // Delete the output files if they exist
        if (File.Exists(SourceName + OUT_SYNTAX_ERRORS_EXTENSION))
            File.Delete(SourceName + OUT_SYNTAX_ERRORS_EXTENSION);
        if (File.Exists(SourceName + OUT_DERIVATION_EXTENSION))
            File.Delete(SourceName + OUT_DERIVATION_EXTENSION);
        if (File.Exists(SourceName + OUT_PRODUCTIONS_EXTENSION))
            File.Delete(SourceName + OUT_PRODUCTIONS_EXTENSION);
        if (File.Exists(SourceName + OUT_AST_EXTENSION))
            File.Delete(SourceName + OUT_AST_EXTENSION);
        if (File.Exists(SourceName + OUT_DOT_AST_EXTENSION))
            File.Delete(SourceName + OUT_DOT_AST_EXTENSION);
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
        bool res = Start();

        // Get the AST root node from the stack
        IASTNode root = SemStack.Peek();

        // Output the AST to the output file
        using StreamWriter sw1 = new(SourceName + OUT_AST_EXTENSION, true);
        sw1.WriteLine(root.ToString());
        sw1.Close();

        // Output the dot AST to the output file
        using StreamWriter sw2 = new(SourceName + OUT_DOT_AST_EXTENSION, true);
        sw2.WriteLine(root.DotASTString());
        sw2.Close();

        // Return the result
        return res;
    }

    public IASTNode GetAST_Root()
    {
        return SemStack.Peek();
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

        // Set the LookBehind token to the current token
        LookBehind = LookAhead;

        if (!isMatch)
        {
            string errorMsg = $"Syntax error: Unexpected '{LookAhead.Lexeme}' at line {LookAhead.Location}. Expected {Token.TokenTypeToString(tokenType)}.";
            WriteLine(errorMsg);
            using StreamWriter sw = new(SourceName + OUT_SYNTAX_ERRORS_EXTENSION, true);
            sw.WriteLine(errorMsg);
        }

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
    
    private static readonly TokenType[] FIRST_AddOp = new TokenType[] { Plus, Minus, Or }; 
    private static readonly TokenType[] FIRST_AParams = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus, Epsilon }; 
    private static readonly TokenType[] FIRST_AParamsTail = new TokenType[] { Comma }; 
    private static readonly TokenType[] FIRST_ArithExpr = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_ArraySize = new TokenType[] { Opensqbr }; 
    private static readonly TokenType[] FIRST_ArraySize2 = new TokenType[] { Intnum, Closesqbr }; 
    private static readonly TokenType[] FIRST_AssignOp = new TokenType[] { Assign }; 
    private static readonly TokenType[] FIRST_Expr = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_Expr2 = new TokenType[] { Eq, Noteq, Lt, Gt, Leq, Geq, Epsilon }; 
    private static readonly TokenType[] FIRST_Factor = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_Factor2 = new TokenType[] { Openpar, Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_FParams = new TokenType[] { Id, Epsilon }; 
    private static readonly TokenType[] FIRST_FParamsTail = new TokenType[] { Comma }; 
    private static readonly TokenType[] FIRST_FuncBody = new TokenType[] { Opencubr }; 
    private static readonly TokenType[] FIRST_FuncDecl = new TokenType[] { Func }; 
    private static readonly TokenType[] FIRST_FuncDef = new TokenType[] { Func }; 
    private static readonly TokenType[] FIRST_FuncHead = new TokenType[] { Func }; 
    private static readonly TokenType[] FIRST_IdNest = new TokenType[] { Dot }; 
    private static readonly TokenType[] FIRST_IdNest2 = new TokenType[] { Openpar, Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_ImplDef = new TokenType[] { Impl }; 
    private static readonly TokenType[] FIRST_Indice = new TokenType[] { Opensqbr }; 
    private static readonly TokenType[] FIRST_MemberDecl = new TokenType[] { Func, Let }; 
    private static readonly TokenType[] FIRST_MultOp = new TokenType[] { Mult, Div, And }; 
    private static readonly TokenType[] FIRST_Opt_structDecl2 = new TokenType[] { Inherits, Epsilon }; 
    private static readonly TokenType[] FIRST_Prog = new TokenType[] { Struct, Impl, Func, Epsilon }; 
    private static readonly TokenType[] FIRST_RelExpr = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_RelOp = new TokenType[] { Eq, Noteq, Lt, Gt, Leq, Geq }; 
    private static readonly TokenType[] FIRST_Rept_aParams1 = new TokenType[] { Comma, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_fParams3 = new TokenType[] { Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_fParams4 = new TokenType[] { Comma, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_fParamsTail4 = new TokenType[] { Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_funcBody1 = new TokenType[] { Let, Id, If, While, TokenType.Read, TokenType.Write, Return, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_idnest1 = new TokenType[] { Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_implDef3 = new TokenType[] { Func, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_opt_structDecl22 = new TokenType[] { Comma, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_prog0 = new TokenType[] { Struct, Impl, Func, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_statBlock1 = new TokenType[] { Id, If, While, TokenType.Read, TokenType.Write, Return, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_structDecl4 = new TokenType[] { Public, Private, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_varDecl4 = new TokenType[] { Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_variable = new TokenType[] { Dot, Epsilon }; 
    private static readonly TokenType[] FIRST_Rept_var_or_funcCall = new TokenType[] { Dot, Epsilon }; 
    private static readonly TokenType[] FIRST_ReturnType = new TokenType[] { Integer, Float, Id, TokenType.Void }; 
    private static readonly TokenType[] FIRST_Rightrec_arithExpr = new TokenType[] { Epsilon, Plus, Minus, Or }; 
    private static readonly TokenType[] FIRST_RightRecTerm = new TokenType[] { Epsilon, Mult, Div, And }; 
    private static readonly TokenType[] FIRST_Sign = new TokenType[] { Plus, Minus }; 
    private static readonly TokenType[] FIRST_Start = new TokenType[] { Struct, Impl, Func, Epsilon }; 
    private static readonly TokenType[] FIRST_StatBlock = new TokenType[] { Opencubr, Id, If, While, TokenType.Read, TokenType.Write, Return, Epsilon }; 
    private static readonly TokenType[] FIRST_Statement = new TokenType[] { Id, If, While, TokenType.Read, TokenType.Write, Return }; 
    private static readonly TokenType[] FIRST_Statement_Id_nest = new TokenType[] { Dot, Openpar, Opensqbr, Assign }; 
    private static readonly TokenType[] FIRST_Statement_Id_nest2 = new TokenType[] { Epsilon, Dot }; 
    private static readonly TokenType[] FIRST_Statement_Id_nest3 = new TokenType[] { Assign, Dot }; 
    private static readonly TokenType[] FIRST_StructDecl = new TokenType[] { Struct }; 
    private static readonly TokenType[] FIRST_StructOrImplOrfunc = new TokenType[] { Struct, Impl, Func }; 
    private static readonly TokenType[] FIRST_Term = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FIRST_Type = new TokenType[] { Integer, Float, Id }; 
    private static readonly TokenType[] FIRST_VarDecl = new TokenType[] { Let }; 
    private static readonly TokenType[] FIRST_VarDeclOrStat = new TokenType[] { Let, Id, If, While, TokenType.Read, TokenType.Write, Return }; 
    private static readonly TokenType[] FIRST_Variable = new TokenType[] { Id }; 
    private static readonly TokenType[] FIRST_Variable2 = new TokenType[] { Opensqbr, Epsilon, Openpar }; 
    private static readonly TokenType[] FIRST_Var_idNest = new TokenType[] { Dot }; 
    private static readonly TokenType[] FIRST_Var_idNest2 = new TokenType[] { Openpar, Opensqbr, Epsilon }; 
    private static readonly TokenType[] FIRST_Visibility = new TokenType[] { Public, Private };

    #endregion First Sets

    #region Follow Sets
    
    private static readonly TokenType[] FOLLOW_AddOp = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_AParams = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_AParamsTail = new TokenType[] { Comma, Closepar }; 
    private static readonly TokenType[] FOLLOW_ArithExpr = new TokenType[] { Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_ArraySize = new TokenType[] { Opensqbr, Comma, Closepar, Semi }; 
    private static readonly TokenType[] FOLLOW_ArraySize2 = new TokenType[] { Opensqbr, Comma, Closepar, Semi }; 
    private static readonly TokenType[] FOLLOW_AssignOp = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_Expr = new TokenType[] { Closepar, Semi, Comma }; 
    private static readonly TokenType[] FOLLOW_Expr2 = new TokenType[] { Closepar, Semi, Comma }; 
    private static readonly TokenType[] FOLLOW_Factor = new TokenType[] { Mult, Div, And, Plus, Minus, Or, Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_Factor2 = new TokenType[] { Dot, Mult, Div, And, Plus, Minus, Or, Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_FParams = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_FParamsTail = new TokenType[] { Comma, Closepar }; 
    private static readonly TokenType[] FOLLOW_FuncBody = new TokenType[] { Func, Closecubr, Struct, Impl, Eof }; 
    private static readonly TokenType[] FOLLOW_FuncDecl = new TokenType[] { Public, Private, Closecubr }; 
    private static readonly TokenType[] FOLLOW_FuncDef = new TokenType[] { Func, Closecubr, Struct, Impl, Eof }; 
    private static readonly TokenType[] FOLLOW_FuncHead = new TokenType[] { Opencubr, Semi }; 
    private static readonly TokenType[] FOLLOW_IdNest = new TokenType[] { Dot, Mult, Div, And, Plus, Minus, Or, Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_IdNest2 = new TokenType[] { Dot, Mult, Div, And, Plus, Minus, Or, Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_ImplDef = new TokenType[] { Struct, Impl, Func, Eof }; 
    private static readonly TokenType[] FOLLOW_Indice = new TokenType[] { Opensqbr, Dot, Mult, Div, And, Plus, Minus, Or, Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr, Assign }; 
    private static readonly TokenType[] FOLLOW_MemberDecl = new TokenType[] { Public, Private, Closecubr }; 
    private static readonly TokenType[] FOLLOW_MultOp = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_Opt_structDecl2 = new TokenType[] { Opencubr }; 
    private static readonly TokenType[] FOLLOW_Prog = new TokenType[] { Eof }; 
    private static readonly TokenType[] FOLLOW_RelExpr = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_RelOp = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_Rept_aParams1 = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_Rept_fParams3 = new TokenType[] { Comma, Closepar }; 
    private static readonly TokenType[] FOLLOW_Rept_fParams4 = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_Rept_fParamsTail4 = new TokenType[] { Comma, Closepar }; 
    private static readonly TokenType[] FOLLOW_Rept_funcBody1 = new TokenType[] { Closecubr }; 
    private static readonly TokenType[] FOLLOW_Rept_idnest1 = new TokenType[] { Dot, Mult, Div, And, Plus, Minus, Or, Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr, Assign }; 
    private static readonly TokenType[] FOLLOW_Rept_implDef3 = new TokenType[] { Closecubr }; 
    private static readonly TokenType[] FOLLOW_Rept_opt_structDecl22 = new TokenType[] { Opencubr }; 
    private static readonly TokenType[] FOLLOW_Rept_prog0 = new TokenType[] { Eof }; 
    private static readonly TokenType[] FOLLOW_Rept_statBlock1 = new TokenType[] { Closecubr }; 
    private static readonly TokenType[] FOLLOW_Rept_structDecl4 = new TokenType[] { Closecubr }; 
    private static readonly TokenType[] FOLLOW_Rept_varDecl4 = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_Rept_variable = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_Rept_var_or_funcCall = new TokenType[] { Mult, Div, And, Plus, Minus, Or, Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_ReturnType = new TokenType[] { Opencubr, Semi }; 
    private static readonly TokenType[] FOLLOW_Rightrec_arithExpr = new TokenType[] { Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_RightRecTerm = new TokenType[] { Plus, Minus, Or, Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_Sign = new TokenType[] { Id, Intnum, Floatnum, Openpar, Not, Plus, Minus }; 
    private static readonly TokenType[] FOLLOW_Start = new TokenType[] { Eof }; 
    private static readonly TokenType[] FOLLOW_StatBlock = new TokenType[] { Else, Semi }; 
    private static readonly TokenType[] FOLLOW_Statement = new TokenType[] { Let, Id, If, While, TokenType.Read, TokenType.Write, Return, Closecubr, Else, Semi }; 
    private static readonly TokenType[] FOLLOW_Statement_Id_nest = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_Statement_Id_nest2 = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_Statement_Id_nest3 = new TokenType[] { Semi }; 
    private static readonly TokenType[] FOLLOW_StructDecl = new TokenType[] { Struct, Impl, Func, Eof }; 
    private static readonly TokenType[] FOLLOW_StructOrImplOrfunc = new TokenType[] { Struct, Impl, Func, Eof }; 
    private static readonly TokenType[] FOLLOW_Term = new TokenType[] { Plus, Minus, Or, Eq, Noteq, Lt, Gt, Leq, Geq, Closepar, Semi, Comma, Closesqbr }; 
    private static readonly TokenType[] FOLLOW_Type = new TokenType[] { Opencubr, Semi, Opensqbr, Comma, Closepar }; 
    private static readonly TokenType[] FOLLOW_VarDecl = new TokenType[] { Let, Id, If, While, TokenType.Read, TokenType.Write, Return, Closecubr, Public, Private }; 
    private static readonly TokenType[] FOLLOW_VarDeclOrStat = new TokenType[] { Let, Id, If, While, TokenType.Read, TokenType.Write, Return, Closecubr }; 
    private static readonly TokenType[] FOLLOW_Variable = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_Variable2 = new TokenType[] { Closepar }; 
    private static readonly TokenType[] FOLLOW_Var_idNest = new TokenType[] { Closepar, Dot }; 
    private static readonly TokenType[] FOLLOW_Var_idNest2 = new TokenType[] { Closepar, Dot }; 
    private static readonly TokenType[] FOLLOW_Visibility = new TokenType[] { Func, Let };

    #endregion Follow Sets

    #region Productions
    
    /// <summary>
    /// AddOp production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool AddOp() 
    {
        if(!SkipErrors(FIRST_AddOp, FOLLOW_AddOp))
            return false;
        if (Plus == LookAhead.Type)
        {
            OutputDerivation("<addOp> -> '+'");
            return Match(Plus);
        }
        else if (Minus == LookAhead.Type)
        {
            OutputDerivation("<addOp> -> '-'");
            return Match(Minus);
        }
        else if (Or == LookAhead.Type)
        {
            OutputDerivation("<addOp> -> 'or'");
            return Match(Or);
        }
        else
            return false;
    } 

    /// <summary>
    /// AParams production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool AParams() 
    {
        if(!SkipErrors(FIRST_AParams, FOLLOW_AParams))
            return false;
        if (FIRST_Expr.Contains(LookAhead.Type))
        {
            OutputDerivation("<aParams> -> <expr> <rept-aParams1>");
            return Expr() && Rept_aParams1();
        }
        else if (FOLLOW_AParams.Contains(LookAhead.Type))
        {
            OutputDerivation("<aParams> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// AParamsTail production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool AParamsTail() 
    {
        if(!SkipErrors(FIRST_AParamsTail, FOLLOW_AParamsTail))
            return false;
        if (Comma == LookAhead.Type)
        {
            OutputDerivation("<aParamsTail> -> ',' <expr>");
            return Match(Comma) && Expr();
        }
        else
            return false;
    } 

    /// <summary>
    /// ArithExpr production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArithExpr() 
    {
        if(!SkipErrors(FIRST_ArithExpr, FOLLOW_ArithExpr))
            return false;
        if (FIRST_Term.Contains(LookAhead.Type))
        {
            OutputDerivation("<arithExpr> -> <term> <rightrec-arithExpr>");
            return Term() && Rightrec_arithExpr();
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
            OutputDerivation("<arraySize> -> '[' <arraySize2>");
            return Match(Opensqbr) && ArraySize2();
        }
        else
            return false;
    } 

    /// <summary>
    /// ArraySize2 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ArraySize2() 
    {
        if(!SkipErrors(FIRST_ArraySize2, FOLLOW_ArraySize2))
            return false;
        if (Intnum == LookAhead.Type)
        {
            OutputDerivation("<arraySize2> -> 'intlit' ']'");

            bool res = Match(Intnum);
            
            if(res)
                SemStack.PushNode(IntLit, LookBehind);
                        
            return res && Match(Closesqbr);
        }
        else if (Closesqbr == LookAhead.Type)
        {
            OutputDerivation("<arraySize2> -> ']'");

            SemStack.PushNode(SemanticOperation.ArrayIndex, null);

            return Match(Closesqbr);
        }
        else
            return false;
    } 

    /// <summary>
    /// AssignOp production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool AssignOp() 
    {
        if(!SkipErrors(FIRST_AssignOp, FOLLOW_AssignOp))
            return false;
        if (Assign == LookAhead.Type)
        {
            OutputDerivation("<assignOp> -> '='");
            return Match(Assign);
        }
        else
            return false;
    } 

    /// <summary>
    /// Expr production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Expr() 
    {
        if(!SkipErrors(FIRST_Expr, FOLLOW_Expr))
            return false;
        if (FIRST_ArithExpr.Contains(LookAhead.Type))
        {
            OutputDerivation("<expr> -> <arithExpr> <expr2>");
            return ArithExpr() && Expr2();
        }
        else
            return false;
    } 

    /// <summary>
    /// Expr2 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Expr2() 
    {
        if(!SkipErrors(FIRST_Expr2, FOLLOW_Expr2))
            return false;
        if (FIRST_RelOp.Contains(LookAhead.Type))
        {
            OutputDerivation("<expr2> -> <relOp> <arithExpr>");
            
            bool res = RelOp(); 
            
            if(res)
                SemStack.PushNode(SemanticOperation.RelOp, LookBehind);

            res = res && ArithExpr();

            if(res)
                SemStack.PushNextX(SemanticOperation.RelExpr, 3);

            return res;
        }
        else if (FOLLOW_Expr2.Contains(LookAhead.Type))
        {
            OutputDerivation("<expr2> -> EPSILON");
            return true;
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
            OutputDerivation("<factor> -> 'id' <factor2> <rept-var-or-funcCall>");

            bool res = Match(Id);

            if(res)
                SemStack.PushNode(Identifier, LookBehind);

            return res && Factor2() && Rept_var_or_funcCall();
        }
        else if (Intnum == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'intlit'");

            bool res = Match(Intnum);

            if(res)
                SemStack.PushNode(IntLit, LookBehind);

            return res;
        }
        else if (Floatnum == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'floatlit'");

            bool res = Match(Floatnum);

            if(res)
                SemStack.PushNode(FloatLit, LookBehind);

            return res;
        }
        else if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<factor> -> '(' <arithExpr> ')'");
            return Match(Openpar) && ArithExpr() && Match(Closepar);
        }
        else if (Not == LookAhead.Type)
        {
            OutputDerivation("<factor> -> 'not' <factor>");
            
            bool res = Match(Not) && Factor();

            if(res)
                SemStack.PushNextX(SemanticOperation.NotFactor, 1);

            return res;
        }
        else if (FIRST_Sign.Contains(LookAhead.Type))
        {
            OutputDerivation("<factor> -> <sign> <factor>");

            bool res = Sign();

            if(res)
                SemStack.PushNode(SemanticOperation.Sign, LookBehind); 
            
            res = res && Factor();

            if(res)
                SemStack.PushNextX(SemanticOperation.SignFactor, 2);

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// Factor2 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Factor2() 
    {
        if(!SkipErrors(FIRST_Factor2, FOLLOW_Factor2))
            return false;
        if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<factor2> -> '(' <aParams> ')'");

            SemStack.PushEmptyNode();

            bool res = Match(Openpar) && AParams() && Match(Closepar);

            if (res)
            {
                SemStack.PushUntilEmptyNode(AParamList);
                SemStack.PushNextX(SemanticOperation.FuncCall, 2);
            }

            return res;
        }
        else if (FIRST_Rept_idnest1.Contains(LookAhead.Type) || FOLLOW_Rept_idnest1.Contains(LookAhead.Type))
        {
            OutputDerivation("<factor2> -> <rept-idnest1>");
            
            SemStack.PushEmptyNode();

            bool res = Rept_idnest1();

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.IndexList);
                SemStack.PushNextX(SemanticOperation.DataMember, 2);
            }

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// FParams production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FParams() 
    {
        if(!SkipErrors(FIRST_FParams, FOLLOW_FParams))
            return false;
        if (Id == LookAhead.Type)
        {
            OutputDerivation("<fParams> -> 'id' ':' <type> <rept-fParams3> <rept-fParams4>");

            bool res = Match(Id);

            if (res)
                SemStack.PushNode(Identifier, LookBehind);

            res = res && Match(Colon) && Type();

            if (res)
                SemStack.PushEmptyNode();

            res = res && Rept_fParams3();

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.ArraySize);
                SemStack.PushNextX(SemanticOperation.FParam, 3);
            }

            return res && Rept_fParams4();
        }
        else if (FOLLOW_FParams.Contains(LookAhead.Type))
        {
            OutputDerivation("<fParams> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// FParamsTail production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FParamsTail() 
    {
        if(!SkipErrors(FIRST_FParamsTail, FOLLOW_FParamsTail))
            return false;
        if (Comma == LookAhead.Type)
        {
            OutputDerivation("<fParamsTail> -> ',' 'id' ':' <type> <rept-fParamsTail4>");

            bool res = Match(Comma) && Match(Id);

            if (res)
                SemStack.PushNode(Identifier, LookBehind);

            res = res && Match(Colon) && Type();

            if (res)
                SemStack.PushEmptyNode();

            res = res && Rept_fParamsTail4();

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.ArraySize);
                SemStack.PushNextX(SemanticOperation.FParam, 3);
            }

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// FuncBody production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FuncBody() 
    {
        if(!SkipErrors(FIRST_FuncBody, FOLLOW_FuncBody))
            return false;
        if (Opencubr == LookAhead.Type)
        {
            OutputDerivation("<funcBody> -> '{' <rept-funcBody1> '}'");



            bool res = Match(Opencubr);

            if (res)
                SemStack.PushEmptyNode();

            res = res && Rept_funcBody1();

            if (res)
                SemStack.PushUntilEmptyNode(VarDeclOrStatList);

            return res && Match(Closecubr);
        }
        else
            return false;
    } 

    /// <summary>
    /// FuncDecl production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FuncDecl() 
    {
        if(!SkipErrors(FIRST_FuncDecl, FOLLOW_FuncDecl))
            return false;
        if (FIRST_FuncHead.Contains(LookAhead.Type))
        {
            OutputDerivation("<funcDecl> -> <funcHead> ';'");
            return FuncHead() && Match(Semi);
        }
        else
            return false;
    } 

    /// <summary>
    /// FuncDef production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FuncDef() 
    {
        if(!SkipErrors(FIRST_FuncDef, FOLLOW_FuncDef))
            return false;
        if (FIRST_FuncHead.Contains(LookAhead.Type))
        {
            OutputDerivation("<funcDef> -> <funcHead> <funcBody>");
            
            bool res = FuncHead() && FuncBody();

            if(res)
                SemStack.PushNextX(SemanticOperation.FuncDef, 2);

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// FuncHead production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool FuncHead() 
    {
        if(!SkipErrors(FIRST_FuncHead, FOLLOW_FuncHead))
            return false;
        if (Func == LookAhead.Type)
        {
            OutputDerivation("<funcHead> -> 'func' 'id' '(' <fParams> ')' 'arrow' <returnType>");

            bool res = Match(Func) && Match(Id);

            if (res)
            {
                SemStack.PushNode(Identifier, LookBehind);
                SemStack.PushEmptyNode();
            }

            res =  res && Match(Openpar) && FParams() && Match(Closepar) && Match(Arrow);

            if (res)
                SemStack.PushUntilEmptyNode(FParamList);
            
            res = res && ReturnType();

            if (res)
                SemStack.PushNextX(SemanticOperation.FuncHead, 3);

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// IdNest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool IdNest() 
    {
        if(!SkipErrors(FIRST_IdNest, FOLLOW_IdNest))
            return false;
        if (Dot == LookAhead.Type)
        {
            OutputDerivation("<idNest> -> '.' 'id' <idNest2>");

            bool res = Match(Dot) && Match(Id);
            
            if(res)
                SemStack.PushNode(Identifier, LookBehind);
            
            return res && IdNest2();
        }
        else
            return false;
    } 

    /// <summary>
    /// IdNest2 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool IdNest2() 
    {
        if(!SkipErrors(FIRST_IdNest2, FOLLOW_IdNest2))
            return false;
        if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<idNest2> -> '(' <aParams> ')'");

            SemStack.PushEmptyNode();

            bool res = Match(Openpar) && AParams() && Match(Closepar);

            if (res)
            {
                SemStack.PushUntilEmptyNode(AParamList);
                SemStack.PushNextX(SemanticOperation.FuncCall, 2);
                SemStack.PushNextX(SemanticOperation.DotChain, 2);
            }

            return res;
        }
        else if (FIRST_Rept_idnest1.Contains(LookAhead.Type) || FOLLOW_Rept_idnest1.Contains(LookAhead.Type))
        {
            OutputDerivation("<idNest2> -> <rept-idnest1>");

            SemStack.PushEmptyNode();

            bool res = Rept_idnest1();

            if(res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.IndexList);
                SemStack.PushNextX(SemanticOperation.DataMember, 2);
                SemStack.PushNextX(SemanticOperation.DotChain, 2);
            }


            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// ImplDef production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool ImplDef() 
    {
        if(!SkipErrors(FIRST_ImplDef, FOLLOW_ImplDef))
            return false;
        if (Impl == LookAhead.Type)
        {
            OutputDerivation("<implDef> -> 'impl' 'id' '{' <rept-implDef3> '}'");

            bool res = Match(Impl) && Match(Id);

            if (res)
            {
                SemStack.PushNode(Identifier, LookBehind);
                SemStack.PushEmptyNode();
            }

            res = res && Match(Opencubr) && Rept_implDef3() && Match(Closecubr);

            if (res)
            {
                SemStack.PushUntilEmptyNode(FuncDefList);
                SemStack.PushNextX(SemanticOperation.ImplDef, 2);
            }

            return res;
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
            OutputDerivation("<indice> -> '[' <arithExpr> ']'");
            return Match(Opensqbr) && ArithExpr() && Match(Closesqbr);
        }
        else
            return false;
    } 

    /// <summary>
    /// MemberDecl production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool MemberDecl() 
    {
        if(!SkipErrors(FIRST_MemberDecl, FOLLOW_MemberDecl))
            return false;
        if (FIRST_FuncDecl.Contains(LookAhead.Type))
        {
            OutputDerivation("<memberDecl> -> <funcDecl>");
            return FuncDecl();
        }
        else if (FIRST_VarDecl.Contains(LookAhead.Type))
        {
            OutputDerivation("<memberDecl> -> <varDecl>");
            return VarDecl();
        }
        else
            return false;
    } 

    /// <summary>
    /// MultOp production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool MultOp() 
    {
        if(!SkipErrors(FIRST_MultOp, FOLLOW_MultOp))
            return false;
        if (Mult == LookAhead.Type)
        {
            OutputDerivation("<multOp> -> '*'");
            return Match(Mult);
        }
        else if (Div == LookAhead.Type)
        {
            OutputDerivation("<multOp> -> '/'");
            return Match(Div);
        }
        else if (And == LookAhead.Type)
        {
            OutputDerivation("<multOp> -> 'and'");
            return Match(And);
        }
        else
            return false;
    } 

    /// <summary>
    /// Opt_structDecl2 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Opt_structDecl2() 
    {
        if(!SkipErrors(FIRST_Opt_structDecl2, FOLLOW_Opt_structDecl2))
            return false;
        if (Inherits == LookAhead.Type)
        {
            OutputDerivation("<opt-structDecl2> -> 'inherits' 'id' <rept-opt-structDecl22>");
            
            bool res = Match(Inherits) && Match(Id);
            
            if(res)
                SemStack.PushNode(Identifier,LookBehind);
            
            return res && Rept_opt_structDecl22();

        }
        else if (FOLLOW_Opt_structDecl2.Contains(LookAhead.Type))
        {
            OutputDerivation("<opt-structDecl2> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Prog production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Prog() 
    {
        // Add an empty node to the stack
        SemStack.PushEmptyNode();

        if(!SkipErrors(FIRST_Prog, FOLLOW_Prog))
            return false;
        if (FIRST_Rept_prog0.Contains(LookAhead.Type) || FOLLOW_Rept_prog0.Contains(LookAhead.Type))
        {
            OutputDerivation("<prog> -> <rept-prog0>");
            
            // Call the production rule for Rept_prog0
            bool res = Rept_prog0();

            // If the production rule was matched, push the node to the stack
            if (res)
                SemStack.PushUntilEmptyNode(Program);
            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// RelExpr production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RelExpr() 
    {
        if(!SkipErrors(FIRST_RelExpr, FOLLOW_RelExpr))
            return false;
        if (FIRST_ArithExpr.Contains(LookAhead.Type))
        {
            OutputDerivation("<relExpr> -> <arithExpr> <relOp> <arithExpr>");

            bool res = ArithExpr() && RelOp();
            
            if (res)
                SemStack.PushNode(SemanticOperation.RelOp, LookBehind);

            res = res && ArithExpr();

            if (res)
                SemStack.PushNextX(SemanticOperation.RelExpr, 3);

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// RelOp production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RelOp() 
    {
        if(!SkipErrors(FIRST_RelOp, FOLLOW_RelOp))
            return false;
        if (Eq == LookAhead.Type)
        {
            OutputDerivation("<relOp> -> 'eq'");
            return Match(Eq);
        }
        else if (Noteq == LookAhead.Type)
        {
            OutputDerivation("<relOp> -> 'neq'");
            return Match(Noteq);
        }
        else if (Lt == LookAhead.Type)
        {
            OutputDerivation("<relOp> -> 'lt'");
            return Match(Lt);
        }
        else if (Gt == LookAhead.Type)
        {
            OutputDerivation("<relOp> -> 'gt'");
            return Match(Gt);
        }
        else if (Leq == LookAhead.Type)
        {
            OutputDerivation("<relOp> -> 'leq'");
            return Match(Leq);
        }
        else if (Geq == LookAhead.Type)
        {
            OutputDerivation("<relOp> -> 'geq'");
            return Match(Geq);
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_aParams1 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_aParams1() 
    {
        if(!SkipErrors(FIRST_Rept_aParams1, FOLLOW_Rept_aParams1))
            return false;
        if (FIRST_AParamsTail.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-aParams1> -> <aParamsTail> <rept-aParams1>");
            return AParamsTail() && Rept_aParams1();
        }
        else if (FOLLOW_Rept_aParams1.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-aParams1> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_fParams3 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_fParams3() 
    {
        if(!SkipErrors(FIRST_Rept_fParams3, FOLLOW_Rept_fParams3))
            return false;
        if (FIRST_ArraySize.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-fParams3> -> <arraySize> <rept-fParams3>");
            return ArraySize() && Rept_fParams3();
        }
        else if (FOLLOW_Rept_fParams3.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-fParams3> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_fParams4 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_fParams4() 
    {
        if(!SkipErrors(FIRST_Rept_fParams4, FOLLOW_Rept_fParams4))
            return false;
        if (FIRST_FParamsTail.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-fParams4> -> <fParamsTail> <rept-fParams4>");
            return FParamsTail() && Rept_fParams4();
        }
        else if (FOLLOW_Rept_fParams4.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-fParams4> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_fParamsTail4 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_fParamsTail4() 
    {
        if(!SkipErrors(FIRST_Rept_fParamsTail4, FOLLOW_Rept_fParamsTail4))
            return false;
        if (FIRST_ArraySize.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-fParamsTail4> -> <arraySize> <rept-fParamsTail4>");
            return ArraySize() && Rept_fParamsTail4();
        }
        else if (FOLLOW_Rept_fParamsTail4.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-fParamsTail4> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_funcBody1 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_funcBody1() 
    {
        if(!SkipErrors(FIRST_Rept_funcBody1, FOLLOW_Rept_funcBody1))
            return false;
        if (FIRST_VarDeclOrStat.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-funcBody1> -> <varDeclOrStat> <rept-funcBody1>");
            return VarDeclOrStat() && Rept_funcBody1();
        }
        else if (FOLLOW_Rept_funcBody1.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-funcBody1> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_idnest1 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_idnest1() 
    {
        if(!SkipErrors(FIRST_Rept_idnest1, FOLLOW_Rept_idnest1))
            return false;
        if (FIRST_Indice.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-idnest1> -> <indice> <rept-idnest1>");
            return Indice() && Rept_idnest1();
        }
        else if (FOLLOW_Rept_idnest1.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-idnest1> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_implDef3 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_implDef3() 
    {
        if(!SkipErrors(FIRST_Rept_implDef3, FOLLOW_Rept_implDef3))
            return false;
        if (FIRST_FuncDef.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-implDef3> -> <funcDef> <rept-implDef3>");
            return FuncDef() && Rept_implDef3();
        }
        else if (FOLLOW_Rept_implDef3.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-implDef3> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_opt_structDecl22 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_opt_structDecl22() 
    {
        if(!SkipErrors(FIRST_Rept_opt_structDecl22, FOLLOW_Rept_opt_structDecl22))
            return false;
        if (Comma == LookAhead.Type)
        {
            OutputDerivation("<rept-opt-structDecl22> -> ',' 'id' <rept-opt-structDecl22>");

            bool res = Match(Comma) && Match(Id);
            
            if(res)
                SemStack.PushNode(Identifier,LookBehind);
            
            return res && Rept_opt_structDecl22();
        }
        else if (FOLLOW_Rept_opt_structDecl22.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-opt-structDecl22> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_prog0 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_prog0() 
    {
        if(!SkipErrors(FIRST_Rept_prog0, FOLLOW_Rept_prog0))
            return false;
        if (FIRST_StructOrImplOrfunc.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-prog0> -> <structOrImplOrfunc> <rept-prog0>");
            return StructOrImplOrfunc() && Rept_prog0();
        }
        else if (FOLLOW_Rept_prog0.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-prog0> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_statBlock1 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_statBlock1() 
    {
        if(!SkipErrors(FIRST_Rept_statBlock1, FOLLOW_Rept_statBlock1))
            return false;
        if (FIRST_Statement.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-statBlock1> -> <statement> <rept-statBlock1>");
            return Statement() && Rept_statBlock1();
        }
        else if (FOLLOW_Rept_statBlock1.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-statBlock1> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_structDecl4 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_structDecl4() 
    {
        if(!SkipErrors(FIRST_Rept_structDecl4, FOLLOW_Rept_structDecl4))
            return false;
        if (FIRST_Visibility.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-structDecl4> -> <visibility> <memberDecl> <rept-structDecl4>");

            bool res = Visibility() && MemberDecl();

            if (res)
                SemStack.PushNextX(StructMember, 2);
            
            return res && Rept_structDecl4();
        }
        else if (FOLLOW_Rept_structDecl4.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-structDecl4> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_varDecl4 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_varDecl4() 
    {
        if(!SkipErrors(FIRST_Rept_varDecl4, FOLLOW_Rept_varDecl4))
            return false;
        if (FIRST_ArraySize.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-varDecl4> -> <arraySize> <rept-varDecl4>");
            return ArraySize() && Rept_varDecl4();
        }
        else if (FOLLOW_Rept_varDecl4.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-varDecl4> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_variable production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_variable() 
    {
        if(!SkipErrors(FIRST_Rept_variable, FOLLOW_Rept_variable))
            return false;
        if (FIRST_Var_idNest.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-variable> -> <var-idNest> <rept-variable>");
            return Var_idNest() && Rept_variable();
        }
        else if (FOLLOW_Rept_variable.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-variable> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rept_var_or_funcCall production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rept_var_or_funcCall() 
    {
        if(!SkipErrors(FIRST_Rept_var_or_funcCall, FOLLOW_Rept_var_or_funcCall))
            return false;
        if (FIRST_IdNest.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-var-or-funcCall> -> <idNest> <rept-var-or-funcCall>");
            return IdNest() && Rept_var_or_funcCall();
        }
        else if (FOLLOW_Rept_var_or_funcCall.Contains(LookAhead.Type))
        {
            OutputDerivation("<rept-var-or-funcCall> -> EPSILON");
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

            bool res = Match(TokenType.Void);

            if(res)
                SemStack.PushNode(SemanticOperation.Type,LookBehind);

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// Rightrec_arithExpr production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Rightrec_arithExpr() 
    {
        if(!SkipErrors(FIRST_Rightrec_arithExpr, FOLLOW_Rightrec_arithExpr))
            return false;
        if (FIRST_AddOp.Contains(LookAhead.Type))
        {
            OutputDerivation("<rightrec-arithExpr> -> <addOp> <term> <rightrec-arithExpr>");

            bool res = AddOp();

            if(res)
                SemStack.PushNode(SemanticOperation.AddOp, LookBehind);

            res = res && Term();
            
            if(res)
                SemStack.PushNextX(SemanticOperation.AddExpr, 3);
            
            return res && Rightrec_arithExpr();
        }
        else if (FOLLOW_Rightrec_arithExpr.Contains(LookAhead.Type))
        {
            OutputDerivation("<rightrec-arithExpr> -> EPSILON");
            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// RightRecTerm production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool RightRecTerm() 
    {
        if(!SkipErrors(FIRST_RightRecTerm, FOLLOW_RightRecTerm))
            return false;
        if (FIRST_MultOp.Contains(LookAhead.Type))
        {
            OutputDerivation("<rightRecTerm> -> <multOp> <factor> <rightRecTerm>");
            
            bool res = MultOp();
            
            if(res)
                SemStack.PushNode(SemanticOperation.MultOp, LookBehind);
            
            res = res && Factor();
            
            if(res)
                SemStack.PushNextX(SemanticOperation.MultExpr, 3);

            return res && RightRecTerm();
        }
        else if (FOLLOW_RightRecTerm.Contains(LookAhead.Type))
        {
            OutputDerivation("<rightRecTerm> -> EPSILON");
            return true;
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
        if (FIRST_Prog.Contains(LookAhead.Type) || FOLLOW_Prog.Contains(LookAhead.Type))
        {
            OutputDerivation("<START> -> <prog> 'eof'");
            return Prog() && Match(Eof);
        }
        else
            return false;
    } 

    /// <summary>
    /// StatBlock production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StatBlock() 
    {
        if(!SkipErrors(FIRST_StatBlock, FOLLOW_StatBlock))
            return false;
        if (Opencubr == LookAhead.Type)
        {
            OutputDerivation("<statBlock> -> '{' <rept-statBlock1> '}'");
            return Match(Opencubr) && Rept_statBlock1() && Match(Closecubr);
        }
        else if (FIRST_Statement.Contains(LookAhead.Type))
        {
            OutputDerivation("<statBlock> -> <statement>");
            return Statement();
        }
        else if (FOLLOW_StatBlock.Contains(LookAhead.Type))
        {
            OutputDerivation("<statBlock> -> EPSILON");
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
            OutputDerivation("<statement> -> 'id' <statement-Id-nest> ';'");

            bool res = Match(Id);

            if (res)
            {
                SemStack.PushNode(Identifier, LookBehind);
                SemStack.PushEmptyNode();
            }

            res =res&& Statement_Id_nest() && Match(Semi);

            return res;
        }
        else if (If == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'if' '(' <relExpr> ')' 'then' <statBlock> 'else' <statBlock> ';'");

            bool res = Match(If) && Match(Openpar) && RelExpr() && Match(Closepar) && Match(Then);

            if (res)
                SemStack.PushEmptyNode();

            res = res && StatBlock();

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.StatBlock);
                SemStack.PushEmptyNode();
            }

            res = res && Match(Else) && StatBlock() && Match(Semi);

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.StatBlock);
                SemStack.PushNextX(IfStat, 3);
            }

            return res;
        }
        else if (While == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'while' '(' <relExpr> ')' <statBlock> ';'");

            bool res = Match(While) && Match(Openpar) && RelExpr() && Match(Closepar); 
            
            if(res)
                SemStack.PushEmptyNode();

            res = res && StatBlock() && Match(Semi);

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.StatBlock);
                SemStack.PushNextX(WhileStat, 2);
            }

            return res;
        }
        else if (TokenType.Read == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'read' '(' <variable> ')' ';'");
        
            bool res = Match(TokenType.Read) && Match(Openpar) && Variable() && Match(Closepar) && Match(Semi);
        
            if(res)
                SemStack.PushNextX(ReadStat, 1);

            return res;
        }
        else if (TokenType.Write == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'write' '(' <expr> ')' ';'");
            
            bool res = Match(TokenType.Write) && Match(Openpar) && Expr() && Match(Closepar) && Match(Semi);
        
            if(res)
                SemStack.PushNextX(WriteStat, 1);

            return res;
        }
        else if (Return == LookAhead.Type)
        {
            OutputDerivation("<statement> -> 'return' '(' <expr> ')' ';'");

            bool res= Match(Return) && Match(Openpar) && Expr() && Match(Closepar) && Match(Semi);
        
            if(res)
                SemStack.PushNextX(ReturnStat, 1);

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// Statement_Id_nest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Statement_Id_nest() 
    {
        if(!SkipErrors(FIRST_Statement_Id_nest, FOLLOW_Statement_Id_nest))
            return false;
        if (Dot == LookAhead.Type)
        {
            OutputDerivation("<statement-Id-nest> -> '.' 'id' <statement-Id-nest>");
            
            SemStack.PushUntilEmptyNode(SemanticOperation.IndexList);
            SemStack.PushNextX(SemanticOperation.DataMember, 2);
            SemStack.PushIfXPlaceholder(DotChain, 2);

            bool res = Match(Dot);
            
            if (res)
                SemStack.PushPlaceholderNodeBeforeX(1);
            
            res = res && Match(Id);

            if (res)
            {
                SemStack.PushNode(Identifier, LookBehind);
                SemStack.PushEmptyNode();
            }

            return res && Statement_Id_nest();
        }
        else if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<statement-Id-nest> -> '(' <aParams> ')' <statement-Id-nest2>");
            


            bool res = Match(Openpar) && AParams() && Match(Closepar);

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.AParamList);
                SemStack.PushNextX(SemanticOperation.FuncCall, 2);
                SemStack.PushIfXPlaceholder(DotChain, 2);
            }

            return res && Statement_Id_nest2();
        }
        else if (FIRST_Indice.Contains(LookAhead.Type))
        {
            OutputDerivation("<statement-Id-nest> -> <indice> <rept-idnest1> <statement-Id-nest3>");


            bool res = Indice() && Rept_idnest1();

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.IndexList);
                SemStack.PushNextX(SemanticOperation.DataMember, 2);
                SemStack.PushIfXPlaceholder(DotChain, 2);
            }

            return res && Statement_Id_nest3();
        }
        else if (FIRST_AssignOp.Contains(LookAhead.Type))
        {
            OutputDerivation("<statement-Id-nest> -> <assignOp> <expr>");
            
            SemStack.PushUntilEmptyNode(SemanticOperation.IndexList);
            SemStack.PushNextX(SemanticOperation.DataMember, 2);
            SemStack.PushIfXPlaceholder(DotChain, 2);

            bool res = AssignOp() && Expr();

            if(res)
                SemStack.PushNextX(SemanticOperation.AssignStat, 2);

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// Statement_Id_nest2 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Statement_Id_nest2() 
    {
        if(!SkipErrors(FIRST_Statement_Id_nest2, FOLLOW_Statement_Id_nest2))
            return false;
        if (Dot == LookAhead.Type)
        {
            OutputDerivation("<statement-Id-nest2> -> '.' 'id' <statement-Id-nest>");

            bool res = Match(Dot);

            if (res)
                SemStack.PushPlaceholderNodeBeforeX(1);
            
            res = res && Match(Id);

            if (res)
            {
                SemStack.PushNode(Identifier, LookBehind);
                SemStack.PushEmptyNode();
            }

            return res && Statement_Id_nest();
        }
        else if (FOLLOW_Statement_Id_nest2.Contains(LookAhead.Type))
        {
            OutputDerivation("<statement-Id-nest2> -> EPSILON");

            SemStack.PushNextX(SemanticOperation.FuncCall, 1);

            return true;
        }
        else
            return false;
    } 

    /// <summary>
    /// Statement_Id_nest3 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Statement_Id_nest3() 
    {
        if(!SkipErrors(FIRST_Statement_Id_nest3, FOLLOW_Statement_Id_nest3))
            return false;
        if (FIRST_AssignOp.Contains(LookAhead.Type))
        {
            OutputDerivation("<statement-Id-nest3> -> <assignOp> <expr>");

            bool res = AssignOp() && Expr();

            if(res)
                SemStack.PushNextX(SemanticOperation.AssignStat, 2);

            return res;
        }
        else if (Dot == LookAhead.Type)
        {
            OutputDerivation("<statement-Id-nest3> -> '.' 'id' <statement-Id-nest>");

            bool res = Match(Dot);

            if (res)
                SemStack.PushPlaceholderNodeBeforeX(1);
            
            res = res && Match(Id);

            if (res)
            {
                SemStack.PushNode(Identifier, LookBehind);
                SemStack.PushEmptyNode();
            }

            return res && Statement_Id_nest();
        }
        else
            return false;
    } 

    /// <summary>
    /// StructDecl production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StructDecl() 
    {
        if(!SkipErrors(FIRST_StructDecl, FOLLOW_StructDecl))
            return false;
        if (Struct == LookAhead.Type)
        {
            OutputDerivation("<structDecl> -> 'struct' 'id' <opt-structDecl2> '{' <rept-structDecl4> '}' ';'");

            bool res = Match(Struct) && Match(Id);

            if (res)
            {
                SemStack.PushNode(Identifier, LookBehind);
                SemStack.PushEmptyNode();
            }
            else
                return false;

            res = res && Opt_structDecl2() && Match(Opencubr);
            
            if(res)
            {
                SemStack.PushUntilEmptyNode(StructInheritList);
                SemStack.PushEmptyNode();
            }
            else
                return false;

            
            res = res && Rept_structDecl4() && Match(Closecubr) && Match(Semi);

            if(res)
            {
                SemStack.PushUntilEmptyNode(StructMemberList);
            }


            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// StructOrImplOrfunc production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool StructOrImplOrfunc() 
    {
        if(!SkipErrors(FIRST_StructOrImplOrfunc, FOLLOW_StructOrImplOrfunc))
            return false;
        if (FIRST_StructDecl.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOrImplOrfunc> -> <structDecl>");

            // Call the production rule for StructDecl
            bool res = StructDecl();
            if (res)
                SemStack.PushNextX(SemanticOperation.StructDecl, 3);
            return res;
        }
        else if (FIRST_ImplDef.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOrImplOrfunc> -> <implDef>");
            return ImplDef();
        }
        else if (FIRST_FuncDef.Contains(LookAhead.Type))
        {
            OutputDerivation("<structOrImplOrfunc> -> <funcDef>");
            return FuncDef();
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
            OutputDerivation("<term> -> <factor> <rightRecTerm>");
            return Factor() && RightRecTerm();
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
            bool res = Match(Integer);

            if(res)
                SemStack.PushNode(SemanticOperation.Type,LookBehind);

            return res;
        }
        else if (Float == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'float'");

            bool res = Match(Float);

            if(res)
                SemStack.PushNode(SemanticOperation.Type, LookBehind);

            return res;
        }
        else if (Id == LookAhead.Type)
        {
            OutputDerivation("<type> -> 'id'");

            bool res = Match(Id);

            if(res)
                SemStack.PushNode(SemanticOperation.Type,LookBehind);

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// VarDecl production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VarDecl() 
    {
        if(!SkipErrors(FIRST_VarDecl, FOLLOW_VarDecl))
            return false;
        if (Let == LookAhead.Type)
        {
            OutputDerivation("<varDecl> -> 'let' 'id' ':' <type> <rept-varDecl4> ';'");

            bool res = Match(Let) && Match(Id);
            
            if(res)
                SemStack.PushNode(Identifier,LookBehind);

            res = res && Match(Colon) && Type();

            if(res)
                SemStack.PushEmptyNode();

            res = res && Rept_varDecl4() && Match(Semi);

            if(res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.ArraySize);
                SemStack.PushNextX(SemanticOperation.VarDecl, 3);
            }

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// VarDeclOrStat production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool VarDeclOrStat() 
    {
        if(!SkipErrors(FIRST_VarDeclOrStat, FOLLOW_VarDeclOrStat))
            return false;
        if (FIRST_VarDecl.Contains(LookAhead.Type))
        {
            OutputDerivation("<varDeclOrStat> -> <varDecl>");
            return VarDecl();
        }
        else if (FIRST_Statement.Contains(LookAhead.Type))
        {
            OutputDerivation("<varDeclOrStat> -> <statement>");
            return Statement();
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
            OutputDerivation("<variable> -> 'id' <variable2>");
            
            SemStack.PushEmptyNode();

            bool res = Match(Id);

            if(res)
                SemStack.PushNode(Identifier,LookBehind);

            res = res && Variable2();

            if(res)
                SemStack.PushUntilEmptyNode(SemanticOperation.Variable);

            return res;
        }
        else
            return false;
    } 

    /// <summary>
    /// Variable2 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Variable2() 
    {
        if(!SkipErrors(FIRST_Variable2.ToList().Union(FIRST_Rept_variable).ToArray(), FOLLOW_Variable2))
            return false;
        if (FIRST_Rept_idnest1.Contains(LookAhead.Type) || FOLLOW_Rept_idnest1.Contains(LookAhead.Type))
        {
            OutputDerivation("<variable2> -> <rept-idnest1> <rept-variable>");
            
            SemStack.PushEmptyNode();

            bool res = Rept_idnest1();

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.IndexList);
                SemStack.PushNextX(SemanticOperation.DataMember, 2);
            }
            
            return res && Rept_variable();
        }
        else if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<variable2> -> '(' <aParams> ')' <var-idNest>");

            SemStack.PushEmptyNode();

            bool res = Match(Openpar) && AParams() && Match(Closepar);

            if(res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.AParamList);
                SemStack.PushNextX(SemanticOperation.FuncCall, 2);
            }

            return res && Var_idNest();
        }
        else
            return false;
    } 

    /// <summary>
    /// Var_idNest production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Var_idNest() 
    {
        if(!SkipErrors(FIRST_Var_idNest, FOLLOW_Var_idNest))
            return false;
        if (Dot == LookAhead.Type)
        {
            OutputDerivation("<var-idNest> -> '.' 'id' <var-idNest2>");
            
            bool res = Match(Dot) && Match(Id);
            
            if(res)
                SemStack.PushNode(Identifier,LookBehind);

            return res && Var_idNest2();
        }
        else
            return false;
    } 

    /// <summary>
    /// Var_idNest2 production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool Var_idNest2() 
    {
        if(!SkipErrors(FIRST_Var_idNest2, FOLLOW_Var_idNest2))
            return false;
        if (Openpar == LookAhead.Type)
        {
            OutputDerivation("<var-idNest2> -> '(' <aParams> ')' <var-idNest>");

            SemStack.PushEmptyNode();

            bool res = Match(Openpar) && AParams() && Match(Closepar);
            
            if(res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.AParamList);
                SemStack.PushNextX(SemanticOperation.FuncCall, 2);
                SemStack.PushNextX(SemanticOperation.DotChain, 2);
            }
            
            return res && Var_idNest();
        }
        else if (FIRST_Rept_idnest1.Contains(LookAhead.Type) || FOLLOW_Rept_idnest1.Contains(LookAhead.Type))
        {
            OutputDerivation("<var-idNest2> -> <rept-idnest1>");

            SemStack.PushEmptyNode();

            bool res = Rept_idnest1();

            if (res)
            {
                SemStack.PushUntilEmptyNode(SemanticOperation.IndexList);
                SemStack.PushNextX(SemanticOperation.DataMember, 2);
                SemStack.PushNextX(SemanticOperation.DotChain, 2);
            }

            return res;
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

            bool res = Match(Public);

            if(res)
                SemStack.PushNode(SemanticOperation.Visibility,LookBehind);

            return res;
        }
        else if (Private == LookAhead.Type)
        {
            OutputDerivation("<visibility> -> 'private'");

            bool res = Match(Private);

            if(res)
                SemStack.PushNode(SemanticOperation.Visibility,LookBehind);

            return res;
        }
        else
            return false;
    }

    #endregion Productions
}
