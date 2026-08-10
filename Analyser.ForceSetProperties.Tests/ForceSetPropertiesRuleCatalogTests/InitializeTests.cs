using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetPropertiesRuleCatalogTests
{
    /// <summary>
    /// Runs the actual DiagnosticAnalyzer through Compilation.WithAnalyzers, the analyzer-side
    /// equivalent of ForceSetAnalyserTests/InitializeTests.cs running the generator through
    /// CSharpGeneratorDriver - confirms Initialize doesn't throw and that this purely-declarative
    /// analyzer never itself reports a diagnostic (that's still entirely ForceSetAnalyser's job).
    /// </summary>
    public class InitializeTests
    {
        [Fact]
        public async Task ReportsNoDiagnosticsOnItsOwn()
        {
            var trustedPlatformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(System.IO.Path.PathSeparator);
            var references = trustedPlatformAssemblies
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .ToArray();

            var tree = CSharpSyntaxTree.ParseText("public class DtoModel { public string Name { get; set; } }");
            var compilation = CSharpCompilation.Create(
                "ScratchAssembly",
                new[] { tree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ForceSetPropertiesRuleCatalog()));

            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();

            Assert.Empty(diagnostics);
        }
    }
}
