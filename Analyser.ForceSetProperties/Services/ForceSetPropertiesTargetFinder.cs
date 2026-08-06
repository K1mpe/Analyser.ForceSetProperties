using System.Collections.Generic;
using System.Linq;
using Analyser.ForceSetProperties.Emission;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyser.ForceSetProperties.Services
{
    public class ForceSetPropertiesTargetFinder
    {
        public IEnumerable<(ISymbol Symbol, SyntaxNode Node, AttributeData Attribute)> FindTargets(Compilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);

                foreach (var node in tree.GetRoot().DescendantNodes().Where(IsSupportedDeclaration))
                {
                    var symbol = semanticModel.GetDeclaredSymbol(node);
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

        public bool IsSupportedDeclaration(SyntaxNode? node)
        {
            return node is ConstructorDeclarationSyntax
                || node is MethodDeclarationSyntax
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
            return IsSupportedDeclaration(attribute.Parent?.Parent);
        }
    }
}
