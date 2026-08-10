using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class IsTaskTypeTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void TaskOfT_ReturnsTrue()
        {
            const string source = @"
            using System.Threading.Tasks;

            public class Factory
            {
                public async Task<string> CreateAsync()
                {
                    await Task.Yield();
                    return string.Empty;
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var createAsync = RoslynTestHelper.GetMethod(compilation, "Factory", "CreateAsync");

            var result = _sut.IsTaskType((INamedTypeSymbol)createAsync.ReturnType);

            Assert.True(result);
        }

        [Fact]
        public void ValueTaskOfT_ReturnsTrue()
        {
            const string source = @"
            using System.Threading.Tasks;

            public class Factory
            {
                public async ValueTask<string> CreateAsync()
                {
                    await Task.Yield();
                    return string.Empty;
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var createAsync = RoslynTestHelper.GetMethod(compilation, "Factory", "CreateAsync");

            var result = _sut.IsTaskType((INamedTypeSymbol)createAsync.ReturnType);

            Assert.True(result);
        }

        [Fact]
        public void NonGenericTask_ReturnsTrue()
        {
            const string source = @"
            using System.Threading.Tasks;

            public class Factory
            {
                public async Task RunAsync()
                {
                    await Task.Yield();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var runAsync = RoslynTestHelper.GetMethod(compilation, "Factory", "RunAsync");

            var result = _sut.IsTaskType((INamedTypeSymbol)runAsync.ReturnType);

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

            var result = _sut.IsTaskType((INamedTypeSymbol)mapper.Type);

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

            var result = _sut.IsTaskType(dtoModel);

            Assert.False(result);
        }
    }
}
