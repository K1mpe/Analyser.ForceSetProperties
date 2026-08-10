using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "Limitations" - the intentional boundaries of tracing into called methods.
    /// </summary>
    public class LimitationsTests
    {
        [Fact]
        public void VirtualMethods_AreNotFollowed_EvenThoughTheyDoSetEveryProperty()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; set; }
                    public string Id { get; set; }
                }

                public abstract class FactoryBase
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return Map();
                    }

                    // Virtual - could be overridden at runtime, so the analyzer refuses to guess
                    // which implementation runs and doesn't follow the call at all.
                    protected virtual DtoModel Map()
                    {
                        return new DtoModel { Name = "x", Id = "1" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP002" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void DelegatesStoredInVariables_AreNotFollowed()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    // The assignment lives in this field initializer, outside Create()'s own body -
                    // finding it requires tracing through the delegate invocation below, which the
                    // analyzer intentionally does not do.
                    private static readonly System.Func<DtoModel> _factory = () => new DtoModel { Name = "x" };

                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return _factory();
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.GetMessage().Contains("Name"));
        }

        [Fact]
        public void ConditionalBranches_AreNotAnalyzedForExhaustiveness()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create(bool flag)
                    {
                        var dto = new DtoModel();

                        // No else branch - the analyzer doesn't prove every path sets Name, it just
                        // finds that an assignment to Name exists somewhere in the method.
                        if (flag)
                        {
                            dto.Name = "x";
                        }

                        return dto;
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void MethodsWithoutAvailableSource_AreNotFollowed()
        {
            const string librarySource = """
                public class DtoModel
                {
                    public string Name { get; set; }
                }

                public static class LibraryFactory
                {
                    public static DtoModel Map(string name)
                    {
                        return new DtoModel { Name = name };
                    }
                }
                """;
            var libraryReference = RoslynTestHelper.CompileToReference(librarySource);

            const string consumerSource = """
                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create(string name)
                    {
                        return LibraryFactory.Map(name);
                    }
                }
                """;
            var consumerCompilation = RoslynTestHelper.Compile(consumerSource)
                .AddReferences(libraryReference);

            var diagnostics = RoslynTestHelper.GetDiagnostics(consumerCompilation);

            // LibraryFactory.Map has no source in this compilation, so the analyzer can't inspect its
            // body - Name is never found as set, even though the referenced method does set it.
            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.GetMessage().Contains("Name"));
        }
    }
}
