using System.Text;
using AbstractSyntaxTreeGeneration;
using SemanticAnalyzer;

namespace CodeGenerator;

public class CodeGenerator : ICodeGenerator
{

    private string SourceFileName{ get; set;}

    private IASTNode Root{ get; set;}

    private ISymbolTable SymbolTable{ get; set;}

    public CodeGenerator(IASTNode root, ISymbolTable symbolTable, string sourceFileName)
    {
        this.Root = root;
        this.SymbolTable = symbolTable;
        SourceFileName = sourceFileName;
    }



    public void GenerateCode()
    {
        StringBuilder code = new ();
        Root.GenerateCode(SymbolTable, code);

        string outputFileName = SourceFileName.Replace(".src", ".moon");
        File.WriteAllText(outputFileName, code.ToString());
    }
}