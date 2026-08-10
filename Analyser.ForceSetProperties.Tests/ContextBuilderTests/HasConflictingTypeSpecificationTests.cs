using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class HasConflictingTypeSpecificationTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void GenericArgumentAndNamedTypes_IsAConflict()
        {
            const string source = @"
            public class TypeA
            {
                public string Name { get; set; }
            }

            public class TypeB
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties<TypeA>(Types = new[] { typeof(TypeB) })]
                public static object Create()
                {
                    return new TypeB { Name = ""x"" };
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            Assert.True(_sut.HasConflictingTypeSpecification(attribute));
        }

        [Fact]
        public void ConstructorArgumentAndNamedTypes_IsAConflict()
        {
            const string source = @"
            public class EmptyDto
            {
            }

            public interface IDto
            {
                string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties(typeof(EmptyDto), Types = new System.Type[] { typeof(IDto) })]
                public static object Create()
                {
                    return new EmptyDto();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            Assert.True(_sut.HasConflictingTypeSpecification(attribute));
        }

        [Fact]
        public void GenericArgumentOnly_IsNotAConflict()
        {
            const string source = @"
            public class TypeA
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties<TypeA>]
                public static object Create()
                {
                    return new TypeA { Name = ""x"" };
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            Assert.False(_sut.HasConflictingTypeSpecification(attribute));
        }

        [Fact]
        public void ConstructorArgumentOnly_IsNotAConflict()
        {
            const string source = @"
            public class TypeA
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties(typeof(TypeA))]
                public static object Create()
                {
                    return new TypeA { Name = ""x"" };
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            Assert.False(_sut.HasConflictingTypeSpecification(attribute));
        }

        [Fact]
        public void NamedTypesOnly_IsNotAConflict()
        {
            const string source = @"
            public class TypeA
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties(Types = new[] { typeof(TypeA) })]
                public static object Create()
                {
                    return new TypeA { Name = ""x"" };
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            Assert.False(_sut.HasConflictingTypeSpecification(attribute));
        }

        [Fact]
        public void NoExplicitSource_IsNotAConflict()
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
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            Assert.False(_sut.HasConflictingTypeSpecification(attribute));
        }
    }
}
