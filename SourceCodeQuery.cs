using System.Collections;
using System.Linq.Expressions;

namespace CodeInspector;

/// <summary>
/// Represents a queryable collection of source code elements of type T.
/// Implements both IQueryable<T> and IOrderedQueryable<T> interfaces to support LINQ operations.
/// </summary>
/// <typeparam name="T">The type of source code elements to query</typeparam>
public class SourceCodeQuery<T> : IQueryable<T>, IOrderedQueryable<T>
{
    /// <summary>
    /// Initializes a new instance of SourceCodeQuery with the specified provider and optional expression.
    /// </summary>
    /// <param name="provider">The query provider that will execute the queries</param>
    /// <param name="expression">Optional expression representing the query. If not provided, a constant expression is used.</param>
    public SourceCodeQuery(SourceCodeQueryProvider provider, Expression? expression = null)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Expression = expression ?? Expression.Constant(this);
    }

    /// <summary>
    /// Gets the type of the elements in the queryable collection.
    /// </summary>
    public Type ElementType => typeof(T);

    /// <summary>
    /// Gets the expression tree associated with this query.
    /// </summary>
    public Expression Expression { get; }

    /// <summary>
    /// Gets the query provider that executes the queries.
    /// </summary>
    public IQueryProvider Provider { get; }

    /// <summary>
    /// Returns an enumerator that iterates through the collection of source code elements.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection</returns>
    public IEnumerator<T> GetEnumerator() => 
        Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    
    /// <summary>
    /// Returns an enumerator that iterates through the collection of source code elements.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
