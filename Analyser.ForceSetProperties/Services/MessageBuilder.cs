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
            return context.IsFullySet
                ? BuildValidatedDiagnostic(context, basePath)
                : BuildMissingPropertiesDiagnostic(context);
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
                return Diagnostic.Create(DiagnosticDescriptors.MissingProperty, context.AttributeLocation, missingNames[0]);
            }

            var message = BuildMissingPropertiesMessage(missingNames);
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

        public List<string> GetMissingPropertyNames(ContextModel context)
        {
            return context.RequiredProperties.Where(p => !p.IsSet).Select(p => p.Name).ToList();
        }

        public string BuildMissingPropertiesMessage(List<string> missingNames)
        {
            var lines = missingNames.Select(name => $" - {name}");
            return "The following properties must be initialized:\n" + string.Join("\n", lines);
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
            var text = $"{relativePath} line {location.LineNumber}";
            return location.MethodName != null ? $"{text} (via {location.MethodName})" : text;
        }

        public string GetRelativePath(string basePath, string fullPath)
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

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString()) ? path : path + Path.DirectorySeparatorChar;
        }
    }
}
