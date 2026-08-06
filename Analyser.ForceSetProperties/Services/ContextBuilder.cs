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
                if (IsUnsupportedDestinationType(destinationType))
                {
                    continue;
                }

                var requiredProperties = ResolveRequiredProperties(destinationType, accessContext, compilation);
                contexts.Add(new ContextModel(targetSymbol, targetNode, attributeLocation, destinationType, requiredProperties));
            }

            return contexts;
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
                return explicitTypes;
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

        public List<RequiredProperty> ResolveRequiredProperties(ITypeSymbol destinationType, ISymbol accessContext, Compilation compilation)
        {
            var properties = new List<RequiredProperty>();

            foreach (var member in destinationType.GetMembers().OfType<IPropertySymbol>())
            {
                if (IsRequiredProperty(member, accessContext, compilation))
                {
                    properties.Add(new RequiredProperty(member.Name, member));
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
