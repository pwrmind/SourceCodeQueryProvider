using System.Collections;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeInspector;

/// <summary>
/// Provides functionality to execute queries against C# source code.
/// Implements IQueryProvider to support LINQ operations on source code.
/// </summary>
public class SourceCodeQueryProvider : IQueryProvider, IDisposable
{
    private readonly string _solutionPath;
    private readonly List<SyntaxTree> _syntaxTrees = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of SourceCodeQueryProvider with the specified solution path.
    /// </summary>
    /// <param name="solutionPath">The path to the solution directory containing C# source files</param>
    /// <exception cref="ArgumentNullException">Thrown when solutionPath is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when solutionPath does not exist</exception>
    public SourceCodeQueryProvider(string solutionPath)
    {
        if (string.IsNullOrEmpty(solutionPath))
            throw new ArgumentNullException(nameof(solutionPath));
        if (!Directory.Exists(solutionPath))
            throw new DirectoryNotFoundException($"Solution path not found: {solutionPath}");
            
        _solutionPath = solutionPath;
        LoadSyntaxTrees();
    }

    /// <summary>
    /// Loads all C# source files from the solution directory and creates syntax trees.
    /// </summary>
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

    /// <summary>
    /// Releases all resources used by the SourceCodeQueryProvider.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _syntaxTrees.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Creates a new query object for the specified expression.
    /// </summary>
    /// <param name="expression">The expression representing the query</param>
    /// <returns>A new query object</returns>
    public IQueryable CreateQuery(Expression expression)
    {
        return new SourceCodeQuery<object>(this, expression);
    }

    /// <summary>
    /// Creates a new query object for the specified expression and element type.
    /// </summary>
    /// <typeparam name="TElement">The type of elements in the query</typeparam>
    /// <param name="expression">The expression representing the query</param>
    /// <returns>A new query object</returns>
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new SourceCodeQuery<TElement>(this, expression);
    }

    /// <summary>
    /// Executes the query represented by the specified expression.
    /// </summary>
    /// <param name="expression">The expression representing the query</param>
    /// <returns>The result of executing the query</returns>
    public object Execute(Expression expression)
    {
        return Execute<object>(expression);
    }

    /// <summary>
    /// Executes the strongly-typed query represented by the specified expression.
    /// </summary>
    /// <typeparam name="TResult">The type of the query result</typeparam>
    /// <param name="expression">The expression representing the query</param>
    /// <returns>The result of executing the query</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is an error executing the query</exception>
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

    /// <summary>
    /// Converts a key value to an IComparable for sorting operations.
    /// </summary>
    /// <param name="key">The key value to convert</param>
    /// <returns>An IComparable representation of the key</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when key cannot be converted to string</exception>
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

    /// <summary>
    /// Provides equality comparison for CSharpSyntaxNode instances based on their string representation.
    /// </summary>
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
