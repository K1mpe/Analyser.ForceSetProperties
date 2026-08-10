using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildNoPropertiesFoundDiagnosticTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void ReportsFSP005AtTheAttributeLocation()
        {
            const string source = @"
public class DtoModel
{
}

public class Factory
{
    [ForceSetProperties]
    public DtoModel Create()
    {
        return new DtoModel();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);

            var diagnostic = _sut.BuildNoPropertiesFoundDiagnostic(context);

            Assert.Equal("FSP005", diagnostic.Id);
            Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(context.AttributeLocation, diagnostic.Location);
            Assert.Equal(
                "ForceSetProperties found no assignable properties on 'DtoModel' to validate; check that this is the intended destination type",
                diagnostic.GetMessage());
        }
    }
}
