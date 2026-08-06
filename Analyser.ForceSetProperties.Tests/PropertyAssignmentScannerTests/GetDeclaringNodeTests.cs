using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class GetDeclaringNodeTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void MethodWithSourceInTheCompilation_ReturnsItsDeclaration()
        {
            const string source = @"
public class Factory
{
    public static void Map()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var map = RoslynTestHelper.GetMethod(compilation, "Factory", "Map");
            var expectedNode = RoslynTestHelper.GetSyntaxNode(map);

            var result = _sut.GetDeclaringNode(map);

            Assert.Equal(expectedNode, result);
        }

        [Fact]
        public void MethodWithoutSourceInTheCompilation_ReturnsNull()
        {
            const string librarySource = @"
public class Factory
{
    public static void Map()
    {
    }
}";
            var libraryReference = RoslynTestHelper.CompileToReference(librarySource);

            const string consumerSource = @"
public class Placeholder
{
}";
            var consumerCompilation = RoslynTestHelper.Compile(consumerSource)
                .AddReferences(libraryReference);

            var map = RoslynTestHelper.GetMethod(consumerCompilation, "Factory", "Map");

            var result = _sut.GetDeclaringNode(map);

            Assert.Null(result);
        }
    }
}
