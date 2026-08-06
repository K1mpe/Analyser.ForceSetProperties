using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildValidatedDiagnosticTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void UsesTheValidatedRuleAtInfoSeverity()
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
        return new DtoModel { Name = ""x"" };
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            context.RequiredProperties[0].SetLocations.Add(new SetLocation(@"C:\Project\Factory.cs", 10));

            var diagnostic = _sut.BuildValidatedDiagnostic(context, @"C:\Project");

            Assert.Equal("FSP101", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
            Assert.Equal("ForceSetProperties validated DtoModel: Name", diagnostic.GetMessage());
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
        return new DtoModel { Name = ""x"" };
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            context.RequiredProperties[0].SetLocations.Add(new SetLocation(@"C:\Project\Factory.cs", 10));

            var diagnostic = _sut.BuildValidatedDiagnostic(context, @"C:\Project");

            Assert.Equal(context.AttributeLocation, diagnostic.Location);
        }
    }
}
