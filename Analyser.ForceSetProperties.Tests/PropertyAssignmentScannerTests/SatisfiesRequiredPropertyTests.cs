using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class SatisfiesRequiredPropertyTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void SameSymbol_IsSatisfied()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var name = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");

            var result = _sut.SatisfiesRequiredProperty(name, name);

            Assert.True(result);
        }

        [Fact]
        public void ConcretePropertyImplementingAnInterfaceProperty_IsSatisfied()
        {
            const string source = @"
public interface IDto
{
    string Name { get; set; }
}

public class DtoModel : IDto
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var interfaceProperty = RoslynTestHelper.GetProperty(compilation, "IDto", "Name");
            var concreteProperty = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Name");

            var result = _sut.SatisfiesRequiredProperty(interfaceProperty, concreteProperty);

            Assert.True(result);
        }

        [Fact]
        public void ConcretePropertyImplementingAnInterfacePropertyExplicitly_IsSatisfied()
        {
            const string source = @"
public interface IDto
{
    string Name { get; set; }
}

public class DtoModel : IDto
{
    string IDto.Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var interfaceProperty = RoslynTestHelper.GetProperty(compilation, "IDto", "Name");
            var explicitImplementation = compilation.GetType("DtoModel")
                .FindImplementationForInterfaceMember(interfaceProperty);

            var result = _sut.SatisfiesRequiredProperty(interfaceProperty, explicitImplementation!);

            Assert.True(result);
        }

        [Fact]
        public void UnrelatedPropertyOnAnotherType_IsNotSatisfied()
        {
            const string source = @"
public interface IDto
{
    string Name { get; set; }
}

public class DtoModel : IDto
{
    public string Name { get; set; }
}

public class Unrelated
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var interfaceProperty = RoslynTestHelper.GetProperty(compilation, "IDto", "Name");
            var unrelatedProperty = RoslynTestHelper.GetProperty(compilation, "Unrelated", "Name");

            var result = _sut.SatisfiesRequiredProperty(interfaceProperty, unrelatedProperty);

            Assert.False(result);
        }

        [Fact]
        public void PropertyOnATypeThatDoesNotImplementTheRequiredInterfaceProperty_IsNotSatisfied()
        {
            const string source = @"
public interface IDto
{
    string Name { get; set; }
}

public interface IOther
{
    string Name { get; set; }
}

public class OtherModel : IOther
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var interfaceProperty = RoslynTestHelper.GetProperty(compilation, "IDto", "Name");
            var otherProperty = RoslynTestHelper.GetProperty(compilation, "OtherModel", "Name");

            var result = _sut.SatisfiesRequiredProperty(interfaceProperty, otherProperty);

            Assert.False(result);
        }

        [Fact]
        public void NonInterfaceRequiredProperty_OnlyMatchesTheExactSameSymbol()
        {
            const string source = @"
public class BaseModel
{
    public string Name { get; set; }
}

public class DtoModel : BaseModel
{
    public string Id { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var baseName = RoslynTestHelper.GetProperty(compilation, "BaseModel", "Name");
            var id = RoslynTestHelper.GetProperty(compilation, "DtoModel", "Id");

            var result = _sut.SatisfiesRequiredProperty(baseName, id);

            Assert.False(result);
        }
    }
}
