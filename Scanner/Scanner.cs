using System.Text.RegularExpressions;

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

    [GeneratedRegex("^(.[0-9]*[1-9])|.0$")]
    private static partial Regex Fraction();



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
        throw new NotImplementedException();
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

    private bool IsFinalState()
    {
        throw new NotImplementedException();
    }


    private Token? CreateToken()
    {
        throw new NotImplementedException();

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


    public static void TestRegex()
    {
        Console.WriteLine("Testing Regex...");
        Console.WriteLine("NonZeroDigit: " + NonZeroDigit().ToString());
        Console.WriteLine("Digit: " + Digit().ToString());
        Console.WriteLine("Letter: " + Letter().ToString());
        Console.WriteLine("Alphanumeric: " + Alphanumeric().ToString());
        Console.WriteLine("Identifier: " + Identifier().ToString());
    }

}