using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetPropertiesTargetFinderTests
{
    public class IsOnSupportedDeclarationTests
    {
        private readonly ForceSetPropertiesTargetFinder _sut = new();

        [Fact]
        public void AttributeOnAMethod_ReturnsTrue()
        {
            const string source = @"
public class Factory
{
    [ForceSetProperties]
    public object Create()
    {
        return new object();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();

            Assert.True(_sut.IsOnSupportedDeclaration(attribute));
        }

        [Fact]
        public void AttributeOnAClass_ReturnsFalse()
        {
            const string source = @"
[ForceSetProperties]
public class DtoModel
{
}";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();

            Assert.False(_sut.IsOnSupportedDeclaration(attribute));
        }
    }
}
