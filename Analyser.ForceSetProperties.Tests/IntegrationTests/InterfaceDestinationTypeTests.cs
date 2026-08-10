using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// Known-issues.md #5 - validating against an interface destination type must require every
    /// property declared on the interface (and any interface it extends), matching an assignment on
    /// the concrete instance to the interface member it implements; and must additionally warn
    /// (FSP101) that only the interface's own properties are checked, not the concrete type's.
    /// </summary>
    public class InterfaceDestinationTypeTests
    {
        [Fact]
        public void PropertySetOnlyOnTheConcreteType_SatisfiesTheInterfacesRequirement()
        {
            const string source = """
                public interface IDto
                {
                    string Name { get; set; }
                    int Age { get; set; }
                }

                public class DtoModel : IDto
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties<IDto>]
                    public DtoModel Create()
                    {
                        return new DtoModel { Name = "x", Age = 1 };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            Assert.Contains("Name", validated.GetMessage());
            Assert.Contains("Age", validated.GetMessage());
        }

        [Fact]
        public void PropertyLeftUnsetOnTheConcreteType_IsReportedMissing()
        {
            const string source = """
                public interface IDto
                {
                    string Name { get; set; }
                    int Age { get; set; }
                }

                public class DtoModel : IDto
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties<IDto>]
                    public DtoModel Create()
                    {
                        return new DtoModel { Name = "x" }; // Age left unset
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var missing = Assert.Single(diagnostics, d => d.Id == "FSP001");
            Assert.Contains("Age", missing.GetMessage());
        }

        [Fact]
        public void PropertyDeclaredOnAnExtendedInterface_IsAlsoRequired()
        {
            const string source = """
                public interface IHasName
                {
                    string Name { get; set; }
                }

                public interface IDto : IHasName
                {
                    int Age { get; set; }
                }

                public class DtoModel : IDto
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties<IDto>]
                    public DtoModel Create()
                    {
                        return new DtoModel { Age = 1 }; // Name (from IHasName) left unset
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var missing = Assert.Single(diagnostics, d => d.Id == "FSP001");
            Assert.Contains("Name", missing.GetMessage());
        }

        [Fact]
        public void UsingAnInterfaceAsTheDestinationType_AlwaysAlsoProducesAWarning()
        {
            const string source = """
                public interface IDto
                {
                    string Name { get; set; }
                }

                public class DtoModel : IDto
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties<IDto>]
                    public DtoModel Create()
                    {
                        return new DtoModel { Name = "x" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var warning = Assert.Single(diagnostics, d => d.Id == "FSP101");
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
            Assert.Contains("IDto", warning.GetMessage());
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void NonInterfaceDestinationType_DoesNotProduceTheInterfaceWarning()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new DtoModel { Name = "x" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Id == "FSP101");
        }
    }
}
