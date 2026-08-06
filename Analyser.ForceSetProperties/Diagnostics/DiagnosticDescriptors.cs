using Microsoft.CodeAnalysis;

namespace Analyser.ForceSetProperties.Diagnostics
{
    public static class DiagnosticDescriptors
    {
        private const string Category = "ForceSetProperties";

        public static readonly DiagnosticDescriptor MissingProperty = new DiagnosticDescriptor(
            id: "FSP001",
            title: "Missing property assignment",
            messageFormat: "Property '{0}' must be initialized when using ForceSetProperties",
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
            id: "FSP006",
            title: "Unsupported attribute target",
            messageFormat: "ForceSetProperties can only be applied to constructors, methods, or properties; this target is not yet supported",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedDestinationType = new DiagnosticDescriptor(
            id: "FSP007",
            title: "Unsupported destination type",
            messageFormat: "ForceSetProperties cannot validate '{0}'; void, object, and dynamic are not supported destination types",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Validated = new DiagnosticDescriptor(
            id: "FSP101",
            title: "All properties validated",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
