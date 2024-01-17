namespace Scanner;

public class Token
{
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


    public override string ToString()
    {
        return $"Type: {Type}, Lexeme: {Lexeme}, Location: {Location}";
    }

}

public enum TokenType
{
    // Atoms
    Identifier, Alphanumeric, Integer, Float,
    Fraction, Letter, Digit, NonZeroDigit,

    // Operators
    EqualEqual, NotEqual, LessEqual, GreaterEqual,
    Less, Greater, Plus, Minus, Star, ForwardSlash, Equal,
    Separator, Ampersand, Exclamation,
    
    // Punctuation
    LeftParen, RightParen, LeftBrace, RightBrace,
    LeftSquareBracket, RightSquareBracket, Comma, Dot, Semicolon,
    DotDot, RightPointer,

    // Reserved Words
    IfKeyword,ThenKeyword,ElseKeyword,IntegerKeyword,FloatKeyword,VoidKeyword,
    PublicKeyword,PrivateKeyword,FuncKeyword,VarKeyword,StructKeyword,WhileKeyword,
    ReadKeyword,WriteKeyword,ReturnKeyword,SelfKeyword,InheritsKeyword,LetKeyword,ImplKeyword
}

    