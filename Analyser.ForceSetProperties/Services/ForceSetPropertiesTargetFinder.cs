using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Analyser.ForceSetProperties.Emission;
using Analyser.ForceSetProperties.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyser.ForceSetProperties.Services
{
    public class ForceSetPropertiesTargetFinder
    {
        /// <summary>
        /// Incrementally-cached equivalent of <see cref="FindUnsupportedAttributeUsages"/> combined
        /// with <see cref="FindTargets"/>, built on ForAttributeWithMetadataName (non-generic form)
        /// and a syntactic search (generic form) so Roslyn can reuse cached results for declarations
        /// whose syntax hasn't changed, instead of re-walking every syntax tree and re-resolving every
        /// declaration's symbol on every edit.
        ///
        /// The generic ForceSetPropertiesAttribute&lt;T&gt; form is emitted conditionally (it needs C#
        /// 11 to even declare, so its emission depends on ParseOptionsProvider) - and content added
        /// through anything other than RegisterPostInitializationOutput is never visible to this
        /// generator's own providers within the same compiler pass (verified directly: a type added via
        /// a regular RegisterSourceOutput never resolves through this generator's own CompilationProvider
        /// or SyntaxProvider, only in the final merged output compilation handed back to the caller).
        /// So ForAttributeWithMetadataName can never find the generic form's usages - it needs the
        /// generic attribute's own type to resolve, and that's exactly what's never visible here.
        ///
        /// The workaround: discover the generic form syntactically instead, the same way
        /// <see cref="IsForceSetPropertiesAttribute"/> already does for the non-incremental path - by
        /// simple name, without needing the attribute's own type to resolve at all. What actually needs
        /// to resolve is the *type argument* inside &lt;...&gt; (the consumer's own type, e.g.
        /// <c>EmptyDto</c> in <c>ForceSetProperties&lt;EmptyDto&gt;</c>) and the decorated declaration's
        /// symbol - both are the consumer's own code and always resolvable, regardless of whether our
        /// attribute type is.
        ///
        /// `[method: ForceSetProperties]` on a record's primary constructor (Known-issues.md #4) needs
        /// the same kind of workaround, verified directly: `ForAttributeWithMetadataName`'s predicate
        /// never even fires for it - the metadata-name search apparently doesn't associate a `method:`
        /// target specifier on a record declaration with the primary constructor the way it does for a
        /// `field:`/`property:` specifier on an auto-property. So this is discovered syntactically too,
        /// by simple name plus <see cref="IsMethodTargetedOnRecordWithPrimaryConstructor"/> (itself
        /// purely syntactic), resolving the primary constructor symbol directly instead of relying on
        /// <c>ctx.TargetSymbol</c> ever being handed to us for it.
        /// </summary>
        public static IncrementalValuesProvider<AttributeUsage> CreateIncrementalProvider(SyntaxValueProvider syntaxProvider)
        {
            var nonGeneric = syntaxProvider.ForAttributeWithMetadataName(
                AttributeEmitter.NonGenericMetadataName,
                predicate: static (_, _) => true,
                transform: static (ctx, _) => Classify(ctx));

            var generic = syntaxProvider.CreateSyntaxProvider(IsGenericAttributeSyntax, static (ctx, _) => ClassifyGeneric(ctx))
                .Where(static usage => usage != null)
                .Select(static (usage, _) => usage!);

            var methodTargetedRecordConstructor = syntaxProvider
                .CreateSyntaxProvider(IsMethodTargetedRecordConstructorAttributeSyntax, static (ctx, _) => ClassifyMethodTargetedRecordConstructor(ctx))
                .Where(static usage => usage != null)
                .Select(static (usage, _) => usage!);

            return nonGeneric.Collect()
                .Combine(generic.Collect())
                .Combine(methodTargetedRecordConstructor.Collect())
                .SelectMany(static (found, _) => found.Left.Left.AddRange(found.Left.Right).AddRange(found.Right));
        }

        private static AttributeUsage Classify(GeneratorAttributeSyntaxContext ctx)
        {
            var finder = new ForceSetPropertiesTargetFinder();
            var attribute = ctx.Attributes[0];

            if (!finder.IsSupportedDeclaration(ctx.TargetNode))
            {
                var location = new ContextBuilder().ResolveAttributeLocation(attribute, ctx.TargetNode);
                return AttributeUsage.Unsupported(location);
            }

            return AttributeUsage.Supported(ctx.TargetSymbol, ctx.TargetNode, attribute);
        }

        private static bool IsMethodTargetedRecordConstructorAttributeSyntax(SyntaxNode node, CancellationToken cancellationToken)
        {
            return node is AttributeSyntax { Name: IdentifierNameSyntax identifier } attribute
                && (identifier.Identifier.Text == AttributeEmitter.AttributeClassName
                    || identifier.Identifier.Text == AttributeEmitter.ShortAttributeClassName)
                && new ForceSetPropertiesTargetFinder().IsMethodTargetedOnRecordWithPrimaryConstructor(attribute);
        }

        // Only reachable for nodes matching IsMethodTargetedRecordConstructorAttributeSyntax, so
        // attribute.Parent.Parent is always a RecordDeclarationSyntax with a primary constructor here.
        private static AttributeUsage? ClassifyMethodTargetedRecordConstructor(GeneratorSyntaxContext ctx)
        {
            var attribute = (AttributeSyntax)ctx.Node;
            var record = (RecordDeclarationSyntax)attribute.Parent!.Parent!;

            var finder = new ForceSetPropertiesTargetFinder();
            var constructor = finder.GetPrimaryConstructor(record, ctx.SemanticModel);
            if (constructor == null)
            {
                return null;
            }

            var attributeData = finder.GetForceSetPropertiesAttributes(constructor)
                .FirstOrDefault(a => a.ApplicationSyntaxReference?.GetSyntax() == attribute);

            if (attributeData == null)
            {
                return null;
            }

            return AttributeUsage.Supported(constructor, record, attributeData);
        }

        private static bool IsGenericAttributeSyntax(SyntaxNode node, CancellationToken cancellationToken)
        {
            return node is AttributeSyntax { Name: GenericNameSyntax generic }
                && (generic.Identifier.Text == AttributeEmitter.AttributeClassName
                    || generic.Identifier.Text == AttributeEmitter.ShortAttributeClassName);
        }

        // Only reachable for nodes matching IsGenericAttributeSyntax, so Name is always a
        // GenericNameSyntax and Parent is always an AttributeListSyntax here.
        private static AttributeUsage? ClassifyGeneric(GeneratorSyntaxContext ctx)
        {
            var attribute = (AttributeSyntax)ctx.Node;
            var genericName = (GenericNameSyntax)attribute.Name;
            var typeArgument = genericName.TypeArgumentList.Arguments.FirstOrDefault();
            if (typeArgument == null)
            {
                return null;
            }

            var finder = new ForceSetPropertiesTargetFinder();
            var declaredNode = attribute.Parent?.Parent;

            if (!finder.IsSupportedDeclaration(declaredNode))
            {
                return AttributeUsage.Unsupported(attribute.GetLocation());
            }

            if (HasTypesArgument(attribute))
            {
                return AttributeUsage.ConflictingTypeSpecification(attribute.GetLocation());
            }

            var targetSymbol = ctx.SemanticModel.GetDeclaredSymbol(declaredNode!);
            var destinationType = ctx.SemanticModel.GetTypeInfo(typeArgument).Type;

            if (targetSymbol == null || destinationType == null)
            {
                return null;
            }

            return AttributeUsage.SupportedGeneric(targetSymbol, declaredNode!, destinationType, attribute.GetLocation());
        }

        /// <summary>
        /// True when the generic form is combined with a named `Types = [...]` argument on the same
        /// attribute application (Known-issues.md #11, repro 1) - purely syntactic, since the
        /// generic form's parameterless constructor makes a positional `typeof(...)` argument
        /// alongside it impossible (that's CS1729, a compile error independent of this analyzer).
        /// </summary>
        private static bool HasTypesArgument(AttributeSyntax attribute)
        {
            return attribute.ArgumentList?.Arguments.Any(a => a.NameEquals?.Name.Identifier.Text == AttributeEmitter.TypesPropertyName) == true;
        }

        public IEnumerable<(ISymbol Symbol, SyntaxNode Node, AttributeData Attribute)> FindTargets(Compilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);

                foreach (var node in tree.GetRoot().DescendantNodes())
                {
                    ISymbol? symbol;

                    if (IsSupportedDeclaration(node))
                    {
                        symbol = semanticModel.GetDeclaredSymbol(node);
                    }
                    else if (node is RecordDeclarationSyntax record)
                    {
                        // [method: ForceSetProperties] on a record's primary constructor
                        // (Known-issues.md #4) - `node` here is the RecordDeclarationSyntax itself,
                        // not a ConstructorDeclarationSyntax, so IsSupportedDeclaration never matches
                        // it; the attribute is attached to the primary constructor's own symbol
                        // regardless, so that's what has to be searched for it.
                        symbol = GetPrimaryConstructor(record, semanticModel);
                    }
                    else
                    {
                        continue;
                    }

                    if (symbol == null)
                    {
                        continue;
                    }

                    foreach (var attribute in GetForceSetPropertiesAttributes(symbol))
                    {
                        yield return (symbol, node, attribute);
                    }
                }
            }
        }

        /// <summary>
        /// The record's own primary constructor - the one whose declaring syntax is the record
        /// declaration itself, rather than a dedicated constructor declaration - or null if the
        /// record has no primary constructor (no parameter list) at all.
        /// </summary>
        public IMethodSymbol? GetPrimaryConstructor(RecordDeclarationSyntax record, SemanticModel semanticModel)
        {
            if (record.ParameterList == null)
            {
                return null;
            }

            if (semanticModel.GetDeclaredSymbol(record) is not INamedTypeSymbol type)
            {
                return null;
            }

            return type.Constructors.FirstOrDefault(c => c.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() == record);
        }

        public bool IsSupportedDeclaration(SyntaxNode? node)
        {
            if (node is MethodDeclarationSyntax method)
            {
                // An interface method signature or an `abstract` method declaration is still a
                // MethodDeclarationSyntax, but has nothing for the scanner to check yet (Known-issues.md
                // #12) - a ConstructorDeclarationSyntax always has a body in a supported placement, so
                // this check only needs to apply here.
                return method.Body != null || method.ExpressionBody != null;
            }

            return node is ConstructorDeclarationSyntax
                || node is PropertyDeclarationSyntax;
        }

        public IEnumerable<AttributeData> GetForceSetPropertiesAttributes(ISymbol symbol)
        {
            return symbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == AttributeEmitter.AttributeClassName);
        }

        public IEnumerable<Location> FindUnsupportedAttributeUsages(Compilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);

                foreach (var attribute in tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>())
                {
                    if (IsForceSetPropertiesAttribute(attribute, semanticModel) && !IsOnSupportedDeclaration(attribute))
                    {
                        yield return attribute.GetLocation();
                    }
                }
            }
        }

        public bool IsForceSetPropertiesAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            return semanticModel.GetTypeInfo(attribute).Type?.Name == AttributeEmitter.AttributeClassName;
        }

        public bool IsOnSupportedDeclaration(AttributeSyntax attribute)
        {
            return IsSupportedDeclaration(attribute.Parent?.Parent)
                || IsMethodTargetedOnRecordWithPrimaryConstructor(attribute);
        }

        /// <summary>
        /// True for `[method: ...]` attached to a record declaration that actually has a primary
        /// constructor for `method:` to target (Known-issues.md #4) - purely syntactic, the same way
        /// <see cref="IsGenericAttributeSyntax"/> avoids needing semantic resolution.
        /// </summary>
        public bool IsMethodTargetedOnRecordWithPrimaryConstructor(AttributeSyntax attribute)
        {
            return attribute.Parent is AttributeListSyntax { Target: { } target } attributeList
                && target.Identifier.Text == "method"
                && attributeList.Parent is RecordDeclarationSyntax { ParameterList: not null };
        }
    }
}
