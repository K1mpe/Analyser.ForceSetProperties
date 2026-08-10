using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "What triggers a compile error" - FSP001, FSP002, FSP003, FSP004, FSP005, FSP006.
    /// </summary>
    public class CompileErrorsTests
    {
        [Fact]
        public void MissingProperty_RaisesFSP001()
        {
            const string source = """
                public class AuditLog
                {
                    public string CreatedAt { get; set; }
                    public string UpdatedAt { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public AuditLog Create()
                    {
                        return new AuditLog
                        {
                            CreatedAt = "x"
                        };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var error = Assert.Single(diagnostics, d => d.Id == "FSP001");
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Equal("Property 'UpdatedAt' must be initialized on 'AuditLog' when using ForceSetProperties", error.GetMessage());
        }

        [Fact]
        public void MultipleMissingProperties_RaiseFSP002()
        {
            const string source = """
                public class AuditLog
                {
                    public string CreatedAt { get; set; }
                    public string UpdatedAt { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public AuditLog Create()
                    {
                        return new AuditLog();
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var error = Assert.Single(diagnostics, d => d.Id == "FSP002");
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Equal(
                "The following properties on 'AuditLog' must be initialized:\n - CreatedAt\n - UpdatedAt",
                error.GetMessage());
        }

        [Fact]
        public void UnsupportedAttributePlacement_OnAField_RaisesFSP003()
        {
            const string source = """
                public class DtoModel
                {
                    [ForceSetProperties]
                    public string Name;
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP003" && d.Severity == DiagnosticSeverity.Error);
        }

        [Theory]
        [InlineData("void", "")]
        [InlineData("object", "return new object();")]
        [InlineData("dynamic", "return 1;")]
        public void UnsupportedDestinationType_RaisesFSP004(string returnType, string body)
        {
            var source = $$"""
                public class Factory
                {
                    [ForceSetProperties]
                    public {{returnType}} Create()
                    {
                        {{body}}
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var error = Assert.Single(diagnostics, d => d.Id == "FSP004");
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Contains(returnType, error.GetMessage());
        }

        [Fact]
        public void DestinationTypeWithNoAssignableProperties_RaisesFSP005()
        {
            const string source = """
                public class EmptyDto
                {
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public EmptyDto Create()
                    {
                        return new EmptyDto();
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var error = Assert.Single(diagnostics, d => d.Id == "FSP005");
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Equal(
                "ForceSetProperties found no assignable properties on 'EmptyDto' to validate; check that this is the intended destination type",
                error.GetMessage());
        }

        [Fact]
        public void AsyncMethodInferringTaskAsTheDestinationType_ValidatesTheWrappedTypeInstead()
        {
            // Known-issues.md #1 (fixed): Task<T>/ValueTask<T> are now unwrapped by return-type
            // inference, so the destination type is DtoModel, not Task - a missing property on
            // DtoModel is reported directly, instead of the old FSP005-against-Task workaround.
            const string source = """
                using System.Threading.Tasks;

                public class DtoModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public async Task<DtoModel> CreateAsync()
                    {
                        await Task.Yield();
                        return new DtoModel();
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var error = Assert.Single(diagnostics, d => d.Id == "FSP001");
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Equal("Property 'Name' must be initialized on 'DtoModel' when using ForceSetProperties", error.GetMessage());
        }

        [Fact]
        public void AmbiguousGenericTypeReachedThroughInference_RaisesFSP006()
        {
            // Known-issues.md #10: List<T> (not unwrapped, unlike Task<T>/ValueTask<T>) reached only
            // through implicit return-type inference is ambiguous - is the intent to validate the
            // list itself, or what it wraps? Reject rather than silently validating the wrapper.
            const string source = """
                using System.Collections.Generic;

                public class DatabaseModel
                {
                    public string Name { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public static List<DatabaseModel> CreateClone(DatabaseModel model)
                    {
                        return new List<DatabaseModel> { model };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var error = Assert.Single(diagnostics, d => d.Id == "FSP006");
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Contains("List<DatabaseModel>", error.GetMessage());
        }
    }
}
