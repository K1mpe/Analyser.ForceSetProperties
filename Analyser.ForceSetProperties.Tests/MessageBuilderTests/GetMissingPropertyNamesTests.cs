using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class GetMissingPropertyNamesTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void OnlyUnsetProperties_AreReturned()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Description { get; set; }
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
            context.RequiredProperties[0].SetLocations.Add(new SetLocation("Factory.cs", 10));

            var result = _sut.GetMissingPropertyNames(context);

            Assert.Equal(new[] { "Id", "Description" }, result);
        }

        [Fact]
        public void EverythingSet_ReturnsEmpty()
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
            context.RequiredProperties[0].SetLocations.Add(new SetLocation("Factory.cs", 10));

            var result = _sut.GetMissingPropertyNames(context);

            Assert.Empty(result);
        }
    }
}
