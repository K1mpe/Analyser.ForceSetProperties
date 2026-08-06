using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class ResolveAttributeLocationTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void AttributeAppliedInSource_PointsAtTheAttributeItself()
        {
            const string source = @"
            public class Factory
            {
                [ForceSetProperties]
                public string Create()
                {
                    return string.Empty;
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var fallbackNode = RoslynTestHelper.GetSyntaxNode(create);

            var location = _sut.ResolveAttributeLocation(attribute, fallbackNode);

            var text = location.SourceTree!.GetText().ToString(location.SourceSpan);
            Assert.Equal("ForceSetProperties", text);
        }

        [Fact]
        public void AttributeWithoutSourceInCurrentCompilation_FallsBackToGivenNode()
        {
            const string librarySource = @"
            public class Factory
            {
                [ForceSetProperties]
                public string Create()
                {
                    return string.Empty;
                }
            }";
            var libraryReference = RoslynTestHelper.CompileToReference(librarySource);

            const string consumerSource = @"
            public class Placeholder
            {
            }";
            var consumerCompilation = RoslynTestHelper.Compile(consumerSource)
                .AddReferences(libraryReference);

            var create = RoslynTestHelper.GetMethod(consumerCompilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var fallbackNode = RoslynTestHelper.GetSyntaxNode(
                RoslynTestHelper.GetType(consumerCompilation, "Placeholder"));

            var location = _sut.ResolveAttributeLocation(attribute, fallbackNode);

            Assert.Equal(fallbackNode.GetLocation(), location);
        }
    }
}
