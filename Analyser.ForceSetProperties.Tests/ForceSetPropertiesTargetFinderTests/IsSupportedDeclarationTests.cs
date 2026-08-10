using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetPropertiesTargetFinderTests
{
    /// <summary>
    /// Known-issues.md #12 - a method with no body (an interface method signature or an `abstract`
    /// method declaration) is a MethodDeclarationSyntax like any other, so it must be explicitly
    /// excluded here rather than falling through as if it were a real, checkable method.
    /// </summary>
    public class IsSupportedDeclarationTests
    {
        private readonly ForceSetPropertiesTargetFinder _sut = new();

        [Fact]
        public void OrdinaryMethodWithABlockBody_ReturnsTrue()
        {
            const string source = @"
public class Factory
{
    public object Create()
    {
        return new object();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);

            Assert.True(_sut.IsSupportedDeclaration(node));
        }

        [Fact]
        public void ExpressionBodiedMethod_ReturnsTrue()
        {
            const string source = @"
public class Factory
{
    public object Create() => new object();
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);

            Assert.True(_sut.IsSupportedDeclaration(node));
        }

        [Fact]
        public void InterfaceMethodSignatureWithNoBody_ReturnsFalse()
        {
            const string source = @"
public class Dto
{
    public string Name { get; set; }
}

public interface IFactory
{
    Dto Create();
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "IFactory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);

            Assert.False(_sut.IsSupportedDeclaration(node));
        }

        [Fact]
        public void DefaultInterfaceMethodImplementation_ReturnsTrue()
        {
            const string source = @"
public class Dto
{
    public string Name { get; set; }
}

public interface IFactory
{
    Dto Create() => new Dto { Name = ""x"" };
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "IFactory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);

            Assert.True(_sut.IsSupportedDeclaration(node));
        }

        [Fact]
        public void AbstractMethodDeclaration_ReturnsFalse()
        {
            const string source = @"
public class Dto
{
    public string Name { get; set; }
}

public abstract class FactoryBase
{
    public abstract Dto Create();
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "FactoryBase", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);

            Assert.False(_sut.IsSupportedDeclaration(node));
        }

        [Fact]
        public void OverrideOfAnAbstractMethod_ReturnsTrue()
        {
            const string source = @"
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
    public override Dto Create() => new Dto { Name = ""x"" };
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var node = RoslynTestHelper.GetSyntaxNode(create);

            Assert.True(_sut.IsSupportedDeclaration(node));
        }

        [Fact]
        public void Constructor_ReturnsTrue()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }

    public DtoModel(string name)
    {
        Name = name;
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetConstructor(compilation, "DtoModel");
            var node = RoslynTestHelper.GetSyntaxNode(constructor);

            Assert.True(_sut.IsSupportedDeclaration(node));
        }

        [Fact]
        public void Property_ReturnsTrue()
        {
            const string source = @"
using System;
using System.Linq.Expressions;

public class DbModel
{
    public string Name { get; set; }
}

public class DtoModel
{
    public string Name { get; set; }
}

public class Factory
{
    public static Expression<Func<DbModel, DtoModel>> FromExpression => db => new DtoModel();
}";
            var compilation = RoslynTestHelper.Compile(source);
            var property = RoslynTestHelper.GetMember(compilation, "Factory", "FromExpression");
            var node = RoslynTestHelper.GetSyntaxNode(property);

            Assert.True(_sut.IsSupportedDeclaration(node));
        }

        [Fact]
        public void ClassDeclaration_ReturnsFalse()
        {
            const string source = @"
public class DtoModel
{
}";
            var compilation = RoslynTestHelper.Compile(source);
            var dtoModel = RoslynTestHelper.GetType(compilation, "DtoModel");
            var node = RoslynTestHelper.GetSyntaxNode(dtoModel);

            Assert.False(_sut.IsSupportedDeclaration(node));
        }
    }
}
