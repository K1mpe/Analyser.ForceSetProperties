using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.Extensions;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class BuildTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void SingleDestinationType_ProducesOneContextWithAllRequiredProperties()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
                public System.DateTime CreatedAt { get; set; }
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
            compilation.CreateContextModel("Factory", "Create")
                .Single()
                .HasDestinationType("DtoModel", compilation)
                .ContainsProperties("Name", "CreatedAt");
        }

        [Fact]
        public void MultipleDestinationTypes_ProducesOneContextPerType()
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
                    userDto = new UserDto();
                    roleDto = new RoleDto();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var contexts = compilation.CreateContextModel("Factory", "Map");

            Assert.Equal(2, contexts.Count);
            Assert.Equal(compilation.GetType("UserDto"), contexts[0].DestinationType, SymbolEqualityComparer.Default);
            Assert.Equal(compilation.GetType("RoleDto"), contexts[1].DestinationType, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void EveryContext_SharesTheSameTargetAndAttributeLocation()
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
                    userDto = new UserDto();
                    roleDto = new RoleDto();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var map = RoslynTestHelper.GetMethod(compilation, "Factory", "Map");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(map);
            var node = RoslynTestHelper.GetSyntaxNode(map);

            var contexts = _sut.Build(map, node, attribute, compilation);

            Assert.All(contexts, context =>
            {
                Assert.Equal(map, context.TargetSymbol, SymbolEqualityComparer.Default);
                Assert.Equal(node, context.TargetNode);
            });
            Assert.Equal(contexts[0].AttributeLocation, contexts[1].AttributeLocation);
        }

        [Fact]
        public void VoidReturningMethod_ProducesNoContexts()
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

            var contexts = compilation.CreateContextModel("Factory", "Create");

            Assert.Empty(contexts);
        }

        [Fact]
        public void RequiredProperties_AreNotScannedYet()
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

            var contexts = compilation.CreateContextModel("Factory", "Create");

            var context = Assert.Single(contexts);
            var name = Assert.Single(context.RequiredProperties);
            Assert.False(name.IsSet);
        }
    }
}
