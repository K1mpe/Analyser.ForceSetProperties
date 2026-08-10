using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class GetAmbiguousInferredDestinationTypesTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void GenericReturnTypeInferredWithNoExplicitOverride_IsReported()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    [ForceSetProperties]
    public System.Collections.Generic.List<DtoModel> Create()
    {
        return new System.Collections.Generic.List<DtoModel>();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var node = RoslynTestHelper.GetSyntaxNode(create);

            var result = _sut.GetAmbiguousInferredDestinationTypes(create, node, attribute);

            var ambiguous = Assert.Single(result);
            Assert.Equal("System.Collections.Generic.List<DtoModel>", ambiguous.Type.ToDisplayString());
        }

        [Fact]
        public void GenericReturnTypeWithExplicitTypesOverride_IsNotReported()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    [ForceSetProperties(Types = new[] { typeof(System.Collections.Generic.List<DtoModel>) })]
    public System.Collections.Generic.List<DtoModel> Create()
    {
        return new System.Collections.Generic.List<DtoModel>();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var node = RoslynTestHelper.GetSyntaxNode(create);

            var result = _sut.GetAmbiguousInferredDestinationTypes(create, node, attribute);

            Assert.Empty(result);
        }

        [Fact]
        public void NonGenericReturnType_IsNotReported()
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
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var node = RoslynTestHelper.GetSyntaxNode(create);

            var result = _sut.GetAmbiguousInferredDestinationTypes(create, node, attribute);

            Assert.Empty(result);
        }

        [Fact]
        public void TaskOfDtoModel_IsNotReported_SinceItIsUnwrappedBeforeInferenceLandsOnIt()
        {
            const string source = @"
using System.Threading.Tasks;

public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    [ForceSetProperties]
    public async Task<DtoModel> CreateAsync()
    {
        await Task.Yield();
        return new DtoModel();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var createAsync = RoslynTestHelper.GetMethod(compilation, "Factory", "CreateAsync");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(createAsync);
            var node = RoslynTestHelper.GetSyntaxNode(createAsync);

            var result = _sut.GetAmbiguousInferredDestinationTypes(createAsync, node, attribute);

            Assert.Empty(result);
        }

        [Fact]
        public void ReportedLocation_IsTheAttributeLocation()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    [ForceSetProperties]
    public System.Collections.Generic.List<DtoModel> Create()
    {
        return new System.Collections.Generic.List<DtoModel>();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var node = RoslynTestHelper.GetSyntaxNode(create);

            var result = _sut.GetAmbiguousInferredDestinationTypes(create, node, attribute);

            var expectedLocation = _sut.ResolveAttributeLocation(attribute, node);
            Assert.Equal(expectedLocation, Assert.Single(result).Location);
        }
    }
}
