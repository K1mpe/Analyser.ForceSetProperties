using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildAmbiguousGenericInferenceDiagnosticTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void UsesTheAmbiguousGenericInferenceRuleAtErrorSeverity()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    public System.Collections.Generic.List<DtoModel> Create()
    {
        return new System.Collections.Generic.List<DtoModel>();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var location = RoslynTestHelper.GetSyntaxNode(create).GetLocation();

            var diagnostic = _sut.BuildAmbiguousGenericInferenceDiagnostic(create.ReturnType, location);

            Assert.Equal("FSP006", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Contains("List<DtoModel>", diagnostic.GetMessage());
        }

        [Fact]
        public void IsReportedAtTheGivenLocation()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    public System.Collections.Generic.List<DtoModel> Create()
    {
        return new System.Collections.Generic.List<DtoModel>();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var location = RoslynTestHelper.GetSyntaxNode(create).GetLocation();

            var diagnostic = _sut.BuildAmbiguousGenericInferenceDiagnostic(create.ReturnType, location);

            Assert.Equal(location, diagnostic.Location);
        }
    }
}
