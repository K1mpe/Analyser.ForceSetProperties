using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildUnsupportedTargetDiagnosticTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void UsesTheUnsupportedTargetRuleAtErrorSeverity()
        {
            const string source = @"
[ForceSetProperties]
public class DtoModel
{
}";
            var compilation = RoslynTestHelper.Compile(source);
            var location = compilation.GetType("DtoModel").Locations[0];

            var diagnostic = _sut.BuildUnsupportedTargetDiagnostic(location);

            Assert.Equal("FSP006", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        }

        [Fact]
        public void IsReportedAtTheGivenLocation()
        {
            const string source = @"
[ForceSetProperties]
public class DtoModel
{
}";
            var compilation = RoslynTestHelper.Compile(source);
            var location = compilation.GetType("DtoModel").Locations[0];

            var diagnostic = _sut.BuildUnsupportedTargetDiagnostic(location);

            Assert.Equal(location, diagnostic.Location);
        }
    }
}
