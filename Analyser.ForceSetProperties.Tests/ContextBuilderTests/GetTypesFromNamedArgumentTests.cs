using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class GetTypesFromNamedArgumentTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void SingleTypeInTypesArgument_ReturnsThatType()
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

            var result = _sut.GetTypesFromNamedArgument(attribute);

            Assert.Equal(new[] { userDto }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void MultipleTypesInTypesArgument_ReturnsAllOfThemInOrder()
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
                public void Map()
                {
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var map = RoslynTestHelper.GetMethod(compilation, "Factory", "Map");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(map);
            var userDto = RoslynTestHelper.GetType(compilation, "UserDto");
            var roleDto = RoslynTestHelper.GetType(compilation, "RoleDto");

            var result = _sut.GetTypesFromNamedArgument(attribute);

            Assert.Equal(new[] { userDto, roleDto }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void NoTypesArgument_ReturnsEmpty()
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

            var result = _sut.GetTypesFromNamedArgument(attribute);

            Assert.Empty(result);
        }
    }
}
