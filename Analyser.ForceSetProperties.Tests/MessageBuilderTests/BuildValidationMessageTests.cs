using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildValidationMessageTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void EveryPropertySetDirectly_UsesTheShortSummary()
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

            var result = _sut.BuildValidationMessage(context, @"C:\Project");

            Assert.Equal("ForceSetProperties validated DtoModel: Name", result);
        }

        [Fact]
        public void APropertyCameFromASubMethod_UsesTheDetailedBreakdown()
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
        return Map();
    }

    private static DtoModel Map()
    {
        return new DtoModel { Name = ""x"" };
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            context.RequiredProperties[0].SetLocations.Add(new SetLocation(@"C:\Project\Factory.cs", 12, "Map"));

            var result = _sut.BuildValidationMessage(context, @"C:\Project");

            Assert.StartsWith("Type checked: DtoModel", result);
        }
    }
}
