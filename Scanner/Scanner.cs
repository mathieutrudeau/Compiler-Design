namespace Scanner;

/// <summary>
/// Represents a scanner that tokenizes source code.
/// </summary>
public class Scanner : IScanner
{
    /// <summary>
    /// Gets or sets the source code to be scanned.
    /// </summary>
    public string Source { get; set; } = "";

    

    /// <summary>
    /// Initializes a new instance of the Scanner class with the specified source code.
    /// </summary>
    /// <param name="source">The source code to be scanned.</param>
    public Scanner(string source)
    {
        Source = source;
    }

    public Token NextToken()
    {
        throw new NotImplementedException();
    }

    private char NextChar()
    {
        throw new NotImplementedException();
    }

    private void BackupChar()
    {

    }

    private bool IsFinalState()
    {
        throw new NotImplementedException();
    }

    private Token CreateToken()
    {
        throw new NotImplementedException();
    }


}