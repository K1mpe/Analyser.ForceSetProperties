using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildShortSummaryTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void StatesTheDestinationTypeAndEveryRequiredProperty()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
    public System.DateTime CreatedAt { get; set; }
    public System.DateTime UpdatedAt { get; set; }
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

            var result = _sut.BuildShortSummary(context);

            Assert.Equal("ForceSetProperties validated DtoModel: Name, CreatedAt, UpdatedAt", result);
        }

        [Fact]
        public void NoRequiredProperties_StillStatesTheDestinationType()
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

            var result = _sut.BuildShortSummary(context);

            Assert.Equal("ForceSetProperties validated DtoModel: ", result);
        }
    }
}
