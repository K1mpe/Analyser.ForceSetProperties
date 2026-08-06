using System.Collections.Generic;
using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class ScanNodeTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void PropertySetDirectly_DoesNotTraceIntoCalledMethods()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    [ForceSetProperties]
    public DtoModel Create()
    {
        UnrelatedSideEffect();
        return new DtoModel { Name = ""x"" };
    }

    private static void UnrelatedSideEffect()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            _sut.ScanNode(context, context.TargetNode, compilation, methodName: null, visited);

            Assert.True(context.IsFullySet);
            Assert.Empty(visited);
        }

        [Fact]
        public void PropertyMissingLocally_TracesIntoACalledMethod()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    [ForceSetProperties]
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
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            _sut.ScanNode(context, context.TargetNode, compilation, methodName: null, visited);

            Assert.True(context.IsFullySet);
            var setLocation = context.RequiredProperties[0].SetLocations.Single();
            Assert.Equal("Map", setLocation.MethodName);
        }

        [Fact]
        public void RecursiveMethod_DoesNotInfiniteLoop()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    [ForceSetProperties]
    public DtoModel Create()
    {
        return Map();
    }

    private static DtoModel Map()
    {
        return Map();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            _sut.ScanNode(context, context.TargetNode, compilation, methodName: null, visited);

            Assert.False(context.IsFullySet);
        }

        [Fact]
        public void PropertyStillMissingAfterTracing_LeavesItUnset()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    [ForceSetProperties]
    public DtoModel Create()
    {
        return Map();
    }

    private static DtoModel Map()
    {
        return new DtoModel();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            _sut.ScanNode(context, context.TargetNode, compilation, methodName: null, visited);

            Assert.False(context.IsFullySet);
        }
    }
}
