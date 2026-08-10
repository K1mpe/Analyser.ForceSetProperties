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

        [Fact]
        public void PropertyDeclaredOnBaseClass_IsIncludedAlongsideDirectProperties()
        {
            const string source = @"
            public class BaseModel
            {
                public string Id { get; set; }
            }

            public class DtoModel : BaseModel
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

            Assert.Equal(new[] { "Name", "Id" }, result.Select(p => p.Name));
        }

        [Fact]
        public void PropertyDeclaredSeveralLevelsUpTheBaseClassChain_IsStillIncluded()
        {
            const string source = @"
            public class GrandBaseModel
            {
                public string Id { get; set; }
            }

            public class BaseModel : GrandBaseModel
            {
                public string CreatedBy { get; set; }
            }

            public class DtoModel : BaseModel
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

            Assert.Equal(new[] { "Name", "CreatedBy", "Id" }, result.Select(p => p.Name));
        }

        [Fact]
        public void OverriddenProperty_IsNotDuplicated_WithTheBaseDeclaration()
        {
            const string source = @"
            public class BaseModel
            {
                public virtual string Name { get; set; }
            }

            public class DtoModel : BaseModel
            {
                public override string Name { get; set; }
                public string Id { get; set; }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.ResolveRequiredProperties(dtoModel, mapper, compilation);

            Assert.Equal(new[] { "Name", "Id" }, result.Select(p => p.Name));
        }

        [Fact]
        public void PrivateSetterOnBaseClass_IsExcluded_WhenAccessedFromOutsideTheBaseClass()
        {
            const string source = @"
            public class BaseModel
            {
                public string Id { get; private set; }
            }

            public class DtoModel : BaseModel
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

            Assert.Equal(new[] { "Name" }, result.Select(p => p.Name));
        }

        [Fact]
        public void PrivateSetterOnBaseClass_IsIncluded_WhenAccessedFromWithinTheBaseClass()
        {
            const string source = @"
            public class BaseModel
            {
                public string Id { get; private set; }
            }

            public class DtoModel : BaseModel
            {
                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var baseModel = RoslynTestHelper.GetType(compilation, "BaseModel");

            var result = _sut.ResolveRequiredProperties(dtoModel, baseModel, compilation);

            Assert.Equal(new[] { "Name", "Id" }, result.Select(p => p.Name));
        }

        [Fact]
        public void InterfaceDestinationType_IncludesItsOwnDirectlyDeclaredProperties()
        {
            const string source = @"
            public interface IDto
            {
                string Name { get; set; }
                int Age { get; set; }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var idto = RoslynTestHelper.GetType(compilation, "IDto");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.ResolveRequiredProperties(idto, mapper, compilation);

            Assert.Equal(new[] { "Name", "Age" }, result.Select(p => p.Name));
        }

        [Fact]
        public void InterfaceDestinationType_AlsoIncludesPropertiesFromInterfacesItExtends()
        {
            const string source = @"
            public interface IHasName
            {
                string Name { get; set; }
            }

            public interface IDto : IHasName
            {
                int Age { get; set; }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var idto = RoslynTestHelper.GetType(compilation, "IDto");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.ResolveRequiredProperties(idto, mapper, compilation);

            Assert.Equal(new[] { "Age", "Name" }, result.Select(p => p.Name));
        }

        [Fact]
        public void InterfaceDestinationType_IncludesPropertiesFromInterfacesExtendedSeveralLevelsUp()
        {
            const string source = @"
            public interface IHasId
            {
                string Id { get; set; }
            }

            public interface IHasName : IHasId
            {
                string Name { get; set; }
            }

            public interface IDto : IHasName
            {
                int Age { get; set; }
            }

            public class Mapper
            {
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var idto = RoslynTestHelper.GetType(compilation, "IDto");
            var mapper = RoslynTestHelper.GetType(compilation, "Mapper");

            var result = _sut.ResolveRequiredProperties(idto, mapper, compilation);

            Assert.Equal(new[] { "Age", "Name", "Id" }, result.Select(p => p.Name));
        }
    }
}
