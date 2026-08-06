using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class GetExplicitTypesTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void NamedTypesArgument_IsReturned()
        {
            const string source = @"
            public class UserDto
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties(Types = new[] { typeof(UserDto) })]
                public object Create()
                {
                    return new UserDto();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var userDto = RoslynTestHelper.GetType(compilation, "UserDto");

            var result = _sut.GetExplicitTypes(attribute);

            Assert.Equal(new[] { userDto }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void PositionalTypeofArgument_IsReturnedWhenNoNamedArgument()
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

            var result = _sut.GetExplicitTypes(attribute);

            Assert.Equal(new[] { dtoModel }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void NamedArgument_TakesPriorityOverPositionalArgument()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class OverrideDto
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties(typeof(DtoModel), Types = new[] { typeof(OverrideDto) })]
                public object Create()
                {
                    return new OverrideDto();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var overrideDto = RoslynTestHelper.GetType(compilation, "OverrideDto");

            var result = _sut.GetExplicitTypes(attribute);

            Assert.Equal(new[] { overrideDto }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void NoExplicitTypes_ReturnsEmpty()
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

            var result = _sut.GetExplicitTypes(attribute);

            Assert.Empty(result);
        }
    }
}
