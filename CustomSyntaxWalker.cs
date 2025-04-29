using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeInspector;

public class CustomCSharpSyntaxWalker : CSharpSyntaxWalker
{
    private readonly List<Func<CSharpSyntaxNode, bool>> _filters;
    private readonly List<CSharpSyntaxNode> _results = new();

    public CustomCSharpSyntaxWalker(List<Func<CSharpSyntaxNode, bool>> filters)
    {
        _filters = filters;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitStructDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitRecordDeclaration(node);
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitEnumDeclaration(node);
    }

    public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitDelegateDeclaration(node);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitNamespaceDeclaration(node);
    }
    
    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitFileScopedNamespaceDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitMethodDeclaration(node);
    }
    
    public override void VisitUsingDirective(UsingDirectiveSyntax node)
    {
        ApplyFilters(node);
        base.VisitUsingDirective(node);
    }

    private void ApplyFilters(CSharpSyntaxNode node)
    {
        if (_filters.All(filter => filter(node)))
        {
            _results.Add(node);
        }
    }

    public IEnumerable<CSharpSyntaxNode> GetResults() => _results;
}