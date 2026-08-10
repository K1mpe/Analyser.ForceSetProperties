using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "Specifying Target Types" - the four ways the destination type can be determined.
    /// </summary>
    public class TargetTypeSpecificationTests
    {
        [Fact]
        public void ReturnTypeInference_IsUsedByDefault()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; set; }
                    public System.DateTime CreatedAt { get; set; }
                    public System.DateTime UpdatedAt { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new DtoModel
                        {
                            Name = "Test",
                            CreatedAt = System.DateTime.UtcNow,
                            UpdatedAt = System.DateTime.UtcNow
                        };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void GenericAttribute_OverridesReturnTypeInference_ForAnOutParameter()
        {
            const string source = """
                public class DbModel
                {
                    public string Name { get; set; }
                    public System.DateTime CreatedAt { get; set; }
                    public System.DateTime UpdatedAt { get; set; }
                }

                public class DtoModel
                {
                    public string Name { get; set; }
                    public System.DateTime CreatedAt { get; set; }
                    public System.DateTime UpdatedAt { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties<DtoModel>]
                    public DbModel SomeFunction(DbModel source, out DtoModel dto)
                    {
                        dto = new DtoModel
                        {
                            Name = source.Name,
                            CreatedAt = source.CreatedAt,
                            UpdatedAt = source.UpdatedAt
                        };

                        return source;
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201" && d.GetMessage().Contains("DtoModel"));
        }

        [Fact]
        public void ExplicitTypeofConstructorArgument_SelectsTheDestinationType()
        {
            const string source = """
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
                    [ForceSetProperties(typeof(DtoModel))]
                    public DbModel SomeFunction(DbModel source, out DtoModel dto)
                    {
                        dto = new DtoModel { Name = source.Name };
                        return source;
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201" && d.GetMessage().Contains("DtoModel"));
        }

        [Fact]
        public void ExplicitTypesProperty_SelectsASingleDestinationType()
        {
            const string source = """
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
                    [ForceSetProperties(Types = [typeof(DtoModel)])]
                    public DbModel SomeFunction(DbModel source, out DtoModel dto)
                    {
                        dto = new DtoModel { Name = source.Name };
                        return source;
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201" && d.GetMessage().Contains("DtoModel"));
        }

        [Fact]
        public void ExplicitTypesProperty_ValidatesEveryListedTypeInTheSameMethod()
        {
            const string source = """
                public class User { public int Id { get; set; } public string Name { get; set; } }
                public class Role { public int Id { get; set; } public string Name { get; set; } }
                public class UserDto { public int Id { get; set; } public string Name { get; set; } }
                public class RoleDto { public int Id { get; set; } public string Name { get; set; } }

                public class Mapper
                {
                    [ForceSetProperties(Types = [typeof(UserDto), typeof(RoleDto)])]
                    public void Map(User user, Role role, out UserDto userDto, out RoleDto roleDto)
                    {
                        userDto = new UserDto { Id = user.Id, Name = user.Name };
                        roleDto = new RoleDto { Id = role.Id, Name = role.Name };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(2, diagnostics.Count(d => d.Id == "FSP201"));
            Assert.Contains(diagnostics, d => d.Id == "FSP201" && d.GetMessage().Contains("UserDto"));
            Assert.Contains(diagnostics, d => d.Id == "FSP201" && d.GetMessage().Contains("RoleDto"));
        }

        [Theory]
        [InlineData("[ForceSetProperties]")]
        [InlineData("[ForceSetProperties<DtoModel>]")]
        [InlineData("[ForceSetProperties(typeof(DtoModel))]")]
        [InlineData("[ForceSetProperties(Types = [typeof(DtoModel)])]")]
        public void AllAttributeForms_AreTreatedEquivalently(string attributeUsage)
        {
            var source = $$"""
                public class DtoModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    {{attributeUsage}}
                    public DtoModel Create()
                    {
                        return new DtoModel { Name = "x" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void GenericArgumentCombinedWithNamedTypes_RaisesFSP007InsteadOfSilentlyPickingOne()
        {
            // Known-issues.md #11, repro 1.
            const string source = """
                public class TypeA { public string Name { get; set; } }
                public class TypeB { public string Name { get; set; } }

                public class Factory
                {
                    [ForceSetProperties<TypeA>(Types = new[] { typeof(TypeB) })]
                    public static object Create()
                    {
                        return new TypeB { Name = "x" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var error = Assert.Single(diagnostics, d => d.Id == "FSP007");
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.DoesNotContain(diagnostics, d => d.Id == "FSP201" || d.Id == "FSP001" || d.Id == "FSP002");
        }

        [Fact]
        public void ConstructorArgumentCombinedWithNamedTypes_RaisesFSP007InsteadOfSilentlyPickingOne()
        {
            // Known-issues.md #11, repro 2.
            const string source = """
                public class EmptyDto { }
                public interface IDto { string Name { get; set; } }

                public class Factory
                {
                    [ForceSetProperties(typeof(EmptyDto), Types = new System.Type[] { typeof(IDto) })]
                    public static object Create()
                    {
                        return new EmptyDto();
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var error = Assert.Single(diagnostics, d => d.Id == "FSP007");
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.DoesNotContain(diagnostics, d => d.Id == "FSP201" || d.Id == "FSP001" || d.Id == "FSP002" || d.Id == "FSP005");
        }

        [Fact]
        public void SameTypeListedTwiceInTypesProperty_ProducesExactlyOneDiagnosticInstead()
        {
            // Known-issues.md #14.
            const string source = """
                public class Dto
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties(Types = new System.Type[] { typeof(Dto), typeof(Dto) })]
                    public static Dto Create()
                    {
                        return new Dto { Name = "x" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Single(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void SameTypeListedTwiceInTypesProperty_StillReportsAGenuinelyMissingPropertyExactlyOnce()
        {
            // Known-issues.md #14 - the doc's own note that a missing property would otherwise be
            // duplicated too, not just the passing FSP201 case.
            const string source = """
                public class Dto
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties(Types = new System.Type[] { typeof(Dto), typeof(Dto) })]
                    public static Dto Create()
                    {
                        return new Dto();
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Single(diagnostics, d => d.Id == "FSP001");
        }
    }
}
