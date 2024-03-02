namespace AbstractSyntaxTreeGeneration;


public class Symbol
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Id";
    public int Dimension { get; set; }
    public object? Value { get; set; } = new object();
    public int Scope { get; set; }
    public int LineOfDeclaration { get; set; }
    public List<int> LinesOfUsage { get; set; } = new List<int>();
}