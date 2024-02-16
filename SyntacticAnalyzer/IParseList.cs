namespace SyntacticAnalyzer;

/// <summary>
/// Represents the derivations obtained from applying a list of rules.
/// </summary>
public interface IParseList
{
    /// <summary>
    /// Applies a rule to the current derivation.
    /// </summary>
    public void Add(string Rule);

    /// <summary>
    /// Prints current derivation.
    /// </summary>
    public void Print();

    /// <summary>
    /// Returns the derivation of the list of rules as a string.
    /// </summary>
    /// <returns>The derivation of the list of rules as a string.</returns>
    public string GetDerivation();
}

/// <summary>
/// Represents a node in a parse list.
/// </summary>
public interface IParseNode
{

}