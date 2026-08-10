using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class IsAmbiguousGenericInferenceTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void GenericTypeReachedByInference_WithNoExplicitTypeSpecified_ReturnsTrue()
        {
            const string source = @"
            using System.Collections.Generic;

            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties]
                public List<DtoModel> Create() => new List<DtoModel>();
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            var result = _sut.IsAmbiguousGenericInference(attribute, create.ReturnType);

            Assert.True(result);
        }

        [Fact]
        public void GenericTypeSpecifiedViaTypesProperty_ReturnsFalse()
        {
            const string source = @"
            using System.Collections.Generic;

            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties(Types = new[] { typeof(List<DtoModel>) })]
                public List<DtoModel> Create() => new List<DtoModel>();
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            var result = _sut.IsAmbiguousGenericInference(attribute, create.ReturnType);

            Assert.False(result);
        }

        [Fact]
        public void GenericTypeSpecifiedViaGenericAttributeArgument_ReturnsFalse()
        {
            const string source = @"
            using System.Collections.Generic;

            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties<List<DtoModel>>]
                public List<DtoModel> Create() => new List<DtoModel>();
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            var result = _sut.IsAmbiguousGenericInference(attribute, create.ReturnType);

            Assert.False(result);
        }

        [Fact]
        public void NonGenericTypeReachedByInference_ReturnsFalse()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                [ForceSetProperties]
                public DtoModel Create() => new DtoModel();
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);

            var result = _sut.IsAmbiguousGenericInference(attribute, create.ReturnType);

            Assert.False(result);
        }
    }
}
