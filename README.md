# Source Code Query Provider

A powerful C# library that enables LINQ-style querying of C# source code. This project provides a custom LINQ provider that allows you to analyze and query C# source code using familiar LINQ syntax.

## Features

- Query C# source code using LINQ syntax
- Support for filtering, selection, and sorting operations
- Analyze various C# syntax elements (classes, methods, namespaces, etc.)
- Process multiple source files in a solution
- Thread-safe and disposable implementation

## Core Components

### SourceCodeQueryProvider
The main provider class that implements `IQueryProvider` and handles query execution. It:
- Loads and parses C# source files
- Processes LINQ queries against the source code
- Manages syntax trees and resources

### SourceCodeQuery
A generic queryable collection that implements both `IQueryable<T>` and `IOrderedQueryable<T>`. It:
- Represents a queryable collection of source code elements
- Provides enumeration capabilities
- Supports LINQ operations

### QueryVisitor
Processes LINQ query expressions to extract:
- Filter conditions
- Selection operations
- Sorting criteria
- Distinct operations

### CustomCSharpSyntaxWalker
Walks through C# syntax trees and:
- Applies filters to collect matching syntax nodes
- Supports various C# syntax elements
- Collects results based on filter criteria

## Usage Examples

### Basic Setup
```csharp
using CodeInspector;

// Initialize the provider with a solution path
using var provider = new SourceCodeQueryProvider("path/to/your/solution");

// Create a queryable collection
var query = new SourceCodeQuery<ClassDeclarationSyntax>(provider);
```

### Finding All Classes
```csharp
var classes = from c in query
              select c;

foreach (var classDecl in classes)
{
    Console.WriteLine($"Found class: {classDecl.Identifier.Text}");
}
```

### Finding Classes with Specific Names
```csharp
var specificClasses = from c in query
                      where c.Identifier.Text.Contains("Service")
                      select c;
```

### Finding Methods with Parameters
```csharp
var methods = from m in new SourceCodeQuery<MethodDeclarationSyntax>(provider)
              where m.ParameterList.Parameters.Count > 0
              select m;
```

### Complex Queries
```csharp
var results = from c in new SourceCodeQuery<ClassDeclarationSyntax>(provider)
              where c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))
              orderby c.Identifier.Text
              select new
              {
                  ClassName = c.Identifier.Text,
                  MethodCount = c.Members.OfType<MethodDeclarationSyntax>().Count()
              };
```

## Requirements

- .NET 6.0 or later
- Microsoft.CodeAnalysis.CSharp package
- Access to C# source files

## Installation

1. Clone the repository
2. Add the project to your solution
3. Reference the project in your application
4. Install required NuGet packages:
   ```bash
   dotnet add package Microsoft.CodeAnalysis.CSharp
   ```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
