using Analyser.ForceSetProperties.Emission;
using Analyser.ForceSetProperties.Services;
using Microsoft.CodeAnalysis;

namespace Analyser.ForceSetProperties
{
    [Generator]
    public class ForceSetAnalyser : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(AttributeEmitter.Emit);

            context.RegisterSourceOutput(context.CompilationProvider, Report);
        }

        private static void Report(SourceProductionContext context, Compilation compilation)
        {
            foreach (var diagnostic in new ForceSetPropertiesDriver().GetDiagnostics(compilation))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
