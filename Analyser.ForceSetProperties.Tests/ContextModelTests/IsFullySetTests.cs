using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextModelTests
{
    public class IsFullySetTests
    {
        [Fact]
        public void AllRequiredPropertiesHaveASetLocation_ReturnsTrue()
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

            foreach (var property in context.RequiredProperties)
            {
                property.SetLocations.Add(new SetLocation("Factory.cs", 1));
            }

            Assert.True(context.IsFullySet);
        }

        [Fact]
        public void OneRequiredPropertyHasNoSetLocation_ReturnsFalse()
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

            context.RequiredProperties[0].SetLocations.Add(new SetLocation("Factory.cs", 1));

            Assert.False(context.IsFullySet);
        }

        [Fact]
        public void NoRequiredProperties_ReturnsTrue()
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

            Assert.True(context.IsFullySet);
        }
    }
}
