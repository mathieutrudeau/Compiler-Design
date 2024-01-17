using System.Text.RegularExpressions;
using static System.Console;

namespace Scanner;

public partial class Token
{

    #region Token Instance Properties

    /// <summary>
    /// An element of the lexical definition of the language.
    /// </summary>
    public TokenType Type { get; set; }

    /// <summary>
    /// A sequence of characters in the source program that matches the pattern for a token and is identified by the lexical analyzer as an instance of that token.
    /// </summary>
    public string Lexeme { get; set; } = "";

    /// <summary>
    /// The line number in the source program where the token was found.
    /// </summary>
    public int Location { get; set; } = 0;

    #endregion Token Instance Properties

    #region Regular Expressions

    [GeneratedRegex("^[1-9]$")]
    public static partial Regex NonZeroDigit();

    [GeneratedRegex("^[0-9]$")]
    public static partial Regex Digit();

    [GeneratedRegex("^[a-zA-Z]$")]
    public static partial Regex Letter();

    [GeneratedRegex("^([a-zA-Z]|[0-9]|_)$")]
    public static partial Regex Alphanumeric();

    [GeneratedRegex("^([a-zA-Z])([a-zA-Z]|[0-9]|_)*$")]
    public static partial Regex Identifier();

    [GeneratedRegex("^([1-9][0-9]*)|0$")]
    public static partial Regex Integer();

    [GeneratedRegex("^(\\.[0-9]*[1-9])|(\\.0)$")]
    public static partial Regex Fraction();

    [GeneratedRegex("^(([1-9][0-9]*)|0)((\\.[0-9]*[1-9])|(\\.0))(e(\\+|-)??(([1-9][0-9]*)|0))??$")]
    public static partial Regex Float();

    [GeneratedRegex("^if|then|else|integer|float|void|public|private|func|var|struct|while|read|write|return|self|inherits|let|impl$")]
    public static partial Regex ReservedWord();

    [GeneratedRegex("^(==|=|<>|<|>|<=|>=|\\+|-|\\*|/|=|\\||&|!)$")]
    public static partial Regex Operator();

    [GeneratedRegex("^(\\(|\\)|{|}|\\[|\\]|;|,|\\.|:|->)$")]
    public static partial Regex Punctuation();

    [GeneratedRegex("^//.*$")]
    public static partial Regex Comment();

    [GeneratedRegex("^/\\*.*$")]
    public static partial Regex MultilineCommentStart();

    [GeneratedRegex(".*\\*/$")]
    public static partial Regex MultilineCommentEnd();

    #endregion Regular Expressions

    public override string ToString()
    {
        return string.Format("[{0}, {1}, {2}]", Type.ToString().ToLower(), Lexeme.ToString().Replace(Environment.NewLine, "\\n").Replace("\r",""), Location.ToString());
    }

    public static TokenType StringToTokenType(string value)
    {
        if(ReservedWord().IsMatch(value))
        {
            return value switch
            {
                "if" => TokenType.If,
                "then" => TokenType.Then,
                "else" => TokenType.Else,
                "integer" => TokenType.Integer,
                "float" => TokenType.Float,
                "void" => TokenType.Void,
                "public" => TokenType.Public,
                "private" => TokenType.Private,
                "func" => TokenType.Func,
                "var" => TokenType.Var,
                "struct" => TokenType.Struct,
                "while" => TokenType.While,
                "read" => TokenType.Read,
                "write" => TokenType.Write,
                "return" => TokenType.Return,
                "self" => TokenType.Self,
                "inherits" => TokenType.Inherits,
                "let" => TokenType.Let,
                "impl" => TokenType.Impl,
                _ => TokenType.Invalidid,
            };
        }
        else if(Punctuation().IsMatch(value))
        {
            //WriteLine("Punctuation: "+value);
            return value switch
            {
                "(" => TokenType.Openpar,
                ")" => TokenType.Closepar,
                "{" => TokenType.Opencubr,
                "}" => TokenType.Closecubr,
                "[" => TokenType.Opensqbr,
                "]" => TokenType.Closesqbr,
                ";" => TokenType.Semi,
                "," => TokenType.Comma,
                "." => TokenType.Dot,
                ":" => TokenType.Colon,
                "::" => TokenType.Coloncolon,
                "->" => TokenType.Arrow,
                _ => TokenType.Invalidchar,
            };
        }
        else if(Operator().IsMatch(value))
        {
            //WriteLine("Operator: "+value);
            return value switch
            {
                "==" => TokenType.Eq,
                "=" => TokenType.Assign,
                "<>" => TokenType.Noteq,
                "<" => TokenType.Lt,
                ">" => TokenType.Gt,
                "<=" => TokenType.Leq,
                ">=" => TokenType.Geq,
                "+" => TokenType.Plus,
                "-" => TokenType.Minus,
                "*" => TokenType.Mult,
                "/" => TokenType.Div,
                "|" => TokenType.Or,
                "&" => TokenType.And,
                "!" => TokenType.Not,
                _ => TokenType.Invalidchar,
            };
        }
        else if(Comment().IsMatch(value))
        {
            return TokenType.Inlinecmt;
        }
        else if(MultilineCommentStart().IsMatch(value)||MultilineCommentEnd().IsMatch(value))
        {
            return TokenType.Blockcmt;
        }
        else if(Float().IsMatch(value))
        {
            return TokenType.Floatnum;
        }
        else if(Integer().IsMatch(value))
        {
            return TokenType.Intnum;
        }
        else if(Identifier().IsMatch(value))
        {
            return TokenType.Id;
        }
        else if(Letter().IsMatch(value))
        {
            return TokenType.Letter;
        }
        else if(Digit().IsMatch(value))
        {
            return TokenType.Digit;
        }
        else if(NonZeroDigit().IsMatch(value))
        {
            return TokenType.Nonzerodigit;
        }
        else if(Fraction().IsMatch(value))
        {
            return TokenType.Fraction;
        }
        else if(Alphanumeric().IsMatch(value))
        {
            return TokenType.Alphanumeric;
        }
        else
           return TokenType.Invalidnum;
    }

    public static Regex[] Regexes()
    {
        return new Regex[] {
            NonZeroDigit(),
            Digit(),
            Letter(),
            Alphanumeric(),
            Identifier(),
            Integer(),
            Fraction(),
            Float(),
            ReservedWord(),
            Operator(),
            Punctuation(),
            Comment(),
            MultilineCommentStart(),
            MultilineCommentEnd()
        };
    }
}

/// <summary>
/// Represents the different types of tokens in the scanner.
/// </summary>
public enum TokenType
{
    // Atoms
    Id, Alphanumeric, Intnum, Floatnum,
    Fraction, Letter, Digit, Nonzerodigit,

    // Operators
    Eq, Noteq, Leq, Geq,
    Lt, Gt, Plus, Minus, Mult, Div, Assign,
    Or, And, Not,
    
    // Punctuation
    Openpar, Closepar, Opencubr, Closecubr,
    Opensqbr, Closesqbr, Comma, Dot, Semi,
    Colon, Arrow, Coloncolon,

    // Reserved Words
    If,Then,Else,Integer,Float,Void,
    Public,Private,Func,Var,Struct,While,
    Read,Write,Return,Self,Inherits,Let,Impl,

    // Comment
    Inlinecmt, Blockcmt,

    // Errors
    Invalidchar, Invalidnum, Invalidid
}

