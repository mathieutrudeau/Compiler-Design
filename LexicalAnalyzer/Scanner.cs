using System.Text.RegularExpressions;
using static LexicalAnalyzer.Token;

namespace LexicalAnalyzer;

/// <summary>
/// Represents a scanner that tokenizes source code.
/// </summary>
public partial class Scanner : IScanner
{

    #region Properties

    /// <summary>
    /// Gets or sets the source code to be scanned.
    /// </summary>
    public string Source { get; set; } = "";

    private List<char> Buffer { get; set; } = new List<char>();

    private int BufferIndex { get; set; } = 0;

    private int LineNumber { get; set; } = 1;
    
    private int LexemeStartLineNumber { get; set; } = 1;

    private const string OUTLEX_TOKENS_EXTENSION = ".outlextokens";
    private const string OUTLEX_ERRORS_EXTENSION = ".outlexerrors";

    private string SourceName {get;set;} = "";

    #endregion Properties


    #region Regular Expressions

    [GeneratedRegex("/\\*")]
    private static partial Regex MultilineCommentStartRegex();

    [GeneratedRegex("\\*/")]
    private static partial Regex MultilineCommentEndRegex();

    #endregion Regular Expressions


    #region Constants

    /// <summary>
    /// Array of special characters used in the scanner.
    /// </summary>
    private static readonly char[] SpecialCharacters = {
        '\n', '\r', '\t', '\0', ' '
        };

    #endregion Constants

    /// <summary>
    /// Initializes a new instance of the Scanner class with the specified source code.
    /// </summary>
    /// <param name="source">The source code to be scanned.</param>
    public Scanner(string source)
    {
        try
        {
            // Set the source code.
            Source = source;

            SourceName = source.Replace(".src", "");

            // Delete the output files if they exist.
            if (File.Exists(SourceName + OUTLEX_TOKENS_EXTENSION))
                File.Delete(SourceName + OUTLEX_TOKENS_EXTENSION);  
            if (File.Exists(SourceName + OUTLEX_ERRORS_EXTENSION))
                File.Delete(SourceName + OUTLEX_ERRORS_EXTENSION);

            // Read the source code into the buffer.
            using StreamReader reader = new(Source);
            while (!reader.EndOfStream)
                Buffer.Add((char)reader.Read());
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    #region Public Methods

    /// <summary>
    /// Returns the next token from the source code.
    /// </summary>
    /// <returns>The next token from the source code.</returns>
    /// <exception cref="Exception">Thrown when no more tokens are available.</exception>
    public Token NextToken()
    {
        // If there are no tokens left, return an end of file token.
        if (!HasTokenLeft())
            return new Token { Type = TokenType.Eof, Lexeme = "End of File", Location = LineNumber};

        // The current lexeme being read.
        string currentLexeme = string.Empty;

        // The current character being read.
        char currentChar;

        // Read the next character until a special character is reached.
        // This loop might capture multiple tokens if they are not separated by special characters.
        do
        {
            // Get the next character from the buffer.
            currentChar = NextChar();

            // Skip special characters if they are located at the start of the lexeme
            while (SpecialCharacters.Contains(currentChar) && currentLexeme == string.Empty)
                currentChar = NextChar();

            // Add the character to the current lexeme if its not a special character.
            if (!SpecialCharacters.Contains(currentChar))
            {
                LexemeStartLineNumber = LineNumber;
                currentLexeme += currentChar;
            }
        }
        while (!SpecialCharacters.Contains(currentChar));

        // It is possible that no special characters and/or spaces are used to separate tokens in the source code.
        // In this case, we need to backtrack until we find the first token that reaches a final state.
        if (!IsFinalState(currentLexeme))
        {


            // Save the current lexeme length.
            int lexemeCount = currentLexeme.Length;

            // Backup the buffer index to not lose the current character. if the end of the file is reached
            if(currentChar != '\0')
                BackupChar();

            // Loop until only one character is left in the current lexeme or the current lexeme reaches a final state.
            do
            {
                // Remove the last character from the current lexeme.
                currentLexeme = currentLexeme[..^1];
            }
            while (!IsFinalState(currentLexeme) && currentLexeme.Length > 1);

            // Restore the buffer index to the position after the current lexeme.
            for (int i = 0; i < (lexemeCount - currentLexeme.Length); i++)
               BackupChar();
        }

        // Check if the current lexeme is a final state.
        if (IsFinalState(currentLexeme))
        {
            // Check if the current lexeme is a line comment.
            if (Comment().IsMatch(currentLexeme))
            {
                // Skip the rest of the line. Until either a new line character or the end of the file is reached.
                while (currentChar != '\n' && currentChar != '\0')
                {
                    currentLexeme += currentChar;
                    currentChar = NextChar();
                }
            }

            // Check if the current lexeme is a multiline comment.
            if (MultilineCommentStart().IsMatch(currentLexeme))
            {
                // Skip the rest of the multiline comment.
                while (!MultilineCommentEnd().IsMatch(currentLexeme) || !IsMultilineCommentDone(currentLexeme))
                {
                    currentLexeme += currentChar;
                    currentChar = NextChar();

                    // If the end of the file is reached, break out of the loop. 
                    // The current lexeme will be returned as a multiline comment token.
                    if (currentChar == '\0' && !IsMultilineCommentDone(currentLexeme))
                        break;
                }

                // Unless the end of the file is reached, backup the buffer index.
                if (currentChar != '\0')
                    BackupChar();
            }
        }

        // Return the current lexeme as a token.
        return CreateToken(currentLexeme);
    }

    /// <summary>
    /// Prints the contents of the buffer, replacing special characters with their escape sequences.
    /// </summary>
    public void PrintBuffer()
    {
        foreach (char c in Buffer)
            switch (c)
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

    /// <summary>
    /// Checks if there are any tokens left in the input stream.
    /// </summary>
    /// <returns>True if there are tokens left, false otherwise.</returns>
    public bool HasTokenLeft()
    {
        // Skip all special characters until we find a non-special character.
        int count = 1;
        char currentChar = NextChar();

        // Skip over all future special characters.
        while (SpecialCharacters.Contains(currentChar))
        {
            currentChar = NextChar();
            count++;

            // If we reach the end of the file, return false.
            if (currentChar == '\0')
                return false;
        }

        // Backup the buffer index, so we don't lose the character(s) we just read.
        for (int i = 0; i < count; i++)
            BackupChar();

        // If we reach this point, there are still tokens left.
        return true;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Retrieves the next character from the buffer.
    /// </summary>
    /// <returns>The next character in the buffer or the end of file character if the buffer is empty.</returns>
    private char NextChar()
    {
        // Check if the buffer is not empty.
        if (BufferIndex < Buffer.Count)
        {
            // Check if the current character is a new line character.
            if (Buffer[BufferIndex] == '\n')
                LineNumber++;
            // Return the next character in the buffer.
            return Buffer[BufferIndex++];
        }
        // Return the end of file character if the buffer is empty.
        else
            return '\0';
    }

    /// <summary>
    /// Moves the buffer index one position back, effectively undoing the last character read.
    /// </summary>
    private void BackupChar()
    {
        // Check if the buffer is not empty.
        if (BufferIndex > 0)
        {
            // Check if the current character is a new line character.
            if (Buffer[BufferIndex - 1] == '\n')
                // Decrement the line number.
                LineNumber--;
            // Backup the buffer index.
            BufferIndex--;
        }
    }

    /// <summary>
    /// Checks if the current lexeme reaches a final state for any of the regular expressions.
    /// </summary>
    /// <param name="currentLexeme">The current lexeme to be checked.</param>
    /// <returns>True if the current lexeme reaches a final state for any of the regular expressions, false otherwise.</returns>
    private static bool IsFinalState(string currentLexeme)
    {
        // Check if the current lexeme reaches a final state for any of the regular expressions.
        foreach (Regex regex in Regexes())
            if (regex.IsMatch(currentLexeme))
                return true;
        return false;
    }

    /// <summary>
    /// Creates a token from the current lexeme.
    /// </summary>
    /// <param name="currentLexeme">The current lexeme to be tokenized.</param>
    /// <returns>A token representing the current lexeme.</returns>
    private Token CreateToken(string currentLexeme)
    {
        // Create a token from the current lexeme.
        Token token = new()
        {
            Lexeme = currentLexeme,
            Type = StringToTokenType(currentLexeme),
            Location = LexemeStartLineNumber
        };

        // Write the token to the output file.
        using StreamWriter sw = new(SourceName + OUTLEX_TOKENS_EXTENSION, true);
        sw.WriteLine(token.ToString());

        // Write the token to the error file if it is invalid.
        if (token.Type.ToString().Contains("Invalid"))
        {
            using StreamWriter sw1 = new(SourceName + OUTLEX_ERRORS_EXTENSION, true);
            sw1.WriteLine(token.ShowError());
        }
        
        // Return the token.
        return token;
    }

    /// <summary>
    /// Checks if a multiline comment is complete by comparing the number of start and end comment tags.
    /// </summary>
    /// <param name="currentLexeme">The current lexeme being checked.</param>
    /// <returns>True if the multiline comment is complete, otherwise false.</returns>
    private static bool IsMultilineCommentDone(string currentLexeme)
    {
        return MultilineCommentStartRegex().Matches(currentLexeme).Count == MultilineCommentEndRegex().Matches(currentLexeme).Count;
    }

    #endregion Private Methods
}