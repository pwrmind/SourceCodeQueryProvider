using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeInspector;

/// <summary>
/// Walks through C# syntax trees and applies filters to collect matching syntax nodes.
/// </summary>
public class CustomCSharpSyntaxWalker : CSharpSyntaxWalker
{
    private readonly List<Func<CSharpSyntaxNode, bool>> _filters;
    private readonly List<CSharpSyntaxNode> _results = new();

    /// <summary>
    /// Initializes a new instance of CustomCSharpSyntaxWalker with the specified filters.
    /// </summary>
    /// <param name="filters">The list of filters to apply to syntax nodes</param>
    public CustomCSharpSyntaxWalker(List<Func<CSharpSyntaxNode, bool>> filters)
    {
        _filters = filters;
    }

    /// <summary>
    /// Visits a class declaration and applies filters.
    /// </summary>
    /// <param name="node">The class declaration to visit</param>
    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitClassDeclaration(node);
    }

    /// <summary>
    /// Visits a struct declaration and applies filters.
    /// </summary>
    /// <param name="node">The struct declaration to visit</param>
    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitStructDeclaration(node);
    }

    /// <summary>
    /// Visits an interface declaration and applies filters.
    /// </summary>
    /// <param name="node">The interface declaration to visit</param>
    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitInterfaceDeclaration(node);
    }

    /// <summary>
    /// Visits a record declaration and applies filters.
    /// </summary>
    /// <param name="node">The record declaration to visit</param>
    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitRecordDeclaration(node);
    }

    /// <summary>
    /// Visits an enum declaration and applies filters.
    /// </summary>
    /// <param name="node">The enum declaration to visit</param>
    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitEnumDeclaration(node);
    }

    /// <summary>
    /// Visits a delegate declaration and applies filters.
    /// </summary>
    /// <param name="node">The delegate declaration to visit</param>
    public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitDelegateDeclaration(node);
    }

    /// <summary>
    /// Visits a namespace declaration and applies filters.
    /// </summary>
    /// <param name="node">The namespace declaration to visit</param>
    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitNamespaceDeclaration(node);
    }
    
    /// <summary>
    /// Visits a file-scoped namespace declaration and applies filters.
    /// </summary>
    /// <param name="node">The file-scoped namespace declaration to visit</param>
    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitFileScopedNamespaceDeclaration(node);
    }

    /// <summary>
    /// Visits a method declaration and applies filters.
    /// </summary>
    /// <param name="node">The method declaration to visit</param>
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        ApplyFilters(node);
        base.VisitMethodDeclaration(node);
    }
    
    /// <summary>
    /// Visits a using directive and applies filters.
    /// </summary>
    /// <param name="node">The using directive to visit</param>
    public override void VisitUsingDirective(UsingDirectiveSyntax node)
    {
        ApplyFilters(node);
        base.VisitUsingDirective(node);
    }

    /// <summary>
    /// Applies all filters to a syntax node and adds it to results if all filters pass.
    /// </summary>
    /// <param name="node">The syntax node to filter</param>
    private void ApplyFilters(CSharpSyntaxNode node)
    {
        if (_filters.All(filter => filter(node)))
        {
            _results.Add(node);
        }
    }

    /// <summary>
    /// Gets the collection of syntax nodes that passed all filters.
    /// </summary>
    /// <returns>An enumerable of matching syntax nodes</returns>
    public IEnumerable<CSharpSyntaxNode> GetResults() => _results;
}