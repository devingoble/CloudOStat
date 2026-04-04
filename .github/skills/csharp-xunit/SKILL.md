---
name: csharp-xunit
description: 'Get best practices for XUnit unit testing, including data-driven tests'
---

# XUnit Best Practices

Your goal is to help me write effective unit tests with XUnit, covering both standard and data-driven testing approaches.

## Project Setup

- Use a separate test project with naming convention `[ProjectName].Tests`
- Reference Microsoft.NET.Test.Sdk, xunit, and xunit.runner.visualstudio packages
- Create test classes that match the classes being tested (e.g., `CalculatorTests` for `Calculator`)
- Use .NET SDK test commands: `dotnet test` for running tests

## Test Structure

- No test class attributes required (unlike MSTest/NUnit)
- Use fact-based tests with `[Fact]` attribute for simple tests
- Follow the Arrange-Act-Assert (AAA) pattern
- Name tests using the pattern `MethodName_Scenario_ExpectedBehavior`
- Use constructor for setup and `IDisposable.Dispose()` for teardown
- Use `IClassFixture<T>` for shared context between tests in a class
- Use `ICollectionFixture<T>` for shared context between multiple test classes

## Standard Tests

- Keep tests focused on a single behavior
- Make tests independent and idempotent (can run in any order)
- Avoid test interdependencies

## Data-Driven Tests

- Use `[Theory]` combined with data source attributes
- Use `[InlineData]` for inline test data
- Use `[MemberData]` for method-based test data
- Use `[ClassData]` for class-based test data
- Use meaningful parameter names in data-driven tests

## Assertions

- Use `Assert.Equal` for value equality
- Use `Assert.Same` for reference equality
- Use `Assert.True`/`Assert.False` for boolean conditions
- Use `Assert.Contains`/`Assert.DoesNotContain` for collections
- Use `Assert.Throws<T>` or `await Assert.ThrowsAsync<T>` to test exceptions
- Use fluent assertions library for more readable assertions

## Mocking and Isolation

- Consider using Moq or NSubstitute alongside XUnit
- Mock dependencies to isolate units under test
- Use interfaces to facilitate mocking

## Test Organization

- Group tests by feature or component
- Use `[Trait("Category", "CategoryName")]` for categorization
- Consider output helpers (`ITestOutputHelper`) for test diagnostics
- Skip tests conditionally with `Skip = "reason"` in fact/theory attributes
