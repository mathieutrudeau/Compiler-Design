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
        return string.Format("[Type: {0}, Lexeme: {1}, Location: {2}]", Type.ToString(), Lexeme.ToString(), Location.ToString());
    }


    public static string ConvertTokenTypeToString(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Identifier => "Identifier",
            TokenType.Alphanumeric => "Alphanumeric",
            TokenType.Integer => "Integer",
            TokenType.Float => "Float",
            TokenType.Fraction => "Fraction",
            TokenType.Letter => "Letter",
            TokenType.Digit => "Digit",
            TokenType.NonZeroDigit => "NonZeroDigit",
            TokenType.EqualEqual => "EqualEqual",
            TokenType.NotEqual => "NotEqual",
            TokenType.LessEqual => "LessEqual",
            TokenType.GreaterEqual => "GreaterEqual",
            TokenType.Less => "Less",
            TokenType.Greater => "Greater",
            TokenType.Plus => "Plus",
            TokenType.Minus => "Minus",
            TokenType.Star => "Star",
            TokenType.ForwardSlash => "ForwardSlash",
            TokenType.Equal => "Equal",
            TokenType.Separator => "Separator",
            TokenType.Ampersand => "Ampersand",
            TokenType.Exclamation => "Exclamation",
            TokenType.LeftParen => "LeftParen",
            TokenType.RightParen => "RightParen",
            TokenType.LeftBrace => "LeftBrace",
            TokenType.RightBrace => "RightBrace",
            TokenType.LeftSquareBracket => "LeftSquareBracket",
            TokenType.RightSquareBracket => "RightSquareBracket",
            TokenType.Comma => "Comma",
            TokenType.Dot => "Dot",
            TokenType.Semicolon => "Semicolon",
            TokenType.DotDot => "DotDot",
            TokenType.RightPointer => "RightPointer",
            TokenType.IfKeyword => "IfKeyword",
            TokenType.ThenKeyword => "ThenKeyword",
            TokenType.ElseKeyword => "ElseKeyword",
            TokenType.IntegerKeyword => "IntegerKeyword",
            TokenType.FloatKeyword => "FloatKeyword",
            TokenType.VoidKeyword => "VoidKeyword",
            TokenType.PublicKeyword => "PublicKeyword",
            TokenType.PrivateKeyword => "PrivateKeyword",
            TokenType.FuncKeyword => "FuncKeyword",
            TokenType.VarKeyword => "VarKeyword",
            TokenType.StructKeyword => "StructKeyword",
            TokenType.WhileKeyword => "WhileKeyword",
            TokenType.ReadKeyword => "ReadKeyword",
            TokenType.WriteKeyword => "WriteKeyword",
            TokenType.ReturnKeyword => "ReturnKeyword",
            TokenType.SelfKeyword => "SelfKeyword",
            TokenType.InheritsKeyword => "InheritsKeyword",
            TokenType.LetKeyword => "LetKeyword",
            TokenType.ImplKeyword => "ImplKeyword",
            _ => "Error",
        };
    }

    public static TokenType ConvertStringToTokenType(string tokenTypeString)
    {
        if (Enum.TryParse(tokenTypeString, out TokenType tokenType))
        {
            return tokenType;
        }
        else
        {
            throw new ArgumentException($"Invalid token type string {tokenTypeString}");
        }
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

