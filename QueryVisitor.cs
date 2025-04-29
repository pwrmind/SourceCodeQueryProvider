using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeInspector;

/// <summary>
/// Visits and processes LINQ query expressions to extract filtering, selection, and sorting operations.
/// </summary>
public class QueryVisitor : ExpressionVisitor
{
    /// <summary>
    /// Gets the list of filter predicates to apply to source code nodes.
    /// </summary>
    public List<Func<CSharpSyntaxNode, bool>> Filters { get; } = new();

    /// <summary>
    /// Gets the selector function that transforms source code nodes.
    /// </summary>
    public Delegate? Selector { get; private set; }

    /// <summary>
    /// Gets the key selector function used for sorting.
    /// </summary>
    public Delegate? SortKeySelector { get; private set; }

    /// <summary>
    /// Gets a value indicating whether sorting should be in descending order.
    /// </summary>
    public bool IsDescending { get; private set; }

    /// <summary>
    /// Gets a value indicating whether duplicate results should be removed.
    /// </summary>
    public bool UseDistinct { get; private set; }

    /// <summary>
    /// Visits a method call expression and processes LINQ operations.
    /// </summary>
    /// <param name="node">The method call expression to visit</param>
    /// <returns>The visited expression</returns>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == "Distinct")
        {
            UseDistinct = true;
            return base.Visit(node.Arguments[0]);
        }
        else if (node.Method.Name == "Where")
        {
            var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;
            var filter = CreateFilter(lambda);
            Filters.Add(filter);
        }
        else if (node.Method.Name == "Select")
        {
            var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;
            Selector = lambda.Compile();
        }
        else if (node.Method.Name == "OrderBy" || node.Method.Name == "OrderByDescending")
        {
            var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;
            SortKeySelector = lambda.Compile();
            IsDescending = node.Method.Name == "OrderByDescending";
        }

        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Creates a filter function from a lambda expression.
    /// </summary>
    /// <param name="lambda">The lambda expression representing the filter</param>
    /// <returns>A function that applies the filter to source code nodes</returns>
    private Func<CSharpSyntaxNode, bool> CreateFilter(LambdaExpression lambda)
    {
        var param = lambda.Parameters[0];
        var body = lambda.Body;

        return node =>
        {
            var nodeType = GetNodeType(node);
            if (nodeType != param.Type) return false;

            var condition = Expression.Lambda(body, param)
                .Compile()
                .DynamicInvoke(node);

            return condition is bool b && b;
        };
    }

    /// <summary>
    /// Gets the specific type of a CSharpSyntaxNode.
    /// </summary>
    /// <param name="node">The syntax node to get the type for</param>
    /// <returns>The specific type of the syntax node</returns>
    private Type GetNodeType(CSharpSyntaxNode node)
    {
        if (node is ClassDeclarationSyntax)
            return typeof(ClassDeclarationSyntax);
        if (node is MethodDeclarationSyntax)
            return typeof(MethodDeclarationSyntax);
        if (node is UsingDirectiveSyntax)
            return typeof(UsingDirectiveSyntax);
        return node.GetType();
    }
}
