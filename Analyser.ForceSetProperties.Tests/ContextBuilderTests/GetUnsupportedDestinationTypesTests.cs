using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class GetUnsupportedDestinationTypesTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void VoidReturningMethod_IsReported()
        {
            const string source = @"
public class Factory
{
    [ForceSetProperties]
    public void Create()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var node = RoslynTestHelper.GetSyntaxNode(create);

            var result = _sut.GetUnsupportedDestinationTypes(create, node, attribute);

            var unsupported = Assert.Single(result);
            Assert.Equal("void", unsupported.Type.ToDisplayString());
        }

        [Fact]
        public void ObjectReturningMethod_IsReported()
        {
            const string source = @"
public class Factory
{
    [ForceSetProperties]
    public object Create()
    {
        return new object();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var node = RoslynTestHelper.GetSyntaxNode(create);

            var result = _sut.GetUnsupportedDestinationTypes(create, node, attribute);

            Assert.Single(result);
        }

        [Fact]
        public void DynamicReturningMethod_IsReported()
        {
            const string source = @"
public class Factory
{
    [ForceSetProperties]
    public dynamic Create()
    {
        return new object();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var node = RoslynTestHelper.GetSyntaxNode(create);

            var result = _sut.GetUnsupportedDestinationTypes(create, node, attribute);

            Assert.Single(result);
        }

        [Fact]
        public void ConcreteReturnType_IsNotReported()
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

            var result = _sut.GetUnsupportedDestinationTypes(create, node, attribute);

            Assert.Empty(result);
        }

        [Fact]
        public void ReportedLocation_IsTheAttributeLocation()
        {
            const string source = @"
public class Factory
{
    [ForceSetProperties]
    public void Create()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var node = RoslynTestHelper.GetSyntaxNode(create);

            var result = _sut.GetUnsupportedDestinationTypes(create, node, attribute);

            var expectedLocation = _sut.ResolveAttributeLocation(attribute, node);
            Assert.Equal(expectedLocation, Assert.Single(result).Location);
        }
    }
}
