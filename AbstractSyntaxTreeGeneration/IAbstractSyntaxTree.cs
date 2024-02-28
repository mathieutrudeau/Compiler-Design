namespace AbstractSyntaxTreeGeneration;

public interface IAST
{
    public IASTNode MakeNode();

    public IASTNode MakeSiblings();

    public IASTNode MakeFamily();    
}

public interface IASTNode
{
    
}