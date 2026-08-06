using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var basePath = GetCommonDirectory(compilation.SyntaxTrees.Select(t => t.FilePath));

            foreach (var location in _targetFinder.FindUnsupportedAttributeUsages(compilation))
            {
                yield return _messageBuilder.BuildUnsupportedTargetDiagnostic(location);
            }

            foreach (var target in _targetFinder.FindTargets(compilation))
            {
                foreach (var unsupported in _contextBuilder.GetUnsupportedDestinationTypes(target.Symbol, target.Node, target.Attribute))
                {
                    yield return _messageBuilder.BuildUnsupportedDestinationTypeDiagnostic(unsupported.Type, unsupported.Location);
                }

                foreach (var context in _contextBuilder.Build(target.Symbol, target.Node, target.Attribute, compilation))
                {
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
