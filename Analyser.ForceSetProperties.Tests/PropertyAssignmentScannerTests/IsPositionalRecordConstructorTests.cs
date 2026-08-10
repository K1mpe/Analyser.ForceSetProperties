using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class IsPositionalRecordConstructorTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void RecordPrimaryConstructor_ReturnsTrue()
        {
            const string source = @"
public record Person(string Name, int Age);";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetConstructor(compilation, "Person");

            Assert.True(_sut.IsPositionalRecordConstructor(constructor));
        }

        [Fact]
        public void RecordStructPrimaryConstructor_ReturnsTrue()
        {
            const string source = @"
public record struct Point(int X, int Y);";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetConstructor(compilation, "Point");

            Assert.True(_sut.IsPositionalRecordConstructor(constructor));
        }

        [Fact]
        public void RecordsOwnSecondaryConstructor_ReturnsFalse()
        {
            const string source = @"
public record Person(string Name, int Age)
{
    public Person(string name) : this(name, 0)
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            // Records also synthesize an implicit copy constructor (used by `with`), which also has
            // exactly one parameter - filter it out to get the user-written secondary constructor.
            var secondary = RoslynTestHelper.GetType(compilation, "Person").Constructors
                .Single(c => c.Parameters.Length == 1 && !c.IsImplicitlyDeclared);

            Assert.False(_sut.IsPositionalRecordConstructor(secondary));
        }

        [Fact]
        public void RegularClassConstructor_ReturnsFalse()
        {
            const string source = @"
public class Person
{
    public Person(string name)
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetConstructor(compilation, "Person");

            Assert.False(_sut.IsPositionalRecordConstructor(constructor));
        }
    }
}
