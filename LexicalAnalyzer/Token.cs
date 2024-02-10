using System.Text.RegularExpressions;
using static System.Console;

namespace LexicalAnalyzer;

/// <summary>
/// Represents a token in the lexical analysis phase of a compiler.
/// </summary>
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

    /// <summary>
    /// Represents a regular expression pattern used for matching non-zero digits.
    /// </summary>
    [GeneratedRegex("^[1-9]$")]
    public static partial Regex NonZeroDigit();

    /// <summary>
    /// Represents a regular expression pattern used for matching digits.
    /// </summary>
    [GeneratedRegex("^[0-9]$")]
    public static partial Regex Digit();

    /// <summary>
    /// Represents a regular expression pattern used for matching letters.
    /// </summary>
    [GeneratedRegex("^[a-zA-Z]$")]
    public static partial Regex Letter();

    /// <summary>
    /// Represents a regular expression pattern used for matching alphanumeric characters.
    /// </summary>
    [GeneratedRegex("^([a-zA-Z]|[0-9]|_)$")]
    public static partial Regex Alphanumeric();

    /// <summary>
    /// Represents a regular expression pattern used for matching identifiers.
    /// </summary>
    [GeneratedRegex("^([a-zA-Z])([a-zA-Z]|[0-9]|_)*$")]
    public static partial Regex Identifier();

    /// <summary>
    /// Represents a regular expression pattern used for matching integers.
    /// </summary>
    [GeneratedRegex("^(([1-9][0-9]*)|0)$")]
    public static partial Regex Integer();

    /// <summary>
    /// Represents a regular expression pattern used for matching fractions.
    /// </summary>
    [GeneratedRegex("^((\\.[0-9]*[1-9])|(\\.0))$")]
    public static partial Regex Fraction();

    /// <summary>
    /// Represents a regular expression pattern used for matching floats.
    /// </summary>
    [GeneratedRegex("^(([1-9][0-9]*)|0)((\\.[0-9]*[1-9])|(\\.0))(e(\\+|-)??(([1-9][0-9]*)|0))??$")]
    public static partial Regex Float();

    /// <summary>
    /// Represents a regular expression pattern used for matching reserved words.
    /// </summary>
    [GeneratedRegex("^(if|then|else|integer|float|void|public|private|func|var|struct|while|read|write|return|self|inherits|let|impl)$")]
    public static partial Regex ReservedWord();

    /// <summary>
    /// Represents a regular expression pattern used for matching operators.
    /// </summary>
    [GeneratedRegex("^(==|=|<>|<|>|<=|>=|\\+|-|\\*|/|=|\\||&|!)$")]
    public static partial Regex Operator();

    /// <summary>
    /// Represents a regular expression pattern used for matching punctuation.
    /// </summary>
    [GeneratedRegex("^(\\(|\\)|{|}|\\[|\\]|;|,|\\.|:|->)$")]
    public static partial Regex Punctuation();

    /// <summary>
    /// Represents a regular expression pattern used for matching comments.
    /// </summary>
    [GeneratedRegex("^//.*$")]
    public static partial Regex Comment();

    /// <summary>
    /// Represents a regular expression pattern used for matching multiline comments.
    /// </summary>
    [GeneratedRegex("^/\\*(.|\\n)*$")]
    public static partial Regex MultilineCommentStart();

    /// <summary>
    /// Represents a regular expression pattern used for matching multiline comments.
    /// </summary>
    [GeneratedRegex(".*\\*/$")]
    public static partial Regex MultilineCommentEnd();

    /// <summary>
    /// Represents a regular expression pattern used for matching characters not in the valid character set.
    /// </summary>
    [GeneratedRegex("^[^a-zA-Z0-9=+\\-*/!><&|(){}\\[\\];,\\.:_]$")]
    public static partial Regex InvalidChar();

    #region Invalid Regular Expressions

    /// <summary>
    /// Represents a regular expression pattern used for matching invalid integers.
    /// </summary>
    [GeneratedRegex("^0+((([1-9][0-9]*)|0))$")]
    public static partial Regex InvalidInteger();

    /// <summary>
    /// Represents a regular expression pattern used for matching invalid floats.
    /// </summary>
    [GeneratedRegex("(^0+(([1-9][0-9]*)|0)((\\.[0-9]*[1-9])|(\\.0))(e(\\+|-)??(([1-9][0-9]*)|0))??$)|(^0*(([1-9][0-9]*)|0)((\\.[0-9]*[1-9])|(\\.0))(0+)(e(\\+|-)??(([1-9][0-9]*)|0))??$)|(^0*(([1-9][0-9]*)|0)((\\.[0-9]*[1-9])|(\\.0))(0*)(e(\\+|-)??(0+([1-9][0-9]*)|0))$)")]
    public static partial Regex InvalidFloat();

    /// <summary>
    /// Represents a regular expression pattern used for matching invalid identifiers.
    /// </summary>
    [GeneratedRegex("^([0-9]|_)([a-zA-Z]|[0-9]|_)*$")]
    public static partial Regex InvalidIdentifier();

    #endregion Invalid Regular Expressions

    #endregion Regular Expressions

    /// <summary>
    /// Returns a string representation of the Token object.
    /// </summary>
    /// <returns>A string in the format "[type, lexeme, location]".</returns>
    public override string ToString()
    {
        return string.Format("[{0}, {1}, {2}]", Type.ToString().ToLower(), Lexeme.ToString().Replace(Environment.NewLine, "\\n").Replace("\r",""), Location.ToString());
    }

    /// <summary>
    /// Generates an error message based on the token type, lexeme, and location.
    /// </summary>
    /// <returns>The error message.</returns>
    public string ShowError()
    {
        return Type switch
        {
            TokenType.Invalidchar => string.Format("Lexical error: Invalid character: \"{0}\": line {1}.\n", Lexeme, Location),
            TokenType.Invalidnum => string.Format("Lexical error: Invalid number: \"{0}\": line {1}.\n", Lexeme, Location),
            TokenType.Invalidid => string.Format("Lexical error: Invalid identifier: \"{0}\": line {1}.\n", Lexeme, Location),
            _ => ""
        };
    }

    /// <summary>
    /// Represents the type of a token.
    /// </summary>
    /// <param name="value">The string representation of the token.</param>
    /// <returns>The type of the token.</returns>
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
        else if(InvalidInteger().IsMatch(value))
        {
            return TokenType.Invalidnum;
        }
        else if(InvalidFloat().IsMatch(value))
        {
            return TokenType.Invalidnum;
        }   
        else if(InvalidIdentifier().IsMatch(value))
        {
            return TokenType.Invalidid;
        }
        else if(InvalidChar().IsMatch(value))
        {
            return TokenType.Invalidchar;
        }
        else
           return TokenType.Invalidchar;
    }

    /// <summary>
    /// Returns an array of regular expressions used for token matching.
    /// </summary>
    /// <returns>An array of regular expressions.</returns>
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
            InvalidInteger(),
            InvalidFloat(),
            InvalidIdentifier(),
            InvalidChar()
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
    Colon, Arrow,

    // Reserved Words
    If,Then,Else,Integer,Float,Void,
    Public,Private,Func,Var,Struct,While,
    Read,Write,Return,Self,Inherits,Let,Impl,

    // Comment
    Inlinecmt, Blockcmt,

    // Errors
    Invalidchar, Invalidnum, Invalidid,

    // End of File
    Eof
}