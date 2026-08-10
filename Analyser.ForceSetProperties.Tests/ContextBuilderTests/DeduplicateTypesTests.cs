using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ContextBuilderTests
{
    public class DeduplicateTypesTests
    {
        private readonly ContextBuilder _sut = new();

        [Fact]
        public void RepeatedType_IsCollapsedToASingleEntry()
        {
            const string source = @"
            public class DtoModel
            {
                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");

            var result = _sut.DeduplicateTypes(new[] { dtoModel, dtoModel });

            Assert.Equal(new[] { dtoModel }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void DistinctTypes_AreAllKept()
        {
            const string source = @"
            public class UserDto
            {
                public string Name { get; set; }
            }

            public class RoleDto
            {
                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var userDto = RoslynTestHelper.GetType(compilation, "UserDto");
            var roleDto = RoslynTestHelper.GetType(compilation, "RoleDto");

            var result = _sut.DeduplicateTypes(new[] { userDto, roleDto });

            Assert.Equal(new ITypeSymbol[] { userDto, roleDto }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void RepeatedTypeAmongDistinctOnes_KeepsFirstOccurrenceOrderAndDropsLaterDuplicates()
        {
            const string source = @"
            public class UserDto
            {
                public string Name { get; set; }
            }

            public class RoleDto
            {
                public string Name { get; set; }
            }";
            var compilation = RoslynTestHelper.Compile(source);
            var userDto = RoslynTestHelper.GetType(compilation, "UserDto");
            var roleDto = RoslynTestHelper.GetType(compilation, "RoleDto");

            var result = _sut.DeduplicateTypes(new[] { userDto, roleDto, userDto });

            Assert.Equal(new ITypeSymbol[] { userDto, roleDto }, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void EmptyList_ReturnsEmpty()
        {
            var result = _sut.DeduplicateTypes(System.Array.Empty<ITypeSymbol>());

            Assert.Empty(result);
        }
    }
}
