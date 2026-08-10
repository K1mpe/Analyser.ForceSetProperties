using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildInterfaceDestinationTypeWarningDiagnosticTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void UsesTheInterfaceDestinationTypeRuleAtWarningSeverity()
        {
            const string source = @"
public interface IDto
{
    string Name { get; set; }
}

public class Factory
{
    public void Create()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var idto = RoslynTestHelper.GetType(compilation, "IDto");
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var location = RoslynTestHelper.GetSyntaxNode(create).GetLocation();

            var diagnostic = _sut.BuildInterfaceDestinationTypeWarningDiagnostic(idto, location);

            Assert.Equal("FSP101", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Contains("IDto", diagnostic.GetMessage());
        }

        [Fact]
        public void IsReportedAtTheGivenLocation()
        {
            const string source = @"
public interface IDto
{
    string Name { get; set; }
}

public class Factory
{
    public void Create()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var idto = RoslynTestHelper.GetType(compilation, "IDto");
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var location = RoslynTestHelper.GetSyntaxNode(create).GetLocation();

            var diagnostic = _sut.BuildInterfaceDestinationTypeWarningDiagnostic(idto, location);

            Assert.Equal(location, diagnostic.Location);
        }
    }
}
