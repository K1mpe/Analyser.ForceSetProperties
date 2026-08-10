using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "Non-Goals" - things the analyzer intentionally does not enforce.
    /// </summary>
    public class NonGoalsTests
    {
        [Fact]
        public void SettersDoNotNeedToBePublic()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; internal set; }
                    public string Id { get; protected set; }

                    [ForceSetProperties]
                    public static DtoModel Create(string name, string id)
                    {
                        return new DtoModel { Name = name, Id = id };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void ConstructorParametersAreNotRequired()
        {
            const string source = """
                public class DtoModel
                {
                    [ForceSetProperties]
                    public DtoModel()
                    {
                        Id = System.Guid.NewGuid().ToString();
                        Name = "Unnamed";
                    }

                    public string Id { get; set; }
                    public string Name { get; set; }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void NullabilityIsNotEnforced()
        {
            const string source = """
                #nullable enable
                public class DtoModel
                {
                    public string Name { get; set; } = "";
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new DtoModel { Name = null };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            // Assigning null is a compiler nullable *warning*, not something this analyzer cares
            // about - Name was still assigned, so it's still fully validated.
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void AssignmentOrderIsNotEnforced()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; set; }
                    public string Id { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new DtoModel
                        {
                            Id = "1",
                            Name = "x"
                        };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void NestedObjectInitializationIsNotRecursivelyEnforced()
        {
            const string source = """
                public class Address
                {
                    public string Street { get; set; }
                    public string City { get; set; }
                }

                public class DtoModel
                {
                    public string Name { get; set; }
                    public Address Address { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new DtoModel
                        {
                            Name = "x",
                            // Street/City are left unset - the analyzer only checks that DtoModel's
                            // own Address property was assigned, not what's inside the assigned value.
                            Address = new Address()
                        };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }
    }
}
