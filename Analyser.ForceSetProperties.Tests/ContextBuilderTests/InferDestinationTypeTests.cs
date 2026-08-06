using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class InferDestinationTypeTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void Constructor_ReturnsItsOwnContainingType()
        {
            const string source = @"
            public class DtoModel
            {
                public DtoModel(string name)
                {
                    Name = name;
                }

                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var constructor = RoslynTestHelper.GetConstructor(compilation, "DtoModel");

            var result = _sut.InferDestinationType(constructor);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void Method_ReturnsItsReturnType()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                public DtoModel Create()
                {
                    return new DtoModel();
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");

            var result = _sut.InferDestinationType(create);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void MethodReturningFunc_ReturnsTheUnwrappedResultType()
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
                public Func<DbModel, DtoModel> CreateMapper()
                {
                    return db => new DtoModel { Name = db.Name };
                }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var createMapper = RoslynTestHelper.GetMethod(compilation, "Factory", "CreateMapper");

            var result = _sut.InferDestinationType(createMapper);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void Property_ReturnsItsDeclaredType()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                public DtoModel Current => new DtoModel();
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var current = RoslynTestHelper.GetProperty(compilation, "Factory", "Current");

            var result = _sut.InferDestinationType(current);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ClassSymbol_ReturnsItself()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");

            var result = _sut.InferDestinationType(dtoModel);

            Assert.Equal(dtoModel, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void UnsupportedSymbolKind_ReturnsNull()
        {
            const string source = @"
            public class Factory
            {
                private string _name = ""x"";
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var nameField = RoslynTestHelper.GetField(compilation, "Factory", "_name");

            var result = _sut.InferDestinationType(nameField);

            Assert.Null(result);
        }
    }
}
