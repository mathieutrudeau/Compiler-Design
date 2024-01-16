namespace Scanner;

public class Token
{
    public TokenType Type { get; set; }
    public string Lexeme { get; set; } ="";
    public int Location { get; set; } = 0;
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

    