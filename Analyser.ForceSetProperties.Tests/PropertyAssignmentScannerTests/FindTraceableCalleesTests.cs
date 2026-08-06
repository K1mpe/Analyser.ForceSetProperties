using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class FindTraceableCalleesTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void MethodInvocation_IsReturned()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    public DtoModel Create()
    {
        return Map();
    }

    private static DtoModel Map()
    {
        return new DtoModel { Name = ""x"" };
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var map = RoslynTestHelper.GetMethod(compilation, "Factory", "Map");
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            var result = _sut.FindTraceableCallees(node, semanticModel);

            Assert.Contains(map, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ConstructorCall_IsReturned()
        {
            const string source = @"
public class DtoModel
{
    public DtoModel(string name)
    {
    }
}

public class Factory
{
    public DtoModel Create()
    {
        return new DtoModel(""x"");
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var constructor = RoslynTestHelper.GetConstructor(compilation, "DtoModel");
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            var result = _sut.FindTraceableCallees(node, semanticModel);

            Assert.Contains(constructor, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void ThisConstructorInitializer_IsReturned()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }

    public DtoModel()
    {
        Name = ""default"";
    }

    public DtoModel(string name) : this()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var constructors = RoslynTestHelper.GetType(compilation, "DtoModel").Constructors;
            var parameterless = constructors.Single(c => c.Parameters.Length == 0);
            var withParameter = constructors.Single(c => c.Parameters.Length == 1);
            var node = RoslynTestHelper.GetSyntaxNode(withParameter);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            var result = _sut.FindTraceableCallees(node, semanticModel);

            Assert.Contains(parameterless, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void BaseConstructorInitializer_IsReturned()
        {
            const string source = @"
public class Base
{
    public string Name { get; set; }

    public Base(string name)
    {
        Name = name;
    }
}

public class Derived : Base
{
    public Derived(string name) : base(name)
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var baseConstructor = RoslynTestHelper.GetConstructor(compilation, "Base");
            var derivedConstructor = RoslynTestHelper.GetConstructor(compilation, "Derived");
            var node = RoslynTestHelper.GetSyntaxNode(derivedConstructor);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            var result = _sut.FindTraceableCallees(node, semanticModel);

            Assert.Contains(baseConstructor, result, SymbolEqualityComparer.Default);
        }

        [Fact]
        public void VirtualMethodCall_IsExcluded()
        {
            const string source = @"
public class Base
{
    public virtual void Map()
    {
    }
}

public class Factory
{
    public void Create(Base source)
    {
        source.Map();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            var result = _sut.FindTraceableCallees(node, semanticModel);

            Assert.Empty(result);
        }

        [Fact]
        public void NoInvocationsOrCreations_ReturnsEmpty()
        {
            const string source = @"
public class Factory
{
    public string Create()
    {
        return ""x"";
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            var result = _sut.FindTraceableCallees(node, semanticModel);

            Assert.Empty(result);
        }
    }
}
