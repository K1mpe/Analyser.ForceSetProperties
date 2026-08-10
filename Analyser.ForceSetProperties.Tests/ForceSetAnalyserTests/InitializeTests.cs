using System.Collections.Immutable;
using System.Linq;
using Analyser.ForceSetProperties.Emission;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetAnalyserTests
{
    /// <summary>
    /// Runs the actual IIncrementalGenerator through CSharpGeneratorDriver, unlike the rest of the
    /// suite which exercises the Compilation-based service methods directly. This is what catches
    /// regressions in the incremental pipeline wiring itself (ForAttributeWithMetadataName usage,
    /// Collect/Combine) and in the language-version gating of the generic attribute form that a
    /// Compilation-only test can't see.
    /// </summary>
    public class InitializeTests
    {
        private const string Source = """
            using Analyser.ForceSetProperties;

            namespace Scratch
            {
                public class Model
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                [ForceSetProperties]
                public class NotSupported { }

                public static class Service
                {
                    [ForceSetProperties]
                    public static Model FullyValid(Model source)
                    {
                        return new Model { Id = source.Id, Name = source.Name };
                    }

                    [ForceSetProperties]
                    public static Model MissingOne(Model source)
                    {
                        return new Model { Id = source.Id };
                    }
                }
            }
            """;

        private const string SourceWithGenericUsage = """
            using Analyser.ForceSetProperties;

            namespace Scratch
            {
                public class Model
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                public static class Service
                {
                    [ForceSetProperties<Model>]
                    public static object FullyValid(Model source)
                    {
                        return new Model { Id = source.Id, Name = source.Name };
                    }

                    [ForceSetProperties<Model>]
                    public static object MissingOne(Model source)
                    {
                        return new Model { Id = source.Id };
                    }
                }
            }
            """;

        private const string SourceWithGenericUsageOnUnsupportedPlacement = """
            using Analyser.ForceSetProperties;

            namespace Scratch
            {
                public class Model
                {
                    public int Id { get; set; }
                }

                public class Holder
                {
                    [ForceSetProperties<Model>]
                    public int Field;
                }
            }
            """;

        private const string SourceWithMethodTargetedRecordPrimaryConstructor = """
            using Analyser.ForceSetProperties;

            namespace Scratch
            {
                [method: ForceSetProperties]
                public record Person(string Name, int Age)
                {
                    public Person() : this(default, default) { }
                }
            }
            """;

        private const string SourceWithGenericUsageConflictingWithNamedTypes = """
            using Analyser.ForceSetProperties;

            namespace Scratch
            {
                public class TypeA
                {
                    public string Name { get; set; } = "";
                }

                public class TypeB
                {
                    public string Name { get; set; } = "";
                }

                public static class Service
                {
                    [ForceSetProperties<TypeA>(Types = new[] { typeof(TypeB) })]
                    public static object Create()
                    {
                        return new TypeB { Name = "x" };
                    }
                }
            }
            """;

        private static (Compilation OutputCompilation, ImmutableArray<Diagnostic> GeneratorDiagnostics) Run(string source, LanguageVersion languageVersion)
        {
            var trustedPlatformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(System.IO.Path.PathSeparator);
            var references = trustedPlatformAssemblies
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .ToArray();

            var parseOptions = new CSharpParseOptions(languageVersion);
            var tree = CSharpSyntaxTree.ParseText(source, parseOptions);
            var compilation = CSharpCompilation.Create(
                "ScratchAssembly",
                new[] { tree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var driver = CSharpGeneratorDriver.Create(
                new[] { new ForceSetAnalyser().AsSourceGenerator() },
                parseOptions: parseOptions);

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
            return (outputCompilation, generatorDiagnostics);
        }

        [Fact]
        public void ProducesDiagnosticsForEveryUsageKind()
        {
            var (_, diagnostics) = Run(Source, LanguageVersion.Latest);

            Assert.Contains(diagnostics, d => d.Id == "FSP003" && d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(1, diagnostics.Count(d => d.Id == "FSP201" && d.Severity == DiagnosticSeverity.Info));
        }

        [Fact]
        public void GenericAttributeForm_IsValidatedOnASupportedLanguageVersion()
        {
            var (outputCompilation, diagnostics) = Run(SourceWithGenericUsage, LanguageVersion.Latest);

            Assert.NotNull(outputCompilation.GetTypeByMetadataName(AttributeEmitter.GenericMetadataName));
            Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

            Assert.Contains(diagnostics, d => d.Id == "FSP201" && d.GetMessage().Contains("Id, Name"));
            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.Severity == DiagnosticSeverity.Error && d.GetMessage().Contains("Name"));
        }

        [Fact]
        public void GenericAttributeForm_OnAnUnsupportedPlacement_RaisesFSP003()
        {
            var (_, diagnostics) = Run(SourceWithGenericUsageOnUnsupportedPlacement, LanguageVersion.Latest);

            Assert.Contains(diagnostics, d => d.Id == "FSP003" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void GenericAttributeForm_IsOmittedOnAnUnsupportedLanguageVersion_WithoutBreakingTheNonGenericForm()
        {
            var (outputCompilation, diagnostics) = Run(Source, LanguageVersion.CSharp9);

            Assert.Null(outputCompilation.GetTypeByMetadataName(AttributeEmitter.GenericMetadataName));
            Assert.NotNull(outputCompilation.GetTypeByMetadataName(AttributeEmitter.NonGenericMetadataName));
            Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

            Assert.Contains(diagnostics, d => d.Id == "FSP003" && d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void MethodTargetedRecordPrimaryConstructor_IsValidatedThroughTheRealIncrementalPipeline()
        {
            // Known-issues.md #4: unlike the other cases in this file, this specifically exercises
            // GeneratorAttributeSyntaxContext.TargetSymbol/TargetNode for a `method:`-targeted
            // attribute on a RecordDeclarationSyntax - behavior the manual, Compilation-based
            // FindTargets path can't observe, since it never goes through ForAttributeWithMetadataName.
            var (_, diagnostics) = Run(SourceWithMethodTargetedRecordPrimaryConstructor, LanguageVersion.Latest);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            Assert.Contains("Name", validated.GetMessage());
            Assert.Contains("Age", validated.GetMessage());
        }

        [Fact]
        public void GenericAttributeFormConflictingWithNamedTypes_RaisesFSP007ThroughTheRealIncrementalPipeline()
        {
            // Known-issues.md #11, repro 1 - the generic form is discovered exclusively through
            // ClassifyGeneric's own syntactic path in the real incremental pipeline (see issue #9),
            // never through ForAttributeWithMetadataName/Classify, so the conflict has to be caught
            // there directly rather than in the AttributeData-based checks the manual driver path uses.
            var (_, diagnostics) = Run(SourceWithGenericUsageConflictingWithNamedTypes, LanguageVersion.Latest);

            var error = Assert.Single(diagnostics, d => d.Id == "FSP007");
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.DoesNotContain(diagnostics, d => d.Id == "FSP201" || d.Id == "FSP001" || d.Id == "FSP002");
        }
    }
}
