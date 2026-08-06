using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class GetTypesFromConstructorArgumentsTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void PositionalTypeofArgument_ReturnsThatType()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties(typeof(DtoModel))]
                public object Create()
                {
                    return new DtoModel();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");

            var result = _sut.GetTypesFromConstructorArguments(attribute);

            Assert.Equal(new[] { dtoModel }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ParameterlessAttribute_ReturnsEmpty()
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

            var result = _sut.GetTypesFromConstructorArguments(attribute);

            Assert.Empty(result);
        }
    }
}
