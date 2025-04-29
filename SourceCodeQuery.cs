using System.Collections;
using System.Linq.Expressions;

namespace CodeInspector;

public class SourceCodeQuery<T> : IQueryable<T>, IOrderedQueryable<T>
{
    public SourceCodeQuery(SourceCodeQueryProvider provider, Expression? expression = null)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Expression = expression ?? Expression.Constant(this);
    }

    public Type ElementType => typeof(T);
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator() => 
        Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
