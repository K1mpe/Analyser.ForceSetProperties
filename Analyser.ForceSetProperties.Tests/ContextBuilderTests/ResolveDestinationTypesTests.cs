using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class ResolveDestinationTypesTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void PlainAttribute_InfersTypeFromReturnType()
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
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");

            var result = _sut.ResolveDestinationTypes(create, attribute);

            Assert.Equal(new[] { dtoModel }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void GenericAttribute_IsUsedOverReturnTypeInference()
        {
            const string source = @"
            public class DbModel
            {
                public string Name { get; set; }
            }

            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties<DtoModel>]
                public DbModel Create(out DtoModel dto)
                {
                    dto = new DtoModel();
                    return new DbModel();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");

            var result = _sut.ResolveDestinationTypes(create, attribute);

            Assert.Equal(new[] { dtoModel }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ExplicitTypesArgument_TakesPriorityOverGenericAndReturnType()
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
                [ForceSetProperties<DtoModel>(Types = new[] { typeof(OverrideDto) })]
                public DtoModel Create()
                {
                    return new DtoModel();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
            var overrideDto = RoslynTestHelper.GetType(compilation, "OverrideDto");

            var result = _sut.ResolveDestinationTypes(create, attribute);

            Assert.Equal(new[] { overrideDto }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void MultipleExplicitTypes_AreAllReturned()
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

            var result = _sut.ResolveDestinationTypes(map, attribute);

            Assert.Equal(new[] { userDto, roleDto }, result, SymbolEqualityComparer.Default);
        }
    }
}
