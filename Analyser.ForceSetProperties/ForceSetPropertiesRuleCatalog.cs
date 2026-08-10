using System.Collections.Immutable;
using Analyser.ForceSetProperties.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyser.ForceSetProperties
{
    /// <summary>
    /// Advertises this package's full rule catalog to the IDE - Solution Explorer's
    /// Dependencies > Analyzers node, .editorconfig rule-list generation, and similar tooling all
    /// read a <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> property to know what rules a
    /// package can produce (Known-issues.md #7).
    ///
    /// <see cref="ForceSetAnalyser"/> itself is an <see cref="Microsoft.CodeAnalysis.IIncrementalGenerator"/>,
    /// not a <see cref="DiagnosticAnalyzer"/> - that interface has no equivalent concept at all, so
    /// there was nothing for the IDE to enumerate. This class reports nothing on its own
    /// (<see cref="Initialize"/> registers no actions); it exists purely to declare every
    /// <see cref="DiagnosticDescriptor"/> the generator can produce, the same way a normal
    /// <c>DiagnosticAnalyzer</c>-based package (NetAnalyzers, Roslynator, StyleCop.Analyzers, ...)
    /// already does.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ForceSetPropertiesRuleCatalog : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            DiagnosticDescriptors.MissingProperty,
            DiagnosticDescriptors.MissingProperties,
            DiagnosticDescriptors.UnsupportedTarget,
            DiagnosticDescriptors.UnsupportedDestinationType,
            DiagnosticDescriptors.NoPropertiesFound,
            DiagnosticDescriptors.AmbiguousGenericInference,
            DiagnosticDescriptors.ConflictingDestinationTypeSpecification,
            DiagnosticDescriptors.InterfaceDestinationType,
            DiagnosticDescriptors.Validated);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        }
    }
}
