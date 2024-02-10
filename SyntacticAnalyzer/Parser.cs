using System.Diagnostics;
using LexicalAnalyzer;
using static System.Console;
using static LexicalAnalyzer.TokenType;

namespace SyntacticAnalyzer;

public class Parser : IParser
{
    private IScanner Scanner { get; }
    private Token LookAhead { get; set; }



    #region First and Follow Sets

    // FIRST sets
    private static readonly HashSet<TokenType> FIRST_FunctionParameters = new() { Id };
    private static readonly HashSet<TokenType> FIRST_FunctionHeader = new() { Func };
    private static readonly HashSet<TokenType> FIRST_Type = new() { Integer,Float,Id};
    private static readonly HashSet<TokenType> FIRST_ReturnType = (HashSet<TokenType>)FIRST_Type.Union(new HashSet<TokenType> { TokenType.Void });
    private static readonly HashSet<TokenType> FIRST_RepetitiveStructInheritance = new() { Comma };
    private static readonly HashSet<TokenType> FIRST_MemberDeclaration = new() { };
    private static readonly HashSet<TokenType> FIRST_Visibility = new() { Public, Private};
    private static readonly HashSet<TokenType> FIRST_RepetitiveStructMemberDeclaration = FIRST_Visibility;
    private static readonly HashSet<TokenType> FIRST_OptionalStructInheritance = new() { Inherits };
    private static readonly HashSet<TokenType> FIRST_StructDeclaration = new() { Struct };
    private static readonly HashSet<TokenType> FIRST_ImplementationDefinition = new() { Impl };
    private static readonly HashSet<TokenType> FIRST_FunctionDefinition = new() { Func };
    private static readonly HashSet<TokenType> FIRST_StructOrImplOrFunc = (HashSet<TokenType>)FIRST_StructDeclaration.Union(FIRST_ImplementationDefinition).Union(FIRST_FunctionDefinition);
    private static readonly HashSet<TokenType> FIRST_Start = FIRST_StructOrImplOrFunc;



    // FOLLOW sets
    private static readonly HashSet<TokenType> FOLLOW_FunctionParameters = new() { Closepar };
    private static readonly HashSet<TokenType> FOLLOW_OptionalStructInheritance = new() { Opencubr };
    private static readonly HashSet<TokenType> FOLLOW_RepetitiveStructInheritance = FOLLOW_OptionalStructInheritance;
    private static readonly HashSet<TokenType> FOLLOW_RepetitiveStructMemberDeclaration = new() { Closecubr };
    private static readonly HashSet<TokenType> FOLLOW_Start = new() { Eof };


    #endregion First and Follow Sets




    public Parser(IScanner scanner)
    {
        Scanner = scanner;
        LookAhead = new Token();
    }

    public bool Parse()
    {
        LookAhead = Scanner.NextToken();
        
        return false;
    }

    public bool Match(TokenType tokenType)
    {
        // Check if the current token matches the expected token
        bool isMatch = LookAhead.Type == tokenType;
        
        // Get the next token
        LookAhead = Scanner.NextToken();
        
        // Return the result
        return isMatch;
    }

    #region Non-Terminals Production Rules

    /// <summary>
    /// Start non-terminal production rule 
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    private bool Start()
    {
        if (FIRST_Start.Contains(LookAhead.Type))
        {
            if (StructOrImplOrFunc() && Start())
            {
                WriteLine("Start -> StructOrImplOrFunc Start");
                return true;
            }
            else
                return false;
        }
        else if (FOLLOW_Start.Contains(LookAhead.Type))
        {
            WriteLine("Start -> EPSILON");
            return true;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }

    /// <summary>
    /// StructOrImplOrFunc non-terminal production rule
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    private bool StructOrImplOrFunc()
    {
        if (FIRST_StructDeclaration.Contains(LookAhead.Type))
        {
            if(StructDeclaration())
            {
                WriteLine("StructOrImplOrFunc -> StructDeclaration");
                return true;
            }
            return false;
        }
        else if (FIRST_ImplementationDefinition.Contains(LookAhead.Type))
        {
            if(ImplementationDefinition())
            {
                WriteLine("StructOrImplOrFunc -> ImplementationDefinition");
                return true;
            }
            return false;
        }
        else if (FIRST_FunctionDefinition.Contains(LookAhead.Type))
        {
            if(FunctionDefinition())
            {
                WriteLine("StructOrImplOrFunc -> FunctionDefinition");
                return true;
            }
            return false;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return true;
        }
    }


    #region Struct Production Rules

    /// <summary>
    /// StructDeclaration non-terminal production rule
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    private bool StructDeclaration()
    {
        if(FIRST_StructDeclaration.Contains(LookAhead.Type))
        {
            if(Match(Struct) && Match(Id) && OptionalStructInheritance() && Match(Opencubr) && RepetitiveStructMemberDeclaration() && Match(Closecubr) && Match(Semi))
            {
                WriteLine("StructDeclaration -> 'Struct' 'Id' OptionalStructInheritance '{' RepetitiveStructMemberDeclaration '}' ';'");
                return true;
            }
            else
                return false;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }

    /// <summary>
    /// OptionalStructInheritance non-terminal production rule
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    private bool OptionalStructInheritance()
    {
        if(FIRST_OptionalStructInheritance.Contains(LookAhead.Type))
        {
            if(Match(Inherits) && Match(Id) && RepetitiveStructInheritance())
            {
                WriteLine("OptionalStructInheritance -> 'Inherits' 'Id' RepetitiveStructInheritance");
                return true;
            }
            else
                return false;
        }
        else if(FOLLOW_OptionalStructInheritance.Contains(LookAhead.Type))
        {
            WriteLine("OptionalStructInheritance -> EPSILON");
            return true;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }

    /// <summary>
    /// RepetitiveStructMemberDeclaration non-terminal production rule
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    private bool RepetitiveStructMemberDeclaration()
    {
        if(FIRST_RepetitiveStructMemberDeclaration.Contains(LookAhead.Type))
        {
            if(Visibility() && MemberDeclaration() && RepetitiveStructMemberDeclaration())
            {
                WriteLine("RepetitiveStructMemberDeclaration -> Visibility MemberDeclaration RepetitiveStructMemberDeclaration");
                return true;
            }
            else
                return false;
        }
        else if(FOLLOW_RepetitiveStructMemberDeclaration.Contains(LookAhead.Type))
        {
            WriteLine("RepetitiveStructMemberDeclaration -> EPSILON");
            return true;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }

    /// <summary>
    /// RepetitiveStructInheritance non-terminal production rule
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    private bool RepetitiveStructInheritance()
    {
        if(FIRST_RepetitiveStructInheritance.Contains(LookAhead.Type))
        {
            if(Match(Comma) && Match(Id) && RepetitiveStructInheritance())
            {
                WriteLine("RepetitiveStructInheritance -> ',' 'Id' RepetitiveStructInheritance");
                return true;
            }
            else
                return false;
        }
        else if(FOLLOW_RepetitiveStructInheritance.Contains(LookAhead.Type))
        {
            WriteLine("RepetitiveStructInheritance -> EPSILON");
            return true;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }


    #endregion Struct Production Rules

    private bool ImplementationDefinition()
    {
        return true;
    }

    #region Function Production Rules

    private bool FunctionDefinition()
    {
        return true;
    }

    /// <summary>
    /// FunctionHeader non-terminal production rule
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    public bool FunctionHeader()
    {
        if(FIRST_FunctionHeader.Contains(LookAhead.Type))
        {
            if(Match(Func) && Match(Id) && Match(Openpar) && FunctionParameters() && Match(Closepar) && Match(Arrow) && ReturnType())
            {
                WriteLine("FunctionHeader -> 'Func' 'Id' '(' FunctionParameters ')' '->' ReturnType");
                return true;
            }
            else
                return false;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }

    public bool FunctionParameters()
    {
        if(FIRST_FunctionParameters.Contains(LookAhead.Type))
        {
            if(Match(Id) && Match(Colon) && Type() )
            {
                WriteLine("FunctionParameters -> 'Id' ':' Type");
                return true;
            }
            else
                return false;
        }
        else if(FOLLOW_FunctionParameters.Contains(LookAhead.Type))
        {
            WriteLine("FunctionParameters -> EPSILON");
            return true;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }

    #endregion Function Production Rules

    /// <summary>
    /// Visibility non-terminal production rule
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    private bool Visibility()
    {
        if(FIRST_FunctionDefinition.Contains(LookAhead.Type))
        {
            if(Match(Public))
            {
                WriteLine("Visibility -> 'Public'");
                return true;
            }
            else if(Match(Private))
            {
                WriteLine("Visibility -> 'Private'");
                return true;
            }
            return false;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }

    /// <summary>
    /// ReturnType non-terminal production rule
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    public bool ReturnType()
    {
        if(FIRST_Type.Contains(LookAhead.Type))
        {
            if(Type())
            {
                WriteLine("ReturnType -> Type");
                return true;
            }
            else if(Match(TokenType.Void))
            {
                WriteLine("ReturnType -> 'Void'");
                return true;
            }
            else
                return false;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }

    /// <summary>
    /// Type non-terminal production rule
    /// </summary>
    /// <returns>True if the production rule matches, false otherwise</returns>
    private bool Type()
    {
        if(FIRST_Type.Contains(LookAhead.Type))
        {
            if(Match(Integer))
            {
                WriteLine("Type -> 'Integer'");
                return true;
            }
            else if(Match(Float))
            {
                WriteLine("Type -> 'Float'");
                return true;
            }
            else if(Match(Id))
            {
                WriteLine("Type -> 'Id'");
                return true;
            }
            else
                return false;
        }
        else
        {
            // Handle the case when the current token type is not in the array
            return false;
        }
    }

    private bool MemberDeclaration()
    {
        return true;
    }


    #endregion Non-Terminals Production Rules

}