using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class IsFuncTypeTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void FuncType_ReturnsTrue()
        {
            const string source = @"
            using System;

            public class Factory
            {
                public Func<string, string> Mapper => x => x;
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var mapper = RoslynTestHelper.GetProperty(compilation, "Factory", "Mapper");

            var result = _sut.IsFuncType((INamedTypeSymbol)mapper.Type);

            Assert.True(result);
        }

        [Fact]
        public void ActionType_ReturnsFalse()
        {
            const string source = @"
            using System;

            public class Factory
            {
                public Action<string> Logger => x => { };
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var logger = RoslynTestHelper.GetProperty(compilation, "Factory", "Logger");

            var result = _sut.IsFuncType((INamedTypeSymbol)logger.Type);

            Assert.False(result);
        }

        [Fact]
        public void PlainType_ReturnsFalse()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");

            var result = _sut.IsFuncType(dtoModel);

            Assert.False(result);
        }
    }
}
