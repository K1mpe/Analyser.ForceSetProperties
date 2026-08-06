using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildPropertyLineTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void PropertyWithOneLocation_RendersOneLine()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var name = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");
            var property = new RequiredProperty(name.Name, name);
            property.SetLocations.Add(new SetLocation(@"C:\Project\Factory.cs", 45));

            var result = _sut.BuildPropertyLine(property, @"C:\Project");

            Assert.Equal("Name: Factory.cs line 45", result);
        }

        [Fact]
        public void PropertyWithMultipleLocations_OnlyUsesTheFirstOne()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var name = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");
            var property = new RequiredProperty(name.Name, name);
            property.SetLocations.Add(new SetLocation(@"C:\Project\Factory.cs", 10));
            property.SetLocations.Add(new SetLocation(@"C:\Project\Factory.cs", 12, "Map"));

            var result = _sut.BuildPropertyLine(property, @"C:\Project");

            Assert.Equal("Name: Factory.cs line 10", result);
        }
    }
}
