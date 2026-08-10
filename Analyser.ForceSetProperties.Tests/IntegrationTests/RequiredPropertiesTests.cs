using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "Which properties are required" - a property is required only when the code being
    /// validated can actually see and set it, using normal C# accessibility rules.
    /// </summary>
    public class RequiredPropertiesTests
    {
        [Fact]
        public void PrivateSetter_IsRequired_WhenTheAnnotatedMemberIsInTheSameClass()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; private set; }

                    [ForceSetProperties]
                    public static DtoModel CreateInstance(string name)
                    {
                        return new DtoModel { Name = name };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void PrivateSetter_IsNotRequired_WhenTheAnnotatedMemberIsInADifferentClass()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; private set; }
                    public string Id { get; set; }
                }

                public class Factory
                {
                    // Can't see DtoModel.Name's private setter from here, so only Id is required -
                    // this must validate cleanly even though Name is never assigned.
                    [ForceSetProperties]
                    public static DtoModel Create(string id)
                    {
                        return new DtoModel { Id = id };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }
    }
}
