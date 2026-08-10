using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "Tracing into called methods and constructors" - following ordinary calls and
    /// constructor chaining to find assignments that don't happen directly in the annotated member.
    /// </summary>
    public class TracingIntoCalledMembersTests
    {
        [Fact]
        public void DelegatingToAnotherMethod_IsTracedAndCountsAsValid()
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
                    [ForceSetProperties]
                    public DtoModel Create(DbModel db)
                    {
                        return Map(db);
                    }

                    private static DtoModel Map(DbModel db)
                    {
                        return new DtoModel
                        {
                            Name = db.Name,
                            CreatedAt = db.CreatedAt,
                            UpdatedAt = db.UpdatedAt
                        };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void DelegatingToAMethodThatMissesAProperty_StillFails()
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
                    [ForceSetProperties]
                    public DtoModel Create(DbModel db)
                    {
                        return Map(db);
                    }

                    private static DtoModel Map(DbModel db)
                    {
                        return new DtoModel
                        {
                            Name = db.Name,
                            CreatedAt = db.CreatedAt
                        };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.GetMessage().Contains("UpdatedAt"));
        }

        [Fact]
        public void ConstructorChaining_IsTracedAndCountsAsValid()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; set; }
                    public string Id { get; set; }

                    public DtoModel()
                    {
                        Id = System.Guid.NewGuid().ToString();
                    }

                    [ForceSetProperties]
                    public DtoModel(string name) : this()
                    {
                        Name = name;
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }
    }
}
