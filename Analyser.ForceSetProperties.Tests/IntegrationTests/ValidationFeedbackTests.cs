using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "Validation feedback" - the informational FSP201 diagnostic reported when a target
    /// passes validation, in its short one-line form and its detailed per-property breakdown.
    /// </summary>
    public class ValidationFeedbackTests
    {
        [Fact]
        public void EveryPropertySetDirectly_ProducesTheShortOneLineSummary()
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
                            Name = "x",
                            CreatedAt = System.DateTime.UtcNow,
                            UpdatedAt = System.DateTime.UtcNow
                        };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            Assert.Equal(DiagnosticSeverity.Info, validated.Severity);
            Assert.Equal("ForceSetProperties validated DtoModel: Name, CreatedAt, UpdatedAt", validated.GetMessage());
        }

        [Fact]
        public void APropertyFoundViaTracing_ProducesTheDetailedPerPropertyBreakdown()
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

            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            var message = validated.GetMessage();

            Assert.StartsWith("Type checked: DtoModel", message);
            Assert.Contains("(via Map)", message);
        }

        [Fact]
        public void WhenTheCompilationHasNoRealFilePaths_TheBreakdownOmitsPathsInsteadOfCrashing()
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
                    [ForceSetProperties]
                    public DtoModel Create(DbModel db)
                    {
                        return Map(db);
                    }

                    private static DtoModel Map(DbModel db)
                    {
                        return new DtoModel { Name = db.Name };
                    }
                }
                """;

            var compilation = RoslynTestHelper.CompileWithoutFilePath(source);
            var diagnostics = RoslynTestHelper.GetDiagnostics(compilation);

            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            var message = validated.GetMessage();

            Assert.StartsWith("Type checked: DtoModel", message);
            Assert.Contains("Name: line", message);
            Assert.Contains("(via Map)", message);
        }
    }
}
