using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Analyser.ForceSetProperties.Emission;
using Analyser.ForceSetProperties.Models;

namespace Analyser.ForceSetProperties.Services
{
    public class ContextBuilder
    {
        public IReadOnlyList<ContextModel> Build(ISymbol targetSymbol, SyntaxNode targetNode, AttributeData attribute, Compilation compilation)
        {
            var attributeLocation = ResolveAttributeLocation(attribute, targetNode);
            var destinationTypes = ResolveDestinationTypes(targetSymbol, attribute);
            var accessContext = ResolveAccessContext(targetSymbol);

            var contexts = new List<ContextModel>(destinationTypes.Count);
            foreach (var destinationType in destinationTypes)
            {
                if (IsUnsupportedDestinationType(destinationType) || IsAmbiguousGenericInference(attribute, destinationType))
                {
                    continue;
                }

                var requiredProperties = ResolveRequiredProperties(destinationType, accessContext, compilation);
                contexts.Add(new ContextModel(targetSymbol, targetNode, attributeLocation, destinationType, requiredProperties));
            }

            return contexts;
        }

        /// <summary>
        /// Builds a context for an already-resolved destination type, bypassing AttributeData-based
        /// resolution entirely - used for attribute usages discovered syntactically (see
        /// ForceSetPropertiesTargetFinder.ClassifyGeneric), where there's no AttributeData to derive
        /// a destination type from. Callers must check <see cref="IsUnsupportedDestinationType"/>
        /// themselves first; this does not check it.
        /// </summary>
        public ContextModel BuildSingle(ISymbol targetSymbol, SyntaxNode targetNode, Location attributeLocation, ITypeSymbol destinationType, Compilation compilation)
        {
            var accessContext = ResolveAccessContext(targetSymbol);
            var requiredProperties = ResolveRequiredProperties(destinationType, accessContext, compilation);
            return new ContextModel(targetSymbol, targetNode, attributeLocation, destinationType, requiredProperties);
        }

        public IReadOnlyList<(ITypeSymbol Type, Location Location)> GetUnsupportedDestinationTypes(ISymbol targetSymbol, SyntaxNode targetNode, AttributeData attribute)
        {
            var attributeLocation = ResolveAttributeLocation(attribute, targetNode);

            return ResolveDestinationTypes(targetSymbol, attribute)
                .Where(IsUnsupportedDestinationType)
                .Select(type => (type, attributeLocation))
                .ToArray();
        }

        public bool IsUnsupportedDestinationType(ITypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_Void
                || type.SpecialType == SpecialType.System_Object
                || type.TypeKind == TypeKind.Dynamic;
        }

        /// <summary>
        /// Detects issue #10: a generic type reached only through implicit return-type/property-type
        /// inference (no explicit <c>Types =</c>, <c>typeof(...)</c> constructor argument, or generic
        /// <c>ForceSetProperties&lt;T&gt;</c> attribute argument) - after the known-safe structural
        /// unwraps (<see cref="UnwrapDelegateReturnType"/>) have already had their chance to unwrap it.
        /// Explicitly stating the destination type by any of those three mechanisms is never flagged,
        /// no matter how generic the resulting type is - only inference landing on an unresolved
        /// generic type is ambiguous enough to reject.
        /// </summary>
        public bool IsAmbiguousGenericInference(AttributeData attribute, ITypeSymbol destinationType)
        {
            if (destinationType is not INamedTypeSymbol { IsGenericType: true })
            {
                return false;
            }

            return GetExplicitTypes(attribute).Count == 0 && GetGenericTypeArgument(attribute) == null;
        }

        public IReadOnlyList<(ITypeSymbol Type, Location Location)> GetAmbiguousInferredDestinationTypes(ISymbol targetSymbol, SyntaxNode targetNode, AttributeData attribute)
        {
            var attributeLocation = ResolveAttributeLocation(attribute, targetNode);

            return ResolveDestinationTypes(targetSymbol, attribute)
                .Where(type => IsAmbiguousGenericInference(attribute, type))
                .Select(type => (type, attributeLocation))
                .ToArray();
        }

        public ISymbol ResolveAccessContext(ISymbol targetSymbol)
        {
            if (targetSymbol is INamedTypeSymbol namedType)
            {
                return namedType;
            }

            return (ISymbol?)targetSymbol.ContainingType ?? targetSymbol.ContainingAssembly;
        }

        public Location ResolveAttributeLocation(AttributeData attribute, SyntaxNode fallbackNode)
        {
            var syntaxReference = attribute.ApplicationSyntaxReference;
            if (syntaxReference != null)
            {
                return syntaxReference.GetSyntax().GetLocation();
            }

            return fallbackNode.GetLocation();
        }

        public IReadOnlyList<ITypeSymbol> ResolveDestinationTypes(ISymbol targetSymbol, AttributeData attribute)
        {
            var explicitTypes = GetExplicitTypes(attribute);
            if (explicitTypes.Count > 0)
            {
                return DeduplicateTypes(explicitTypes);
            }

            var genericType = GetGenericTypeArgument(attribute);
            if (genericType != null)
            {
                return new[] { genericType };
            }

            var inferredType = InferDestinationType(targetSymbol);
            if (inferredType != null)
            {
                return new[] { inferredType };
            }

            return Array.Empty<ITypeSymbol>();
        }

        /// <summary>
        /// De-duplicates by symbol equality (Known-issues.md #14) - the same type listed twice in
        /// `Types = [...]` (or, in principle, `typeof(X), typeof(X)` as separate constructor
        /// arguments) would otherwise produce one <see cref="ContextModel"/>, and therefore one
        /// diagnostic, per repeated occurrence rather than per distinct type.
        /// </summary>
        public IReadOnlyList<ITypeSymbol> DeduplicateTypes(IReadOnlyList<ITypeSymbol> types)
        {
            var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var result = new List<ITypeSymbol>(types.Count);

            foreach (var type in types)
            {
                if (seen.Add(type))
                {
                    result.Add(type);
                }
            }

            return result;
        }

        /// <summary>
        /// True when more than one of the three destination-type sources - the named `Types = [...]`
        /// property, one or more `typeof(...)` constructor arguments, and the generic `&lt;T&gt;`
        /// argument - produced a result on the same attribute application (Known-issues.md #11).
        /// These don't merge into "validate both"; using more than one is a conflict, not a priority
        /// order to silently pick a winner from. Covers repro 2 (constructor argument + `Types`)
        /// directly, and also repro 1 (generic argument + `Types`) for the manual/non-incremental
        /// discovery path - <see cref="GetGenericTypeArgument"/> resolves the generic form's type
        /// argument from <see cref="AttributeData"/> just fine there, unlike the incremental
        /// generator's own pass, which needs <c>ClassifyGeneric</c>'s separate syntactic check
        /// instead (the generic form's usages never reach this method at all in that pipeline).
        /// </summary>
        public bool HasConflictingTypeSpecification(AttributeData attribute)
        {
            var sourceCount = 0;

            if (GetTypesFromNamedArgument(attribute).Count > 0)
            {
                sourceCount++;
            }

            if (GetTypesFromConstructorArguments(attribute).Count > 0)
            {
                sourceCount++;
            }

            if (GetGenericTypeArgument(attribute) != null)
            {
                sourceCount++;
            }

            return sourceCount > 1;
        }

        public IReadOnlyList<ITypeSymbol> GetExplicitTypes(AttributeData attribute)
        {
            var fromNamedArgument = GetTypesFromNamedArgument(attribute);
            if (fromNamedArgument.Count > 0)
            {
                return fromNamedArgument;
            }

            return GetTypesFromConstructorArguments(attribute);
        }

        public IReadOnlyList<ITypeSymbol> GetTypesFromNamedArgument(AttributeData attribute)
        {
            var namedArgument = attribute.NamedArguments
                .FirstOrDefault(a => a.Key == AttributeEmitter.TypesPropertyName);

            if (namedArgument.Key == null || namedArgument.Value.Kind != TypedConstantKind.Array)
            {
                return Array.Empty<ITypeSymbol>();
            }

            return namedArgument.Value.Values
                .Select(v => v.Value as ITypeSymbol)
                .Where(t => t != null)
                .Select(t => t!)
                .ToArray();
        }

        public IReadOnlyList<ITypeSymbol> GetTypesFromConstructorArguments(AttributeData attribute)
        {
            var types = new List<ITypeSymbol>();

            foreach (var argument in attribute.ConstructorArguments)
            {
                if (argument.Kind == TypedConstantKind.Type && argument.Value is ITypeSymbol singleType)
                {
                    types.Add(singleType);
                }
                else if (argument.Kind == TypedConstantKind.Array)
                {
                    types.AddRange(argument.Values
                        .Select(v => v.Value as ITypeSymbol)
                        .Where(t => t != null)
                        .Select(t => t!));
                }
            }

            return types;
        }

        public ITypeSymbol? GetGenericTypeArgument(AttributeData attribute)
        {
            if (attribute.AttributeClass is INamedTypeSymbol { IsGenericType: true } namedType
                && namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }

            return null;
        }

        public ITypeSymbol? InferDestinationType(ISymbol targetSymbol)
        {
            switch (targetSymbol)
            {
                case IMethodSymbol { MethodKind: MethodKind.Constructor } constructor:
                    return constructor.ContainingType;

                case IMethodSymbol method:
                    return UnwrapDelegateReturnType(method.ReturnType);

                case IPropertySymbol property:
                    return UnwrapDelegateReturnType(property.Type);

                case INamedTypeSymbol type:
                    return type;

                default:
                    return null;
            }
        }

        public ITypeSymbol UnwrapDelegateReturnType(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol { IsGenericType: true } namedType)
            {
                if (IsExpressionType(namedType) && namedType.TypeArguments.Length == 1)
                {
                    return UnwrapDelegateReturnType(namedType.TypeArguments[0]);
                }

                if (IsFuncType(namedType) && namedType.TypeArguments.Length > 0)
                {
                    return namedType.TypeArguments[namedType.TypeArguments.Length - 1];
                }

                if (IsTaskType(namedType) && namedType.TypeArguments.Length == 1)
                {
                    return UnwrapDelegateReturnType(namedType.TypeArguments[0]);
                }
            }

            return type;
        }

        public bool IsExpressionType(INamedTypeSymbol type)
        {
            return type.ConstructedFrom.Name == "Expression"
                && type.ConstructedFrom.ContainingNamespace?.ToDisplayString() == "System.Linq.Expressions";
        }

        public bool IsFuncType(INamedTypeSymbol type)
        {
            return type.ConstructedFrom.Name == "Func"
                && type.ConstructedFrom.ContainingNamespace?.ToDisplayString() == "System";
        }

        public bool IsTaskType(INamedTypeSymbol type)
        {
            return (type.ConstructedFrom.Name == "Task" || type.ConstructedFrom.Name == "ValueTask")
                && type.ConstructedFrom.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";
        }

        /// <summary>
        /// The types whose own (non-inherited) members contribute to a destination type's required
        /// properties: the base class chain for a class/struct, or the interface itself plus every
        /// interface it extends (<see cref="INamedTypeSymbol.AllInterfaces"/>) for an interface -
        /// interfaces have no <see cref="ITypeSymbol.BaseType"/> to walk instead.
        /// </summary>
        public IEnumerable<ITypeSymbol> GetTypeHierarchy(ITypeSymbol destinationType)
        {
            if (destinationType is INamedTypeSymbol { TypeKind: TypeKind.Interface } interfaceType)
            {
                yield return interfaceType;

                foreach (var extendedInterface in interfaceType.AllInterfaces)
                {
                    yield return extendedInterface;
                }

                yield break;
            }

            for (var type = destinationType; type != null; type = type.BaseType)
            {
                yield return type;
            }
        }

        public List<RequiredProperty> ResolveRequiredProperties(ITypeSymbol destinationType, ISymbol accessContext, Compilation compilation)
        {
            var properties = new List<RequiredProperty>();
            var overriddenProperties = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var typeHierarchy = GetTypeHierarchy(destinationType).ToList();

            foreach (var type in typeHierarchy)
            {
                foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
                {
                    for (var overridden = member.OverriddenProperty; overridden != null; overridden = overridden.OverriddenProperty)
                    {
                        overriddenProperties.Add(overridden);
                    }
                }
            }

            foreach (var type in typeHierarchy)
            {
                foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
                {
                    if (overriddenProperties.Contains(member))
                    {
                        continue;
                    }

                    if (IsRequiredProperty(member, accessContext, compilation))
                    {
                        properties.Add(new RequiredProperty(member.Name, member));
                    }
                }
            }

            return properties;
        }

        public bool IsRequiredProperty(IPropertySymbol property, ISymbol accessContext, Compilation compilation)
        {
            if (property.IsStatic || property.IsIndexer)
            {
                return false;
            }

            if (!compilation.IsSymbolAccessibleWithin(property, accessContext))
            {
                return false;
            }

            var setter = property.SetMethod;
            if (setter == null)
            {
                return false;
            }

            return compilation.IsSymbolAccessibleWithin(setter, accessContext);
        }
    }
}
