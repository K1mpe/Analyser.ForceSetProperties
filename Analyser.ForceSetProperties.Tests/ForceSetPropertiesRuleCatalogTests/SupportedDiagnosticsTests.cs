using System.Linq;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetPropertiesRuleCatalogTests
{
    /// <summary>
    /// Known-issues.md #7 - ForceSetPropertiesRuleCatalog exists purely to declare every rule
    /// ForceSetAnalyser (an IIncrementalGenerator, which has no SupportedDiagnostics concept of its
    /// own) can produce, so the IDE has something to enumerate (Solution Explorer's
    /// Dependencies > Analyzers node, .editorconfig rule-list generation, etc.).
    /// </summary>
    public class SupportedDiagnosticsTests
    {
        private readonly ForceSetPropertiesRuleCatalog _sut = new();

        [Fact]
        public void IncludesEveryDiagnosticDescriptorTheGeneratorCanReport()
        {
            var expectedIds = new[] { "FSP001", "FSP002", "FSP003", "FSP004", "FSP005", "FSP006", "FSP007", "FSP101", "FSP201" };

            var actualIds = _sut.SupportedDiagnostics.Select(d => d.Id).ToArray();

            Assert.Equal(expectedIds.OrderBy(id => id), actualIds.OrderBy(id => id));
        }

        [Fact]
        public void HasNoDuplicateIds()
        {
            var ids = _sut.SupportedDiagnostics.Select(d => d.Id).ToList();

            Assert.Equal(ids.Distinct().Count(), ids.Count);
        }
    }
}
