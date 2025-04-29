using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeInspector;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a path to the source code directory.");
            return;
        }

        string path = args[0];
        
        using var provider = new SourceCodeQueryProvider(path);

        var namespaceDeclarationQuery = new SourceCodeQuery<FileScopedNamespaceDeclarationSyntax>(provider)
            .Where(c => c.Name.ToString().Contains("Inspector"))
            .OrderBy(x => x.Name)
            .Distinct();

        foreach (var namespaceDeclaration in namespaceDeclarationQuery)
        {
            Console.WriteLine($"{namespaceDeclaration.Name}");
        }

        var classDeclarationQuery = from c in new SourceCodeQuery<ClassDeclarationSyntax>(provider)
              where c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))
              orderby c.Identifier.Text
              select new
              {
                  ClassName = c.Identifier.Text,
                  MethodCount = c.Members.OfType<MethodDeclarationSyntax>().Count()
              };

        foreach (var classDeclaration in classDeclarationQuery)
        {
            Console.WriteLine($"{classDeclaration}");
        }
    }
}
