using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class ResolveRequiredPropertiesTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void AllPublicSettableProperties_AreIncluded()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
                public System.DateTime CreatedAt { get; set; }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.ResolveRequiredProperties(dtoModel, mapper, compilation);

            Assert.Equal(new[] { "Name", "CreatedAt" }, result.Select(p => p.Name));
        }

        [Fact]
        public void ReadOnlyProperty_IsExcluded()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
                public string Computed => Name + ""!"";
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.ResolveRequiredProperties(dtoModel, mapper, compilation);

            Assert.Equal(new[] { "Name" }, result.Select(p => p.Name));
        }

        [Fact]
        public void EachRequiredProperty_StartsWithNoSetLocations()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.ResolveRequiredProperties(dtoModel, mapper, compilation);

            var name = Assert.Single(result);
            Assert.Empty(name.SetLocations);
            Assert.False(name.IsSet);
        }

        [Fact]
        public void PrivateSetterFromSameClass_IsIncludedAlongsidePublicOnes()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
                public string Id { get; private set; }

                public static DtoModel CreateInstance(string name, string id)
                {
                    return new DtoModel { Name = name, Id = id };
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");

            var result = _sut.ResolveRequiredProperties(dtoModel, dtoModel, compilation);

            Assert.Equal(new[] { "Name", "Id" }, result.Select(p => p.Name));
        }

        [Fact]
        public void PrivateSetterFromAnotherClass_IsExcluded()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
                public string Id { get; private set; }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.ResolveRequiredProperties(dtoModel, mapper, compilation);

            Assert.Equal(new[] { "Name" }, result.Select(p => p.Name));
        }
    }
}
