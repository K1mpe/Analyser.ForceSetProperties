using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class MarkAssignmentsTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void ObjectInitializerAssignments_AreAllMarked()
        {
            const string source = @"
public class DtoModel
{
    public string Name { get; set; }
    public string Id { get; set; }
}

public class Factory
{
    [ForceSetProperties]
    public DtoModel Create()
    {
        return new DtoModel { Name = ""x"", Id = ""y"" };
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            _sut.MarkAssignments(context, node, semanticModel, methodName: null);

            Assert.True(context.IsFullySet);
        }

        [Fact]
        public void PostCreationAssignment_IsMarked()
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
        var dto = new DtoModel();
        dto.Name = ""x"";
        return dto;
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            _sut.MarkAssignments(context, node, semanticModel, methodName: null);

            Assert.True(context.IsFullySet);
        }

        [Fact]
        public void ConstructorBodyAssignment_IsMarkedWhenScanningTheConstructorItself()
        {
            const string source = @"
public class DtoModel
{
    [ForceSetProperties]
    public DtoModel(string id)
    {
        Id = id;
    }

    public string Id { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var constructor = RoslynTestHelper.GetConstructor(compilation, "DtoModel");
            var context = RoslynTestHelper.BuildContext(compilation, constructor);
            var node = RoslynTestHelper.GetSyntaxNode(constructor);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            _sut.MarkAssignments(context, node, semanticModel, methodName: null);

            Assert.True(context.IsFullySet);
        }

        [Fact]
        public void StopsMarkingAssignmentsOnceEverythingIsSet()
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
        var dto = new DtoModel { Name = ""x"" };
        dto.Name = ""y"";
        return dto;
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            _sut.MarkAssignments(context, node, semanticModel, methodName: null);

            Assert.Single(context.RequiredProperties[0].SetLocations);
        }

        [Fact]
        public void NoAssignmentsAtAll_LeavesPropertiesUnset()
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
        return new DtoModel();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            _sut.MarkAssignments(context, node, semanticModel, methodName: null);

            Assert.False(context.IsFullySet);
        }
    }
}
