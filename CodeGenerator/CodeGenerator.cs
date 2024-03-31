using AbstractSyntaxTreeGeneration;

namespace CodeGenerator;

public class CodeGenerator : ICodeGenerator
{

    private string SourceFileName{ get; set;}

    private IASTNode Root{ get; set;}

    public CodeGenerator(IASTNode root , string sourceFileName)
    {
        this.Root = root;
        SourceFileName = sourceFileName;
    }



    public void GenerateCode()
    {
        throw new System.NotImplementedException();
    }
}