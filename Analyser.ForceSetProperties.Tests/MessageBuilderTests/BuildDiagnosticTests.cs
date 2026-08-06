using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildDiagnosticTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void FullySetContext_ProducesTheValidatedDiagnostic()
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

            var diagnostic = _sut.BuildDiagnostic(context, @"C:\Project");

            Assert.Equal("FSP101", diagnostic.Id);
        }

        [Fact]
        public void NotFullySetContext_ProducesAMissingPropertiesDiagnostic()
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

            var diagnostic = _sut.BuildDiagnostic(context, @"C:\Project");

            Assert.Equal("FSP001", diagnostic.Id);
        }
    }
}
