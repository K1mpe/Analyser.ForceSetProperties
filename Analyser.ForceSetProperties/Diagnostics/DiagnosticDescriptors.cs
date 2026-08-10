using Microsoft.CodeAnalysis;

namespace Analyser.ForceSetProperties.Diagnostics
{
    public static class DiagnosticDescriptors
    {
        private const string Category = "ForceSetProperties";

        public static readonly DiagnosticDescriptor MissingProperty = new DiagnosticDescriptor(
            id: "FSP001",
            title: "Missing property assignment",
            messageFormat: "Property '{0}' must be initialized on '{1}' when using ForceSetProperties",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingProperties = new DiagnosticDescriptor(
            id: "FSP002",
            title: "Multiple properties missing",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedTarget = new DiagnosticDescriptor(
            id: "FSP003",
            title: "Unsupported attribute target",
            messageFormat: "ForceSetProperties can only be applied to constructors, methods, or properties; this target is not yet supported",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedDestinationType = new DiagnosticDescriptor(
            id: "FSP004",
            title: "Unsupported destination type",
            messageFormat: "ForceSetProperties cannot validate '{0}'; void, object, and dynamic are not supported destination types",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NoPropertiesFound = new DiagnosticDescriptor(
            id: "FSP005",
            title: "No properties found to validate",
            messageFormat: "ForceSetProperties found no assignable properties on '{0}' to validate; check that this is the intended destination type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AmbiguousGenericInference = new DiagnosticDescriptor(
            id: "FSP006",
            title: "Ambiguous inferred generic destination type",
            messageFormat: "ForceSetProperties inferred '{0}' as the destination type, but it's generic and its properties may not be what you intend to validate; specify the destination type explicitly, for example ForceSetProperties(typeof(YourType)) or ForceSetProperties<T>, to confirm '{0}' is really what you mean",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ConflictingDestinationTypeSpecification = new DiagnosticDescriptor(
            id: "FSP007",
            title: "Conflicting destination type specification",
            messageFormat: "ForceSetProperties has more than one destination type specification on the same member (generic argument, constructor argument, and/or Types property); these don't merge - remove all but one",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InterfaceDestinationType = new DiagnosticDescriptor(
            id: "FSP101",
            title: "Destination type is an interface",
            messageFormat: "ForceSetProperties is validating against the interface '{0}'; only properties declared on '{0}' (or an interface it extends) are checked, so properties on the concrete type actually being constructed that aren't part of '{0}' are not verified. Consider specifying the concrete destination type instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Validated = new DiagnosticDescriptor(
            id: "FSP201",
            title: "All properties validated",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
