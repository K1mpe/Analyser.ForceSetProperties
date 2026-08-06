using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyser.ForceSetProperties.Models
{
    public class ContextModel
    {
        public ContextModel(
            ISymbol targetSymbol,
            SyntaxNode targetNode,
            Location attributeLocation,
            ITypeSymbol destinationType,
            List<RequiredProperty> requiredProperties)
        {
            TargetSymbol = targetSymbol;
            TargetNode = targetNode;
            AttributeLocation = attributeLocation;
            DestinationType = destinationType;
            RequiredProperties = requiredProperties;
        }

        public ISymbol TargetSymbol { get; }

        public SyntaxNode TargetNode { get; }

        public Location AttributeLocation { get; }

        public ITypeSymbol DestinationType { get; }

        public List<RequiredProperty> RequiredProperties { get; }

        public bool IsFullySet => RequiredProperties.All(p => p.IsSet);
    }
}
