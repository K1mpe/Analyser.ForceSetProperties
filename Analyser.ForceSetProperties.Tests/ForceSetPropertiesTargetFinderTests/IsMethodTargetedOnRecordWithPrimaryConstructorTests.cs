using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetPropertiesTargetFinderTests
{
    public class IsMethodTargetedOnRecordWithPrimaryConstructorTests
    {
        private readonly ForceSetPropertiesTargetFinder _sut = new();

        [Fact]
        public void MethodTargetOnARecordWithAPrimaryConstructor_ReturnsTrue()
        {
            const string source = @"
[method: ForceSetProperties]
public record Person(string Name, int Age);";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();

            Assert.True(_sut.IsMethodTargetedOnRecordWithPrimaryConstructor(attribute));
        }

        [Fact]
        public void NoTargetSpecifierOnARecordWithAPrimaryConstructor_ReturnsFalse()
        {
            const string source = @"
[ForceSetProperties]
public record Person(string Name, int Age);";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();

            Assert.False(_sut.IsMethodTargetedOnRecordWithPrimaryConstructor(attribute));
        }

        [Fact]
        public void MethodTargetOnARecordWithNoPrimaryConstructor_ReturnsFalse()
        {
            const string source = @"
[method: ForceSetProperties]
public record Person
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();

            Assert.False(_sut.IsMethodTargetedOnRecordWithPrimaryConstructor(attribute));
        }

        [Fact]
        public void MethodTargetOnAnOrdinaryMethod_ReturnsFalse()
        {
            const string source = @"
public class Factory
{
    [method: ForceSetProperties]
    public object Create()
    {
        return new object();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();

            Assert.False(_sut.IsMethodTargetedOnRecordWithPrimaryConstructor(attribute));
        }
    }
}
