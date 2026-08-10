using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// Known-issues.md #6 - properties inherited from a base class (and not overridden) must be
    /// required just like properties declared directly on the destination type.
    /// </summary>
    public class InheritedPropertiesTests
    {
        [Fact]
        public void InheritedProperty_LeftUnset_IsReportedMissing()
        {
            const string source = """
                public class BaseModel
                {
                    public string Id { get; set; }
                }

                public class DtoModel : BaseModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new DtoModel { Name = "x" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var missing = Assert.Single(diagnostics, d => d.Id == "FSP001");
            Assert.Contains("Id", missing.GetMessage());
        }

        [Fact]
        public void InheritedProperty_AlsoSet_ValidatesSuccessfully()
        {
            const string source = """
                public class BaseModel
                {
                    public string Id { get; set; }
                }

                public class DtoModel : BaseModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new DtoModel { Id = "1", Name = "x" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            Assert.Contains("Id", validated.GetMessage());
            Assert.Contains("Name", validated.GetMessage());
        }

        [Fact]
        public void PrivateSetterOnBaseClass_IsNotRequired_WhenAccessedFromOutsideTheBaseClass()
        {
            const string source = """
                public class BaseModel
                {
                    public string Id { get; private set; }
                }

                public class DtoModel : BaseModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    // Can't see BaseModel.Id's private setter from here, so only Name is
                    // required - this must validate cleanly even though Id is never assigned.
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new DtoModel { Name = "x" };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            Assert.DoesNotContain("Id", validated.GetMessage());
        }

        [Fact]
        public void OverriddenProperty_IsNotDoubleReportedAsMissing()
        {
            const string source = """
                public class BaseModel
                {
                    public virtual string Name { get; set; }
                }

                public class DtoModel : BaseModel
                {
                    public override string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel Create()
                    {
                        return new DtoModel();
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var missing = Assert.Single(diagnostics, d => d.Id == "FSP001");
            Assert.Contains("Name", missing.GetMessage());
        }
    }
}
