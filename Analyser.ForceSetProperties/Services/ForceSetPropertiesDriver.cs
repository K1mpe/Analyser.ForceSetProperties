using System.Collections.Generic;
using System.IO;
using System.Linq;
using Analyser.ForceSetProperties.Models;
using Microsoft.CodeAnalysis;

namespace Analyser.ForceSetProperties.Services
{
    public class ForceSetPropertiesDriver
    {
        private readonly ForceSetPropertiesTargetFinder _targetFinder = new();
        private readonly ContextBuilder _contextBuilder = new();
        private readonly PropertyAssignmentScanner _scanner = new();
        private readonly MessageBuilder _messageBuilder = new();

        public IEnumerable<Diagnostic> GetDiagnostics(Compilation compilation)
        {
            var unsupported = _targetFinder.FindUnsupportedAttributeUsages(compilation)
                .Select(AttributeUsage.Unsupported);

            var supported = _targetFinder.FindTargets(compilation)
                .Select(target => AttributeUsage.Supported(target.Symbol, target.Node, target.Attribute));

            return GetDiagnostics(compilation, unsupported.Concat(supported).ToArray());
        }

        /// <summary>
        /// Same validation as <see cref="GetDiagnostics(Compilation)"/>, but for usages already
        /// discovered elsewhere (the incremental generator pipeline supplies these from its own
        /// cached attribute search instead of re-walking the whole compilation here).
        /// </summary>
        public IEnumerable<Diagnostic> GetDiagnostics(Compilation compilation, IReadOnlyList<AttributeUsage> usages)
        {
            var basePath = GetCommonDirectory(compilation.SyntaxTrees.Select(t => t.FilePath));

            foreach (var usage in usages)
            {
                if (usage.ConflictingTypeSpecificationLocation != null)
                {
                    yield return _messageBuilder.BuildConflictingDestinationTypeDiagnostic(usage.ConflictingTypeSpecificationLocation);
                    continue;
                }

                if (!usage.IsSupported)
                {
                    yield return _messageBuilder.BuildUnsupportedTargetDiagnostic(usage.UnsupportedLocation!);
                    continue;
                }

                if (usage.GenericDestinationType != null)
                {
                    var destinationType = usage.GenericDestinationType;
                    var attributeLocation = usage.AttributeLocation!;

                    if (_contextBuilder.IsUnsupportedDestinationType(destinationType))
                    {
                        yield return _messageBuilder.BuildUnsupportedDestinationTypeDiagnostic(destinationType, attributeLocation);
                        continue;
                    }

                    if (destinationType.TypeKind == TypeKind.Interface)
                    {
                        yield return _messageBuilder.BuildInterfaceDestinationTypeWarningDiagnostic(destinationType, attributeLocation);
                    }

                    var genericContext = _contextBuilder.BuildSingle(usage.Symbol!, usage.Node!, attributeLocation, destinationType, compilation);
                    _scanner.Scan(genericContext, compilation);
                    yield return _messageBuilder.BuildDiagnostic(genericContext, basePath);
                    continue;
                }

                if (_contextBuilder.HasConflictingTypeSpecification(usage.Attribute!))
                {
                    var conflictLocation = _contextBuilder.ResolveAttributeLocation(usage.Attribute!, usage.Node!);
                    yield return _messageBuilder.BuildConflictingDestinationTypeDiagnostic(conflictLocation);
                    continue;
                }

                foreach (var unsupported in _contextBuilder.GetUnsupportedDestinationTypes(usage.Symbol!, usage.Node!, usage.Attribute!))
                {
                    yield return _messageBuilder.BuildUnsupportedDestinationTypeDiagnostic(unsupported.Type, unsupported.Location);
                }

                foreach (var ambiguous in _contextBuilder.GetAmbiguousInferredDestinationTypes(usage.Symbol!, usage.Node!, usage.Attribute!))
                {
                    yield return _messageBuilder.BuildAmbiguousGenericInferenceDiagnostic(ambiguous.Type, ambiguous.Location);
                }

                foreach (var context in _contextBuilder.Build(usage.Symbol!, usage.Node!, usage.Attribute!, compilation))
                {
                    if (context.DestinationType.TypeKind == TypeKind.Interface)
                    {
                        yield return _messageBuilder.BuildInterfaceDestinationTypeWarningDiagnostic(context.DestinationType, context.AttributeLocation);
                    }

                    _scanner.Scan(context, compilation);
                    yield return _messageBuilder.BuildDiagnostic(context, basePath);
                }
            }
        }

        public string GetCommonDirectory(IEnumerable<string> filePaths)
        {
            var directories = filePaths
                .Select(Path.GetDirectoryName)
                .Where(d => !string.IsNullOrEmpty(d))
                .Select(d => d!.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .ToList();

            if (directories.Count == 0)
            {
                return string.Empty;
            }

            var commonSegments = directories[0];
            foreach (var segments in directories.Skip(1))
            {
                var commonLength = 0;
                while (commonLength < commonSegments.Length
                    && commonLength < segments.Length
                    && string.Equals(commonSegments[commonLength], segments[commonLength], System.StringComparison.OrdinalIgnoreCase))
                {
                    commonLength++;
                }

                commonSegments = commonSegments.Take(commonLength).ToArray();
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), commonSegments);
        }
    }
}
