using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetPropertiesTargetFinderTests
{
    public class GetPrimaryConstructorTests
    {
        private readonly ForceSetPropertiesTargetFinder _sut = new();

        [Fact]
        public void RecordWithAPrimaryConstructor_ReturnsIt()
        {
            const string source = @"
public record Person(string Name, int Age);";
            var compilation = RoslynTestHelper.Compile(source);
            var person = RoslynTestHelper.GetType(compilation, "Person");
            var tree = compilation.GetUserSyntaxTree();
            var record = tree.GetRoot().DescendantNodesAndSelf().OfType<RecordDeclarationSyntax>().Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var result = _sut.GetPrimaryConstructor(record, semanticModel);

            Assert.NotNull(result);
            Assert.Equal(2, result!.Parameters.Length);
            Assert.Equal(person, result.ContainingType, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void RecordStructWithAPrimaryConstructor_ReturnsIt()
        {
            const string source = @"
public record struct Point(int X, int Y);";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var record = tree.GetRoot().DescendantNodesAndSelf().OfType<RecordDeclarationSyntax>().Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var result = _sut.GetPrimaryConstructor(record, semanticModel);

            Assert.NotNull(result);
            Assert.Equal(2, result!.Parameters.Length);
        }

        [Fact]
        public void RecordWithNoPrimaryConstructor_ReturnsNull()
        {
            const string source = @"
public record Person
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var record = tree.GetRoot().DescendantNodesAndSelf().OfType<RecordDeclarationSyntax>().Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var result = _sut.GetPrimaryConstructor(record, semanticModel);

            Assert.Null(result);
        }

        [Fact]
        public void RecordWithAPrimaryConstructorAndAChainedSecondaryConstructor_StillReturnsThePrimaryOne()
        {
            const string source = @"
public record Person(string Name, int Age)
{
    public Person() : this(default, default) { }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var record = tree.GetRoot().DescendantNodesAndSelf().OfType<RecordDeclarationSyntax>().Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var result = _sut.GetPrimaryConstructor(record, semanticModel);

            Assert.NotNull(result);
            Assert.Equal(2, result!.Parameters.Length);
        }
    }
}
