using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildMissingPropertiesDiagnosticTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void OneMissingProperty_UsesTheSinglePropertyRule()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
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

            var diagnostic = _sut.BuildMissingPropertiesDiagnostic(context);

            Assert.Equal("FSP001", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(
                "Property 'Name' must be initialized when using ForceSetProperties",
                diagnostic.GetMessage());
        }

        [Fact]
        public void MultipleMissingProperties_UsesTheMultiplePropertiesRule()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
    public string Id { get; set; }
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

            var diagnostic = _sut.BuildMissingPropertiesDiagnostic(context);

            Assert.Equal("FSP002", diagnostic.Id);
            Assert.Equal(
                "The following properties must be initialized:\n - Name\n - Id",
                diagnostic.GetMessage());
        }

        [Fact]
        public void IsReportedAtTheAttributeLocation()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
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

            var diagnostic = _sut.BuildMissingPropertiesDiagnostic(context);

            Assert.Equal(context.AttributeLocation, diagnostic.Location);
        }
    }
}
