using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class CanTraceTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void OrdinaryMethod_CanBeTraced()
        {
            const string source = @"
public class Factory
{
    public static void Map()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var map = RoslynTestHelper.GetMethod(compilation, "Factory", "Map");

            Assert.True(_sut.CanTrace(map));
        }

        [Fact]
        public void Constructor_CanBeTraced()
        {
            const string source = @"
public class DtoModel
{
    public DtoModel(string name)
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetConstructor(compilation, "DtoModel");

            Assert.True(_sut.CanTrace(constructor));
        }

        [Fact]
        public void VirtualMethod_CannotBeTraced()
        {
            const string source = @"
public class Base
{
    public virtual void Map()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var map = RoslynTestHelper.GetMethod(compilation, "Base", "Map");

            Assert.False(_sut.CanTrace(map));
        }

        [Fact]
        public void OverriddenMethod_CannotBeTraced()
        {
            const string source = @"
public class Base
{
    public virtual void Map()
    {
    }
}

public class Derived : Base
{
    public override void Map()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var map = RoslynTestHelper.GetMethod(compilation, "Derived", "Map");

            Assert.False(_sut.CanTrace(map));
        }

        [Fact]
        public void AbstractMethod_CannotBeTraced()
        {
            const string source = @"
public abstract class Base
{
    public abstract void Map();
}";
            var compilation = RoslynTestHelper.Compile(source);
            var map = RoslynTestHelper.GetMethod(compilation, "Base", "Map");

            Assert.False(_sut.CanTrace(map));
        }

        [Fact]
        public void InterfaceMethod_CannotBeTraced()
        {
            const string source = @"
public interface IMapper
{
    void Map();
}";
            var compilation = RoslynTestHelper.Compile(source);
            var map = RoslynTestHelper.GetMethod(compilation, "IMapper", "Map");

            Assert.False(_sut.CanTrace(map));
        }

        [Fact]
        public void DelegateInvoke_CannotBeTraced()
        {
            const string source = @"
public class Factory
{
    public System.Action Callback => () => { };
}";
            var compilation = RoslynTestHelper.Compile(source);
            var callback = RoslynTestHelper.GetProperty(compilation, "Factory", "Callback");
            var invokeMethod = ((INamedTypeSymbol)callback.Type).DelegateInvokeMethod!;

            Assert.False(_sut.CanTrace(invokeMethod));
        }
    }
}
