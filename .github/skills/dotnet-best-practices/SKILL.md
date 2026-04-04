---
name: dotnet-best-practices
description: 'Ensure .NET/C# code meets best practices for the solution/project.'
---

# .NET/C# Best Practices

Your task is to ensure .NET/C# code meets the best practices specific to this solution/project. This includes:

## Documentation & Structure

- Create comprehensive XML documentation comments for all public classes, interfaces, methods, and properties
- Include parameter descriptions and return value descriptions in XML comments
- Follow the established namespace structure

## Design Patterns & Architecture

- Use primary constructor syntax for dependency injection (e.g., `public class MyClass(IDependency dependency)`)
- Use interface segregation with clear naming conventions (prefix interfaces with 'I')
- Follow the Factory pattern for complex object creation

## Dependency Injection & Services

- Use constructor dependency injection with null checks via ArgumentNullException
- Register services with appropriate lifetimes (Singleton, Scoped, Transient)
- Use Microsoft.Extensions.DependencyInjection patterns
- Implement service interfaces for testability

## Async/Await Patterns

- Use async/await for all I/O operations and long-running tasks
- Return Task or Task<T> from async methods
- Use ConfigureAwait(false) where appropriate
- Handle async exceptions properly

## Testing Standards

- Follow AAA pattern (Arrange, Act, Assert)
- Test both success and failure scenarios
- Include null parameter validation tests

## Configuration & Settings

- Use strongly-typed configuration classes with data annotations
- Use IConfiguration binding for settings
- Support appsettings.json configuration files

## Error Handling & Logging

- Use structured logging with Microsoft.Extensions.Logging
- Include scoped logging with meaningful context
- Throw specific exceptions with descriptive messages

## Performance & Security

- Implement proper input validation and sanitization
- Follow secure coding practices
- Use C# 12+ features and .NET optimizations where applicable

## Code Quality

- Ensure SOLID principles compliance
- Avoid code duplication through base classes and utilities
- Use meaningful names that reflect domain concepts
- Keep methods focused and cohesive
- Implement proper disposal patterns for resources
