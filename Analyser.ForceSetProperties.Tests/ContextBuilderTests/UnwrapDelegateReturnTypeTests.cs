using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class UnwrapDelegateReturnTypeTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void PlainType_IsReturnedUnchanged()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");

            var result = _sut.UnwrapDelegateReturnType(dtoModel);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void FuncType_ReturnsItsLastTypeArgument()
        {
            const string source = @"
            using System;

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
                public Func<DbModel, DtoModel> Mapper => db => new DtoModel();
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var mapper = RoslynTestHelper.GetProperty(compilation, "Factory", "Mapper");

            var result = _sut.UnwrapDelegateReturnType(mapper.Type);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void FuncTypeWithMultipleParameters_ReturnsTheFinalTypeArgument()
        {
            const string source = @"
            using System;

            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                public Func<int, string, bool, DtoModel> Mapper => (a, b, c) => new DtoModel();
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var mapper = RoslynTestHelper.GetProperty(compilation, "Factory", "Mapper");

            var result = _sut.UnwrapDelegateReturnType(mapper.Type);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ExpressionOfFunc_ReturnsTheUnwrappedResultType()
        {
            const string source = @"
            using System;
            using System.Linq.Expressions;

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
                public Expression<Func<DbModel, DtoModel>> Mapper => db => new DtoModel();
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var mapper = RoslynTestHelper.GetProperty(compilation, "Factory", "Mapper");

            var result = _sut.UnwrapDelegateReturnType(mapper.Type);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ActionType_IsReturnedUnchanged()
        {
            const string source = @"
            using System;

            public class Factory
            {
                public Action<string> Logger => text => { };
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var logger = RoslynTestHelper.GetProperty(compilation, "Factory", "Logger");

            var result = _sut.UnwrapDelegateReturnType(logger.Type);

            Assert.Equal(logger.Type, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void TaskOfT_ReturnsItsTypeArgument()
        {
            const string source = @"
            using System.Threading.Tasks;

            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                public async Task<DtoModel> CreateAsync()
                {
                    await Task.Yield();
                    return new DtoModel();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var createAsync = RoslynTestHelper.GetMethod(compilation, "Factory", "CreateAsync");

            var result = _sut.UnwrapDelegateReturnType(createAsync.ReturnType);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ValueTaskOfT_ReturnsItsTypeArgument()
        {
            const string source = @"
            using System.Threading.Tasks;

            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                public async ValueTask<DtoModel> CreateAsync()
                {
                    await Task.Yield();
                    return new DtoModel();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var createAsync = RoslynTestHelper.GetMethod(compilation, "Factory", "CreateAsync");

            var result = _sut.UnwrapDelegateReturnType(createAsync.ReturnType);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void NonGenericTask_IsReturnedUnchanged()
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

            var result = _sut.UnwrapDelegateReturnType(runAsync.ReturnType);

            Assert.Equal(runAsync.ReturnType, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ListOfT_IsReturnedUnchanged()
        {
            const string source = @"
            using System.Collections.Generic;

            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                public List<DtoModel> Create() => new List<DtoModel>();
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");

            var result = _sut.UnwrapDelegateReturnType(create.ReturnType);

            Assert.Equal(create.ReturnType, result, SymbolEqualityComparer.Default);
        }
    }
}
