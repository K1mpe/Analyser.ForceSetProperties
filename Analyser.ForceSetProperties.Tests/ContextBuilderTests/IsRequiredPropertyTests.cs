using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class IsRequiredPropertyTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void PublicGetSet_IsRequired()
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
            var name = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.IsRequiredProperty(name, mapper, compilation);

            Assert.True(result);
        }

        [Fact]
        public void PublicGetInit_IsRequired()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; init; }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var name = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.IsRequiredProperty(name, mapper, compilation);

            Assert.True(result);
        }

        [Fact]
        public void GetOnlyProperty_IsNotRequired()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name => ""x"";
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var name = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.IsRequiredProperty(name, mapper, compilation);

            Assert.False(result);
        }

        [Fact]
        public void StaticProperty_IsNotRequired()
        {
            const string source = @"
            public class DtoModel
            {
                public static string Name { get; set; }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var name = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.IsRequiredProperty(name, mapper, compilation);

            Assert.False(result);
        }

        [Fact]
        public void Indexer_IsNotRequired()
        {
            const string source = @"
            public class DtoModel
            {
                public string this[int index]
                {
                    get => ""x"";
                    set { }
                }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var indexer = (IPropertySymbol)dtoModel.GetMembers()[0];
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.IsRequiredProperty(indexer, mapper, compilation);

            Assert.False(result);
        }

        [Fact]
        public void PrivateSetter_IsRequiredWhenAccessedFromTheSameClass()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; private set; }

                public static DtoModel CreateInstance(string name)
                {
                    return new DtoModel { Name = name };
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var name = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");

            var result = _sut.IsRequiredProperty(name, dtoModel, compilation);

            Assert.True(result);
        }

        [Fact]
        public void PrivateSetter_IsNotRequiredWhenAccessedFromAnotherClass()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; private set; }
            }

            public class Mapper
            {
                public DtoModel Create()
                {
                    return new DtoModel();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var name = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.IsRequiredProperty(name, mapper, compilation);

            Assert.False(result);
        }
    }
}
