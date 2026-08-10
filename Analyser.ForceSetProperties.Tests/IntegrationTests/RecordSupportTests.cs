using System.Linq;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.IntegrationTests
{
    /// <summary>
    /// Known-issues.md #2 and #3 - positional records/record structs and with-expressions, the two
    /// record-specific gaps fixed together since both only manifest on records.
    /// </summary>
    public class RecordSupportTests
    {
        [Fact]
        public void PositionalRecord_ConstructedDirectly_ValidatesEachPositionalProperty()
        {
            const string source = """
                public record Person(string Name, int Age);

                public class Factory
                {
                    [ForceSetProperties]
                    public Person Create(string name, int age)
                    {
                        return new Person(name, age);
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            Assert.Contains("(via .ctor)", validated.GetMessage());
        }

        [Fact]
        public void PositionalRecord_ConstructedViaTargetTypedNew_ValidatesEachPositionalPropertyAndTheAdditionalOne()
        {
            const string source = """
                public record Person(string Name, int Age)
                {
                    public string? Description { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public static Person Create(string name, string description)
                    {
                        return new(name, 10)
                        {
                            Description = description
                        };
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            Assert.Contains("(via .ctor)", validated.GetMessage());
            Assert.Contains("Description", validated.GetMessage());
        }

        [Fact]
        public void PositionalRecordStruct_ConstructedDirectly_ValidatesEachPositionalProperty()
        {
            const string source = """
                public record struct Point(int X, int Y);

                public class Factory
                {
                    [ForceSetProperties]
                    public Point Create(int x, int y)
                    {
                        return new Point(x, y);
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void PositionalRecord_WithAnAdditionalUnsetProperty_StillReportsItMissing()
        {
            const string source = """
                public record Person(string Name, int Age)
                {
                    public string Email { get; set; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public Person Create(string name, int age)
                    {
                        return new Person(name, age); // Email is never set
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.GetMessage().Contains("Email"));
        }

        [Fact]
        public void PositionalRecord_ConstructedViaAChainedConstructor_TracesIntoThePrimaryConstructor()
        {
            const string source = """
                public record Person(string Name, int Age)
                {
                    public Person(string name) : this(name, 0)
                    {
                    }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public Person Create(string name)
                    {
                        return new Person(name);
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Contains(diagnostics, d => d.Id == "FSP201");
        }

        [Fact]
        public void RegularConstructor_WithAParameterNameMatchingAProperty_StillRequiresAnExplicitAssignment()
        {
            // Guards against the positional-record fix over-firing on an ordinary class where a
            // constructor parameter happens to share a property's name but never actually assigns it.
            const string source = """
                public class Person
                {
                    public string Name { get; set; }

                    public Person(string name)
                    {
                        // forgot to assign Name
                    }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public Person Create(string name)
                    {
                        return new Person(name);
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.GetMessage().Contains("Name"));
        }

        [Fact]
        public void WithExpression_PropertiesNotMentioned_AreTreatedAsCopiedFromTheSource()
        {
            const string source = """
                public record Person
                {
                    public string Name { get; init; }
                    public int Age { get; init; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public Person Rename(Person existing, string name)
                    {
                        return existing with { Name = name }; // Age intentionally preserved from existing
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            var validated = Assert.Single(diagnostics, d => d.Id == "FSP201");
            Assert.Contains("(via with)", validated.GetMessage());
        }

        [Fact]
        public void WithExpression_OnAnUnrelatedType_DoesNotSatisfyTheDestinationType()
        {
            const string source = """
                public record Other
                {
                    public int Value { get; init; }
                }

                public record Person
                {
                    public string Name { get; init; }
                    public int Age { get; init; }
                }

                public class Factory
                {
                    [ForceSetProperties]
                    public Person Create(Other other, string name)
                    {
                        var unrelated = other with { Value = 1 };
                        return new Person { Name = name }; // Age is genuinely missing
                    }
                }
                """;

            var diagnostics = RoslynTestHelper.GetDiagnostics(source);

            Assert.Contains(diagnostics, d => d.Id == "FSP001" && d.GetMessage().Contains("Age"));
        }
    }
}
