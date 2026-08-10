using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "Example: Safe DTO evolution" - a mapping that's valid today must be updated the
    /// moment a new property is added. Also covers the "multi-return methods" supported scenario.
    /// </summary>
    public class SafeDtoEvolutionTests
    {
        [Fact]
        public void BeforeAddingAProperty_TheExistingMappingValidates()
        {
            const string source = """
                public class DtoModel
                {
                    [ForceSetProperties]
                    public DtoModel(string name)
                    {
                        Name = name;
                    }

                    public string Name { get; set; }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void AfterAddingAProperty_TheSameMappingFailsUntilUpdated()
        {
            const string source = """
                public class DtoModel
                {
                    [ForceSetProperties]
                    public DtoModel(string name)
                    {
                        Name = name;
                    }

                    public string Name { get; set; }
                    public System.DateTime CreatedAt { get; set; }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.GetMessage().Contains("CreatedAt"));
        }

        [Fact]
        public void MultipleReturnStatements_AreEachValidatedIndependently()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create(bool useDefault)
                    {
                        if (useDefault)
                        {
                            return new DtoModel { Name = "default" };
                        }

                        return new DtoModel { Name = "custom" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }
    }
}
