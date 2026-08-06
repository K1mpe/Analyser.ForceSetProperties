using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class IsExpressionTypeTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void ExpressionOfFunc_ReturnsTrue()
        {
            const string source = @"
            using System;
            using System.Linq.Expressions;

            public class Factory
            {
                public Expression<Func<string, string>> Mapper => x => x;
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var mapper = RoslynTestHelper.GetProperty(compilation, "Factory", "Mapper");

            var result = _sut.IsExpressionType((INamedTypeSymbol)mapper.Type);

            Assert.True(result);
        }

        [Fact]
        public void FuncType_ReturnsFalse()
        {
            const string source = @"
            using System;

            public class Factory
            {
                public Func<string, string> Mapper => x => x;
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var mapper = RoslynTestHelper.GetProperty(compilation, "Factory", "Mapper");

            var result = _sut.IsExpressionType((INamedTypeSymbol)mapper.Type);

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

            var result = _sut.IsExpressionType(dtoModel);

            Assert.False(result);
        }
    }
}
