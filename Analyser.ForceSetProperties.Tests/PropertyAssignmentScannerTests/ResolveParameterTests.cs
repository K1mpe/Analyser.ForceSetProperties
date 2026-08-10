using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class ResolveParameterTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void PositionalArgument_ResolvesByIndex()
        {
            const string source = @"
public record Person(string Name, int Age);

public class Factory
{
    public Person Create(string name, int age)
    {
        return new Person(name, age);
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetConstructor(compilation, "Person");
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var argumentList = node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single().ArgumentList!;

            var parameter = _sut.ResolveParameter(constructor, argumentList.Arguments[1], position: 1);

            Assert.Equal("Age", parameter?.Name);
        }

        [Fact]
        public void NamedArgument_ResolvesByNameRegardlessOfPosition()
        {
            const string source = @"
public record Person(string Name, int Age);

public class Factory
{
    public Person Create(string name, int age)
    {
        return new Person(Age: age, Name: name);
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetConstructor(compilation, "Person");
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var argumentList = node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single().ArgumentList!;

            // Written first, but named "Age" - must resolve by name, not by its position of 0.
            var parameter = _sut.ResolveParameter(constructor, argumentList.Arguments[0], position: 0);

            Assert.Equal("Age", parameter?.Name);
        }

        [Fact]
        public void PositionOutOfRange_ReturnsNull()
        {
            const string source = @"
public record Person(string Name);

public class Factory
{
    public Person Create(string name)
    {
        return new Person(name);
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetConstructor(compilation, "Person");
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var argument = node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single().ArgumentList!.Arguments[0];

            var parameter = _sut.ResolveParameter(constructor, argument, position: 5);

            Assert.Null(parameter);
        }
    }
}
