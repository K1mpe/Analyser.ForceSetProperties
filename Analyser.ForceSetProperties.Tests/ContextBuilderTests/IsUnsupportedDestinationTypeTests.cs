using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class IsUnsupportedDestinationTypeTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void Void_IsUnsupported()
        {
            const string source = @"
public class Factory
{
    public void Create()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");

            Assert.True(_sut.IsUnsupportedDestinationType(create.ReturnType));
        }

        [Fact]
        public void Object_IsUnsupported()
        {
            const string source = @"
public class Factory
{
    public object Create()
    {
        return new object();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");

            Assert.True(_sut.IsUnsupportedDestinationType(create.ReturnType));
        }

        [Fact]
        public void Dynamic_IsUnsupported()
        {
            const string source = @"
public class Factory
{
    public dynamic Create()
    {
        return new object();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");

            Assert.True(_sut.IsUnsupportedDestinationType(create.ReturnType));
        }

        [Fact]
        public void ConcreteType_IsSupported()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    public DtoModel Create()
    {
        return new DtoModel();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");

            Assert.False(_sut.IsUnsupportedDestinationType(create.ReturnType));
        }
    }
}
