using System.Collections.Immutable;
using Analyser.ForceSetProperties.Emission;
using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Analyser.ForceSetProperties
{
    [Generator]
    public class ForceSetAnalyser : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // The non-generic attribute is always safe to declare on any language version, so it goes
            // through RegisterPostInitializationOutput - the only mechanism whose output is visible to
            // this generator's own CompilationProvider/SyntaxProvider within the same compiler pass,
            // which is what lets ForAttributeWithMetadataName below find its usages at all.
            context.RegisterPostInitializationOutput(static ctx =>
                ctx.AddSource(AttributeEmitter.AttributeHintName, AttributeEmitter.NonGenericSource));

            // ForceSetPropertiesAttribute<T> needs C# 11 to even declare, so it can't go through
            // post-init (which has no access to ParseOptions to gate on) - it's added conditionally
            // here instead, based on the consumer's actual language version. This barely ever reruns
            // in practice, since a project's language version essentially never changes mid-session.
            //
            // Since only RegisterPostInitializationOutput content is visible within this same pass,
            // ForAttributeWithMetadataName can never resolve this type here - so usages of the generic
            // form are discovered syntactically instead (see
            // ForceSetPropertiesTargetFinder.CreateIncrementalProvider), which doesn't need the
            // attribute's own type to resolve at all.
            var supportsGenericAttributes = context.ParseOptionsProvider.Select(static (options, _) =>
                options is CSharpParseOptions csharpOptions
                && csharpOptions.LanguageVersion.MapSpecifiedToEffectiveVersion() >= LanguageVersion.CSharp11);

            context.RegisterSourceOutput(supportsGenericAttributes, static (spc, supportsGeneric) =>
            {
                if (supportsGeneric)
                {
                    spc.AddSource(AttributeEmitter.GenericAttributeHintName, AttributeEmitter.GenericSource);
                }
            });

            var usages = ForceSetPropertiesTargetFinder.CreateIncrementalProvider(context.SyntaxProvider);

            // Compilation is combined in last, and unavoidably, since validating an annotated member
            // can require tracing into constructors/methods it calls anywhere in the compilation - so
            // this callback still reruns on every edit. What incrementality buys here is that `usages`
            // is cheap: Roslyn reuses cached entries for declarations whose syntax didn't change,
            // instead of re-walking and re-resolving every declaration in the whole project each time.
            context.RegisterSourceOutput(usages.Collect().Combine(context.CompilationProvider), Report);
        }

        private static void Report(SourceProductionContext context, (ImmutableArray<AttributeUsage> Usages, Compilation Compilation) input)
        {
            if (input.Usages.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var diagnostic in new ForceSetPropertiesDriver().GetDiagnostics(input.Compilation, input.Usages))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
