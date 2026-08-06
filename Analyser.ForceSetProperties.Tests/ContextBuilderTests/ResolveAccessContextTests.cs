using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class ResolveAccessContextTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void ClassTarget_ReturnsTheClassItself()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");

            var accessContext = _sut.ResolveAccessContext(dtoModel);

            Assert.Equal(dtoModel, accessContext, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void MethodTarget_ReturnsContainingType()
        {
            const string source = @"
            public class Factory
            {
                public string Create()
                {
                    return string.Empty;
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var factory = RoslynTestHelper.GetType(compilation, "Factory");
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");

            var accessContext = _sut.ResolveAccessContext(create);

            Assert.Equal(factory, accessContext, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ConstructorTarget_ReturnsContainingType()
        {
            const string source = @"
            public class DtoModel
            {
                public DtoModel(string name)
                {
                    Name = name;
                }

                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var constructor = RoslynTestHelper.GetConstructor(compilation, "DtoModel");

            var accessContext = _sut.ResolveAccessContext(constructor);

            Assert.Equal(dtoModel, accessContext, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void PropertyTarget_ReturnsContainingType()
        {
            const string source = @"
            public class Factory
            {
                public string Name => ""x"";
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var factory = RoslynTestHelper.GetType(compilation, "Factory");
            var nameProperty = RoslynTestHelper.GetProperty(compilation, "Factory", "Name");

            var accessContext = _sut.ResolveAccessContext(nameProperty);

            Assert.Equal(factory, accessContext, SymbolEqualityComparer.Default);
        }
    }
}
