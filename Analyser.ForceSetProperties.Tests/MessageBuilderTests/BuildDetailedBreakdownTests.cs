using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildDetailedBreakdownTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void ListsTheTypeAndEveryPropertyOnItsOwnLine()
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
            context.RequiredProperties[0].SetLocations.Add(new SetLocation(@"C:\Project\Factory.cs", 45));
            context.RequiredProperties[1].SetLocations.Add(new SetLocation(@"C:\Project\Factory.cs", 46, "Map"));

            var result = _sut.BuildDetailedBreakdown(context, @"C:\Project");

            Assert.Equal(
                "Type checked: DtoModel\nName: Factory.cs line 45\nId: Factory.cs line 46 (via Map)",
                result);
        }
    }
}
