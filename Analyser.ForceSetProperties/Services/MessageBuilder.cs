using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Analyser.ForceSetProperties.Diagnostics;
using Analyser.ForceSetProperties.Models;
using Microsoft.CodeAnalysis;

namespace Analyser.ForceSetProperties.Services
{
    public class MessageBuilder
    {
        public Diagnostic BuildDiagnostic(ContextModel context, string basePath)
        {
            if (context.RequiredProperties.Count == 0)
            {
                return BuildNoPropertiesFoundDiagnostic(context);
            }

            return context.IsFullySet
                ? BuildValidatedDiagnostic(context, basePath)
                : BuildMissingPropertiesDiagnostic(context);
        }

        public Diagnostic BuildNoPropertiesFoundDiagnostic(ContextModel context)
        {
            return Diagnostic.Create(DiagnosticDescriptors.NoPropertiesFound, context.AttributeLocation, context.DestinationType.Name);
        }

        public Diagnostic BuildValidatedDiagnostic(ContextModel context, string basePath)
        {
            var message = BuildValidationMessage(context, basePath);
            return Diagnostic.Create(DiagnosticDescriptors.Validated, context.AttributeLocation, message);
        }

        public Diagnostic BuildMissingPropertiesDiagnostic(ContextModel context)
        {
            var missingNames = GetMissingPropertyNames(context);

            if (missingNames.Count == 1)
            {
                return Diagnostic.Create(DiagnosticDescriptors.MissingProperty, context.AttributeLocation, missingNames[0], context.DestinationType.Name);
            }

            var message = BuildMissingPropertiesMessage(missingNames, context.DestinationType.Name);
            return Diagnostic.Create(DiagnosticDescriptors.MissingProperties, context.AttributeLocation, message);
        }

        public Diagnostic BuildUnsupportedTargetDiagnostic(Location location)
        {
            return Diagnostic.Create(DiagnosticDescriptors.UnsupportedTarget, location);
        }

        public Diagnostic BuildUnsupportedDestinationTypeDiagnostic(ITypeSymbol type, Location location)
        {
            return Diagnostic.Create(DiagnosticDescriptors.UnsupportedDestinationType, location, type.ToDisplayString());
        }

        public Diagnostic BuildAmbiguousGenericInferenceDiagnostic(ITypeSymbol type, Location location)
        {
            return Diagnostic.Create(DiagnosticDescriptors.AmbiguousGenericInference, location, type.ToDisplayString());
        }

        public Diagnostic BuildInterfaceDestinationTypeWarningDiagnostic(ITypeSymbol type, Location location)
        {
            return Diagnostic.Create(DiagnosticDescriptors.InterfaceDestinationType, location, type.ToDisplayString());
        }

        public Diagnostic BuildConflictingDestinationTypeDiagnostic(Location location)
        {
            return Diagnostic.Create(DiagnosticDescriptors.ConflictingDestinationTypeSpecification, location);
        }

        public List<string> GetMissingPropertyNames(ContextModel context)
        {
            return context.RequiredProperties.Where(p => !p.IsSet).Select(p => p.Name).ToList();
        }

        public string BuildMissingPropertiesMessage(List<string> missingNames, string typeName)
        {
            var lines = missingNames.Select(name => $" - {name}");
            return $"The following properties on '{typeName}' must be initialized:\n" + string.Join("\n", lines);
        }

        public bool UsedTracing(ContextModel context)
        {
            return context.RequiredProperties
                .SelectMany(p => p.SetLocations)
                .Any(location => location.MethodName != null);
        }

        public string BuildValidationMessage(ContextModel context, string basePath)
        {
            return UsedTracing(context)
                ? BuildDetailedBreakdown(context, basePath)
                : BuildShortSummary(context);
        }

        public string BuildShortSummary(ContextModel context)
        {
            var propertyNames = string.Join(", ", context.RequiredProperties.Select(p => p.Name));
            return $"ForceSetProperties validated {context.DestinationType.Name}: {propertyNames}";
        }

        public string BuildDetailedBreakdown(ContextModel context, string basePath)
        {
            var lines = new List<string> { $"Type checked: {context.DestinationType.Name}" };
            lines.AddRange(context.RequiredProperties.Select(p => BuildPropertyLine(p, basePath)));
            return string.Join("\n", lines);
        }

        public string BuildPropertyLine(RequiredProperty property, string basePath)
        {
            var firstLocation = property.SetLocations[0];
            return $"{property.Name}: {BuildLocationText(firstLocation, basePath)}";
        }

        public string BuildLocationText(SetLocation location, string basePath)
        {
            var relativePath = GetRelativePath(basePath, location.FileName);
            var text = relativePath != null ? $"{relativePath} line {location.LineNumber}" : $"line {location.LineNumber}";
            return location.MethodName != null ? $"{text} (via {location.MethodName})" : text;
        }

        // Returns null - rather than throwing - when a path isn't available or isn't a real,
        // absolute file path (e.g. a Compilation built without real file paths); callers just
        // leave the path out of the message in that case instead of failing the whole diagnostic.
        public string? GetRelativePath(string basePath, string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            try
            {
                var baseUri = new Uri(AppendDirectorySeparator(basePath));
                var fullUri = new Uri(fullPath);

                if (baseUri.Scheme != fullUri.Scheme)
                {
                    return fullPath;
                }

                var relativeUri = baseUri.MakeRelativeUri(fullUri);
                var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
                return relativePath.Replace('/', Path.DirectorySeparatorChar);
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString()) ? path : path + Path.DirectorySeparatorChar;
        }
    }
}
