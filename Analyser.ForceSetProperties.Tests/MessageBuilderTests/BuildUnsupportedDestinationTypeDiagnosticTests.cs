using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildUnsupportedDestinationTypeDiagnosticTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void UsesTheUnsupportedDestinationTypeRuleAtErrorSeverity()
        {
            const string source = @"
public class Factory
{
    public void Create()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var location = RoslynTestHelper.GetSyntaxNode(create).GetLocation();

            var diagnostic = _sut.BuildUnsupportedDestinationTypeDiagnostic(create.ReturnType, location);

            Assert.Equal("FSP007", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Contains("void", diagnostic.GetMessage());
        }

        [Fact]
        public void IsReportedAtTheGivenLocation()
        {
            const string source = @"
public class Factory
{
    public void Create()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var location = RoslynTestHelper.GetSyntaxNode(create).GetLocation();

            var diagnostic = _sut.BuildUnsupportedDestinationTypeDiagnostic(create.ReturnType, location);

            Assert.Equal(location, diagnostic.Location);
        }
    }
}
