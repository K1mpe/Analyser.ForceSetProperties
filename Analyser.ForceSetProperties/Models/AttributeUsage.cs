using Microsoft.CodeAnalysis;

namespace Analyser.ForceSetProperties.Models
{
    /// <summary>
    /// A single `[ForceSetProperties]` (or `[ForceSetProperties&lt;T&gt;]`) usage, discovered via
    /// the incremental attribute search before any compilation-wide validation runs.
    ///
    /// The non-generic form carries a real <see cref="AttributeData"/> (found via
    /// ForAttributeWithMetadataName, which needs the attribute type to be resolvable). The generic
    /// form is found syntactically instead - its own type is never resolvable within this
    /// generator's own pass - so it carries an already-resolved destination type and attribute
    /// location directly, with no <see cref="AttributeData"/> at all.
    /// </summary>
    public class AttributeUsage
    {
        private AttributeUsage(bool isSupported, ISymbol? symbol, SyntaxNode? node, AttributeData? attribute, ITypeSymbol? genericDestinationType, Location? attributeLocation, Location? unsupportedLocation, Location? conflictingTypeSpecificationLocation)
        {
            IsSupported = isSupported;
            Symbol = symbol;
            Node = node;
            Attribute = attribute;
            GenericDestinationType = genericDestinationType;
            AttributeLocation = attributeLocation;
            UnsupportedLocation = unsupportedLocation;
            ConflictingTypeSpecificationLocation = conflictingTypeSpecificationLocation;
        }

        public static AttributeUsage Supported(ISymbol symbol, SyntaxNode node, AttributeData attribute)
            => new(true, symbol, node, attribute, null, null, null, null);

        public static AttributeUsage SupportedGeneric(ISymbol symbol, SyntaxNode node, ITypeSymbol destinationType, Location attributeLocation)
            => new(true, symbol, node, null, destinationType, attributeLocation, null, null);

        public static AttributeUsage Unsupported(Location location)
            => new(false, null, null, null, null, null, location, null);

        /// <summary>
        /// The generic `&lt;T&gt;` form combined with a named `Types = [...]` argument on the same
        /// attribute application (Known-issues.md #11) - discovered purely syntactically, the same
        /// way the generic form itself is, since resolving a destination type from either source
        /// would be pointless when they conflict.
        /// </summary>
        public static AttributeUsage ConflictingTypeSpecification(Location location)
            => new(true, null, null, null, null, null, null, location);

        public bool IsSupported { get; }

        public ISymbol? Symbol { get; }

        public SyntaxNode? Node { get; }

        public AttributeData? Attribute { get; }

        /// <summary>Set only for usages discovered via the generic-form syntactic path.</summary>
        public ITypeSymbol? GenericDestinationType { get; }

        /// <summary>Set only for usages discovered via the generic-form syntactic path.</summary>
        public Location? AttributeLocation { get; }

        public Location? UnsupportedLocation { get; }

        /// <summary>Set only for a generic-form usage combined with a conflicting `Types = [...]` argument.</summary>
        public Location? ConflictingTypeSpecificationLocation { get; }
    }
}
