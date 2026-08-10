using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "What counts as 'set'" - the three assignment shapes the analyzer recognizes.
    /// </summary>
    public class WhatCountsAsSetTests
    {
        private const string DtoModel = """
            public class DtoModel
            {
                public string Name { get; set; }
                public System.DateTime CreatedAt { get; set; }
                public System.DateTime UpdatedAt { get; set; }
            }
            """;

        [Fact]
        public void ObjectInitializer_CountsAsSet()
        {
            var source = $$"""
                {{DtoModel}}

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

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void AssignmentAfterCreation_CountsAsSet()
        {
            var source = $$"""
                {{DtoModel}}

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        var dto = new DtoModel();

                        dto.Name = "x";
                        dto.CreatedAt = System.DateTime.UtcNow;
                        dto.UpdatedAt = System.DateTime.UtcNow;

                        return dto;
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void ObjectInitializerViaTargetTypedNew_CountsAsSet()
        {
            var source = $$"""
                {{DtoModel}}

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new()
                        {
                            Name = "x",
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
        public void ConstructorAssignments_CountAsSet()
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
                    [ForceSetProperties]
                    public DtoModel(DbModel db)
                    {
                        Name = db.Name;
                        CreatedAt = db.CreatedAt;
                        UpdatedAt = db.UpdatedAt;
                    }

                    public string Name { get; set; }
                    public System.DateTime CreatedAt { get; set; }
                    public System.DateTime UpdatedAt { get; set; }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }
    }
}
