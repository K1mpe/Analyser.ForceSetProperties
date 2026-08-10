using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// README: "Basic Usage" / "Attribute Targets" - where [ForceSetProperties] can be applied.
    /// </summary>
    public class AttributeTargetsTests
    {
        [Fact]
        public void OnAClass_IsNotSupportedAndFailsLoudly()
        {
            const string source = """
                [ForceSetProperties]
                public class DtoModel
                {
                    public string Name { get; set; }
                    public System.DateTime CreatedAt { get; set; }
                    public System.DateTime UpdatedAt { get; set; }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP003" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void OnAConstructor_ValidatesThePropertiesItSets()
        {
            const string source = """
                public class DbModel
                {
                    public string Name { get; set; }
                    public System.DateTime CreatedAt { get; set; }
                    public System.DateTime UpdatedAt { get; set; }
                }

                public class DtoModel
                {
                    public DtoModel()
                    {
                    }

                    [ForceSetProperties]
                    public DtoModel(DbModel db)
                    {
                        Name = db.Name;
                        CreatedAt = db.CreatedAt;
                        UpdatedAt = db.UpdatedAt;
                    }

                    public string Name { get; set; }
                    public System.DateTime CreatedAt { get; set; }
                    public System.DateTime UpdatedAt { get; set; }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void OnAMethod_ValidatesTheReturnedInstance()
        {
            const string source = """
                public class DtoModel
                {
                    public string Name { get; set; }
                    public System.DateTime CreatedAt { get; set; }
                    public System.DateTime UpdatedAt { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public DtoModel FromFunction(string name, System.DateTime createdAt, System.DateTime updatedAt)
                    {
                        return new DtoModel
                        {
                            Name = name,
                            CreatedAt = createdAt,
                            UpdatedAt = updatedAt
                        };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void OnAnExpressionBodiedProperty_ValidatesTheLambdaBody()
        {
            const string source = """
                using System;
                using System.Linq.Expressions;

                public class DbModel
                {
                    public string Name { get; set; }
                    public DateTime CreatedAt { get; set; }
                    public DateTime UpdatedAt { get; set; }
                }

                public class DtoModel
                {
                    public string Name { get; set; }
                    public DateTime CreatedAt { get; set; }
                    public DateTime UpdatedAt { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public static Expression<Func<DbModel, DtoModel>> FromExpression => db => new DtoModel
                    {
                        Name = db.Name,
                        CreatedAt = db.CreatedAt,
                        UpdatedAt = db.UpdatedAt
                    };
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void OnAnInterfaceMethodSignatureWithNoBody_IsNotSupportedAndFailsLoudly()
        {
            // Known-issues.md #12: there's nothing here to check yet - the real implementation
            // (whichever type implements IFactory) is what should be annotated instead.
            const string source = """
                public class Dto
                {
                    public string Name { get; set; }
                }

                public interface IFactory
                {
                    [ForceSetProperties]
                    Dto Create();
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP003" && d.Severity == DiagnosticSeverity.Error);
            Assert.DoesNotContain(diagnostics, d => d.Id == "FSP001" || d.Id == "FSP002");
        }

        [Fact]
        public void OnAnAbstractMethodDeclaration_IsNotSupportedAndFailsLoudly()
        {
            // Known-issues.md #12: same reasoning as the interface case above - annotate the
            // override instead, once it exists.
            const string source = """
                public class Dto
                {
                    public string Name { get; set; }
                }

                public abstract class FactoryBase
                {
                    [ForceSetProperties]
                    public abstract Dto Create();
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP003" && d.Severity == DiagnosticSeverity.Error);
            Assert.DoesNotContain(diagnostics, d => d.Id == "FSP001" || d.Id == "FSP002");
        }

        [Fact]
        public void OnADefaultInterfaceMethodImplementation_ValidatesNormally()
        {
            // Known-issues.md #12: a default implementation has a real body to check, so it must
            // be unaffected by the interface-method-signature rejection above.
            const string source = """
                public class Dto
                {
                    public string Name { get; set; }
                }

                public interface IFactory
                {
                    [ForceSetProperties]
                    Dto Create() => new Dto { Name = "x" };
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void OnAnOverrideOfAnAbstractMethod_ValidatesNormally()
        {
            const string source = """
                public class Dto
                {
                    public string Name { get; set; }
                }

                public abstract class FactoryBase
                {
                    public abstract Dto Create();
                }

                public class Factory : FactoryBase
                {
                    [ForceSetProperties]
                    public override Dto Create() => new Dto { Name = "x" };
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void OnARecordPrimaryConstructorViaMethodTarget_ValidatesNormally()
        {
            // Known-issues.md #4: `method:` is the syntactically correct way to target a record's
            // primary constructor - both Name and Age are set via the `: this(default, default)`
            // chain into it, so this should validate cleanly rather than being rejected outright.
            const string source = """
                [method: ForceSetProperties]
                public record Person(string Name, int Age)
                {
                    public Person() : this(default, default) { }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            Assert.Contains("Name", validated.GetMessage());
            Assert.Contains("Age", validated.GetMessage());
        }

        [Fact]
        public void OnARecordWithNoMethodTarget_IsStillNotSupported()
        {
            // Without `method:`, the attribute targets the record type itself, same as a plain
            // class - still unsupported, unaffected by the primary-constructor fix above.
            const string source = """
                [ForceSetProperties]
                public record Person(string Name, int Age);
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP003" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void OnARecordPrimaryConstructorViaMethodTarget_StillReportsAGenuinelyMissingProperty()
        {
            const string source = """
                [method: ForceSetProperties]
                public record Person(string Name, int Age)
                {
                    public string Extra { get; set; }

                    public Person() : this(default, default) { }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            var missing = Assert.Single(diagnostics, d => d.Id == "FSP001");
            Assert.Contains("Extra", missing.GetMessage());
        }
    }
}
