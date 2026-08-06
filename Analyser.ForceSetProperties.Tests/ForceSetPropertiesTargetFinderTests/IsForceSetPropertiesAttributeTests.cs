using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetPropertiesTargetFinderTests
{
    public class IsForceSetPropertiesAttributeTests
    {
        private readonly ForceSetPropertiesTargetFinder _sut = new();

        [Fact]
        public void ForceSetPropertiesAttribute_ReturnsTrue()
        {
            const string source = @"
[ForceSetProperties]
public class DtoModel
{
}";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var semanticModel = compilation.GetSemanticModel(tree);
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();

            var result = _sut.IsForceSetPropertiesAttribute(attribute, semanticModel);

            Assert.True(result);
        }

        [Fact]
        public void UnrelatedAttribute_ReturnsFalse()
        {
            const string source = @"
[System.Obsolete]
public class DtoModel
{
}";
            var compilation = RoslynTestHelper.Compile(source);
            var tree = compilation.GetUserSyntaxTree();
            var semanticModel = compilation.GetSemanticModel(tree);
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();

            var result = _sut.IsForceSetPropertiesAttribute(attribute, semanticModel);

            Assert.False(result);
        }
    }
}
