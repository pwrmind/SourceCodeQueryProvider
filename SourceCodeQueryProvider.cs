using System.Collections;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeInspector;

public class SourceCodeQueryProvider : IQueryProvider, IDisposable
{
    private readonly string _solutionPath;
    private readonly List<SyntaxTree> _syntaxTrees = new();
    private bool _disposed;

    public SourceCodeQueryProvider(string solutionPath)
    {
        if (string.IsNullOrEmpty(solutionPath))
            throw new ArgumentNullException(nameof(solutionPath));
        if (!Directory.Exists(solutionPath))
            throw new DirectoryNotFoundException($"Solution path not found: {solutionPath}");
            
        _solutionPath = solutionPath;
        LoadSyntaxTrees();
    }

    private void LoadSyntaxTrees()
    {
        try 
        {
            var csFiles = Directory.EnumerateFiles(_solutionPath, "*.cs", SearchOption.AllDirectories);
            foreach (var file in csFiles)
            {
                try 
                {
                    var code = File.ReadAllText(file);
                    var syntaxTree = CSharpSyntaxTree.ParseText(code);
                    _syntaxTrees.Add(syntaxTree);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing directory {_solutionPath}: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _syntaxTrees.Clear();
            _disposed = true;
        }
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new SourceCodeQuery<object>(this, expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new SourceCodeQuery<TElement>(this, expression);
    }

    public object Execute(Expression expression)
    {
        return Execute<object>(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SourceCodeQueryProvider));

        try
        {
            var visitor = new QueryVisitor();
            visitor.Visit(expression);

            var walker = new CustomCSharpSyntaxWalker(visitor.Filters);
            foreach (var tree in _syntaxTrees)
            {
                walker.Visit(tree.GetRoot());
            }

            var results = walker.GetResults();

            // Validate target type
            Type targetType = visitor.Selector?.Method.ReturnType ?? 
                (typeof(TResult).IsGenericType 
                    ? typeof(TResult).GetGenericArguments()[0] 
                    : typeof(TResult));

            if (targetType == null)
                throw new InvalidOperationException("Could not determine target type");

            var filtered = results
                .Where(n => targetType == typeof(string) 
                        || targetType.IsAssignableFrom(n.GetType()))
                .ToList();

            // Apply Select if needed
            if (visitor.Selector != null)
            {
                filtered = filtered.Select(node => 
                {
                    try
                    {
                        var result = visitor.Selector.DynamicInvoke(node);
                        return result as CSharpSyntaxNode ?? 
                            throw new InvalidCastException($"Cannot cast {result?.GetType()} to CSharpSyntaxNode");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error applying selector to node: {ex.Message}", ex);
                    }
                }).ToList();
            }

            // Apply sorting
            if (visitor.SortKeySelector != null)
            {
                try
                {
                    var ordered = visitor.IsDescending
                        ? filtered.OrderByDescending(node => 
                            ConvertToComparable(visitor.SortKeySelector.DynamicInvoke(node)))
                        : filtered.OrderBy(node => 
                            ConvertToComparable(visitor.SortKeySelector.DynamicInvoke(node)));

                    filtered = ordered.Cast<CSharpSyntaxNode>().ToList();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error applying sorting: {ex.Message}", ex);
                }
            }

            // Handle IEnumerable<T>
            if (typeof(TResult).IsGenericType && 
                typeof(TResult).GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = typeof(TResult).GetGenericArguments()[0];
                
                // Фильтруем и приводим к нужному типу
                var typedResults = filtered
                    .Where(n => elementType.IsAssignableFrom(n.GetType()))
                    .ToList();

                // Применяем Distinct если нужно
                if (visitor.UseDistinct)
                {
                    typedResults = typedResults
                        .Distinct(new SyntaxNodeEqualityComparer())
                        .ToList();
                }

                // Создаем список нужного типа и добавляем элементы
                var listType = typeof(List<>).MakeGenericType(elementType);
                var typedList = (IList)Activator.CreateInstance(listType)!;
                
                foreach (var item in typedResults)
                {
                    if (elementType.IsAssignableFrom(item.GetType()))
                    {
                        typedList.Add(item);
                    }
                }

                // Создаем IEnumerable<T>
                return (TResult)typedList;
            }

            // Handle scalar results
            var scalarResult = filtered.OfType<TResult>().FirstOrDefault();
            if (scalarResult == null && default(TResult) != null)
            {
                throw new InvalidOperationException("No matching result found");
            }
            return scalarResult!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing query: {ex.Message}", ex);
        }
    }

    private static IComparable ConvertToComparable(object? key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key), "Key cannot be null for comparison");

        if (key is IComparable comparable)
            return comparable;
        if (key is string str)
            return str;
            
        var strKey = key.ToString();
        if (strKey == null)
            throw new InvalidOperationException("Key cannot be converted to string");
            
        return strKey;
    }

    private class SyntaxNodeEqualityComparer : IEqualityComparer<CSharpSyntaxNode>
    {
        public bool Equals(CSharpSyntaxNode? x, CSharpSyntaxNode? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.ToFullString() == y.ToFullString();
        }

        public int GetHashCode(CSharpSyntaxNode obj)
        {
            return obj.ToFullString().GetHashCode();
        }
    }
}
