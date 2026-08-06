using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class GetGenericTypeArgumentTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void GenericAttribute_ReturnsTheTypeArgument()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties<DtoModel>]
                public object Create()
                {
                    return new DtoModel();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var attribute = compilation.GetMethod("Factory", "Create")
                .GetForceSetPropertiesAttribute();
            var dtoModel = compilation.GetType("DtoModel");

            var result = _sut.GetGenericTypeArgument(attribute);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void NonGenericAttribute_ReturnsNull()
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
            var create = compilation.GetMethod("Factory", "Create");
            var attribute = create.GetForceSetPropertiesAttribute();

            var result = _sut.GetGenericTypeArgument(attribute);

            Assert.Null(result);
        }
    }
}
