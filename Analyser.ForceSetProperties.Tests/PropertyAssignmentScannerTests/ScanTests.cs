using System.Linq;
using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class ScanTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void AllPropertiesSetDirectly_MarksEveryRequiredProperty()
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
        return new DtoModel { Name = ""x"", Id = ""y"" };
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);

            _sut.Scan(context, compilation);

            Assert.True(context.IsFullySet);
        }

        [Fact]
        public void PropertySetThroughATracedConstructor_IsMarkedWithTheConstructorName()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }

    public DtoModel(string name)
    {
        Name = name;
    }
}

public class Factory
{
    [ForceSetProperties]
    public DtoModel Create()
    {
        return new DtoModel(""x"");
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);

            _sut.Scan(context, compilation);

            var setLocation = context.RequiredProperties[0].SetLocations.Single();
            Assert.Equal(".ctor", setLocation.MethodName);
        }

        [Fact]
        public void PropertySetOnlyInAChainedConstructor_IsFoundThroughTheThisInitializer()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
    public string Id { get; set; }

    public DtoModel()
    {
        Id = ""generated"";
    }

    [ForceSetProperties]
    public DtoModel(string name) : this()
    {
        Name = name;
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetType(compilation, "DtoModel").Constructors
                .Single(c => c.Parameters.Length == 1);
            var context = RoslynTestHelper.BuildContext(compilation, constructor);

            _sut.Scan(context, compilation);

            Assert.True(context.IsFullySet);
            var idLocation = context.RequiredProperties.Single(p => p.Name == "Id").SetLocations.Single();
            Assert.Equal(".ctor", idLocation.MethodName);
        }

        [Fact]
        public void MultipleDestinationTypesFromTheSameAttribute_AreScannedIndependently()
        {
            const string source = @"
public class UserDto
{
    public string Name { get; set; }
}

public class RoleDto
{
    public string Name { get; set; }
}

public class Factory
{
    [ForceSetProperties(Types = new[] { typeof(UserDto), typeof(RoleDto) })]
    public void Map(out UserDto userDto, out RoleDto roleDto)
    {
        userDto = new UserDto { Name = ""user"" };
        roleDto = new RoleDto();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var map = RoslynTestHelper.GetMethod(compilation, "Factory", "Map");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(map);
            var node = RoslynTestHelper.GetSyntaxNode(map);
            var contexts = new ContextBuilder().Build(map, node, attribute, compilation);

            foreach (var context in contexts)
            {
                _sut.Scan(context, compilation);
            }

            Assert.True(contexts[0].IsFullySet);
            Assert.False(contexts[1].IsFullySet);
        }

        [Fact]
        public void PropertyNeverSetAnywhere_RemainsUnset()
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

            _sut.Scan(context, compilation);

            Assert.False(context.IsFullySet);
            Assert.Empty(context.RequiredProperties[0].SetLocations);
        }

        [Fact]
        public void ContextAlreadyFullySet_SkipsScanningEntirely()
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
            context.RequiredProperties[0].SetLocations.Add(new SetLocation("Precomputed.cs", 1));

            _sut.Scan(context, compilation);

            Assert.Single(context.RequiredProperties[0].SetLocations);
        }
    }
}
