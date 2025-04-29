using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeInspector;

public class QueryVisitor : ExpressionVisitor
{
    public List<Func<CSharpSyntaxNode, bool>> Filters { get; } = new();
    public Delegate? Selector { get; private set; }
    public Delegate? SortKeySelector { get; private set; }
    public bool IsDescending { get; private set; }
    public bool UseDistinct { get; private set; }

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
