using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static System.Console;

namespace Scanner;

/// <summary>
/// Represents a scanner that tokenizes source code.
/// </summary>
public partial class Scanner : IScanner
{
    /// <summary>
    /// Gets or sets the source code to be scanned.
    /// </summary>
    public string Source { get; set; } = "";

    private List<char> Buffer { get; set; } = new List<char>();

    private int BufferIndex { get; set; } = 0;

    private int LineNumber { get; set; } = 1;


    #region Regular Expressions

    [GeneratedRegex("^[1-9]$")]
    private static partial Regex NonZeroDigit();

    [GeneratedRegex("^[0-9]$")]
    private static partial Regex Digit();

    [GeneratedRegex("^[a-zA-Z]$")]
    private static partial Regex Letter();

    [GeneratedRegex("^([a-zA-Z]|[0-9]|_)$")]
    private static partial Regex Alphanumeric();

    [GeneratedRegex("^([a-zA-Z])([a-zA-Z]|[0-9]|_)*$")]
    private static partial Regex Identifier();

    [GeneratedRegex("^([1-9][0-9]*)|0$")]
    private static partial Regex Integer();

    [GeneratedRegex("^((\\.)[0-9]*[1-9])|(\\.)0$")]
    private static partial Regex Fraction();

    [GeneratedRegex("^(([1-9][0-9]*)|0)(e(\\+|-)?(([1-9][0-9]*)|0))?$")]
    private static partial Regex Float();

    [GeneratedRegex("^if|then|else|integer|float|void|public|private|func|var|struct|while|read|write|return|self|inherits|let|impl$")]
    private static partial Regex ReservedWord();

    [GeneratedRegex("^==|=|<>|<|>|<=|>=|\\+|-|\\*|/|=|\\||&|!$")]
    private static partial Regex Operator();

    [GeneratedRegex("^\\(|\\)|{|}|\\[|\\]|;|,|\\.|:|->$")]
    private static partial Regex Punctuation();

    [GeneratedRegex("^//$")]
    private static partial Regex Comment();


    #endregion Regular Expressions


    private static readonly char[] Alphabet = {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i',
        'j', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
        't', 'u', 'v', 'x', 'z', 'w', 'y', 'k', 'A',
        'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
        'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
        'U', 'V', 'X', 'Z', 'W', 'Y', 'K', '0', '1',
        '2', '3', '4', '5', '6', '7', '8', '9', '=',
        '+', '-', '*', '/', '!', '>', '<', '&', '|',
        '(', ')', '{', '}', '[', ']', ';', ',', '.',
        ':','_'
        };

    private static readonly char[] SpecialCharacters = {
        '\n', '\r', '\t', '\0', ' '
        };

    /// <summary>
    /// Initializes a new instance of the Scanner class with the specified source code.
    /// </summary>
    /// <param name="source">The source code to be scanned.</param>
    public Scanner(string source)
    {
        try
        {
            Source = source;

            // Read the source code into the buffer.
            using StreamReader reader = new(Source);
            while (!reader.EndOfStream)
            {
                Buffer.Add((char)reader.Read());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public Token NextToken()
    {
        WriteLine(".............................................");
        WriteLine("Fetching Next Token");
        WriteLine(".............................................");

        char currentChar = char.MinValue;
        string currentLexeme = string.Empty;

        do
        {
            // Get the next character from the buffer.
            currentChar = NextChar();

            // Skip special characters if they are located at the start of the lexeme
            while(SpecialCharacters.Contains(currentChar) && currentLexeme == string.Empty)
            {
                currentChar = NextChar();
            }
            
            // Make sure the character is part of the alphabet.
            if (!Alphabet.Contains(currentChar) && !SpecialCharacters.Contains(currentChar))
            {
                // Maybe throw an error here
                WriteLine("Error: Invalid character");
            }

            
            // Add the character to the current lexeme if its not a special character.
            if (!SpecialCharacters.Contains(currentChar))
            {
                currentLexeme += currentChar;
                WriteLine("Current Char: " + currentChar);
            }

        }
        while (!SpecialCharacters.Contains(currentChar));

        WriteLine("Current Lexeme: " + currentLexeme);

        // Check if the current lexeme is a final state.
        if (IsFinalState(currentLexeme))
        {
            WriteLine("Final State");
        }


        return CreateToken(currentLexeme);


    }

    /// <summary>
    /// Retrieves the next character from the buffer.
    /// </summary>
    /// <returns>The next character in the buffer or the end of file character if the buffer is empty.</returns>
    private char NextChar()
    {
        // Check if the buffer is not empty.
        if (BufferIndex < Buffer.Count)
        {
            // Return the next character in the buffer.
            return Buffer[BufferIndex++];
        }
        else
        {
            // Return the end of file character.
            return '\0';
        }
    }

    /// <summary>
    /// Moves the buffer index one position back, effectively undoing the last character read.
    /// </summary>
    private void BackupChar()
    {
        // Check if the buffer is not empty.
        if (BufferIndex > 0)
        {
            // Backup the buffer index.
            BufferIndex--;
        }
    }

    private static bool IsFinalState(string currentLexeme)
    {
        MatchCollection matches = ReservedWord().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Operator().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Punctuation().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Identifier().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Integer().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Float().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Comment().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Letter().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Digit().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = NonZeroDigit().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Alphanumeric().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        matches = Fraction().Matches(currentLexeme);
        if (matches.Count > 0)
            return true;
        return false;    
    }


    private Token CreateToken(string currentLexeme)
    {
        return new Token()
        {
            Lexeme = currentLexeme,
            Type = TokenType.Identifier,
            Location = LineNumber
        };
    }

    

    /// <summary>
    /// Prints the contents of the buffer, replacing special characters with their escape sequences.
    /// </summary>
    public void PrintBuffer()
    {
        foreach (char c in Buffer)
        {
            switch(c)
            {
                case '\n':
                    Console.Write("\\n");
                    break;
                case '\r':
                    Console.Write("\\r");
                    break;
                case '\t':
                    Console.Write("\\t");
                    break;
                default:
                    Console.Write(c);
                    break;
            }
        }
    }


    /// <summary>
    /// Checks if there are any tokens left in the input stream.
    /// </summary>
    /// <returns>True if there are tokens left, false otherwise.</returns>
    public bool HasTokenLeft()
    {
        int count=1;
        char currentChar = NextChar();

        while(SpecialCharacters.Contains(currentChar))
        {
            currentChar = NextChar();
            count++;
            if(currentChar == '\0')
            {
                return false;
            }
        }

        for(int i=0; i<count; i++)
        {
            BackupChar();
        }
        return true;
    }
}