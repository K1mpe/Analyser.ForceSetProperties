using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.PropertyAssignmentScannerTests
{
    public class MarkIfRequiredPropertyTests
    {
        private readonly PropertyAssignmentScanner _sut = new();

        [Fact]
        public void AssignmentToARequiredProperty_AddsASetLocation()
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
        return new DtoModel { Name = ""x"" };
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
            var assignment = node.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            _sut.MarkIfRequiredProperty(context, assignment, semanticModel, methodName: null);

            var name = Assert.Single(context.RequiredProperties);
            var setLocation = Assert.Single(name.SetLocations);
            Assert.Null(setLocation.MethodName);
        }

        [Fact]
        public void MethodNameParameter_IsStoredOnTheSetLocation()
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
        return new DtoModel { Name = ""x"" };
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
            var assignment = node.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            _sut.MarkIfRequiredProperty(context, assignment, semanticModel, methodName: "Map");

            var setLocation = context.RequiredProperties[0].SetLocations.Single();
            Assert.Equal("Map", setLocation.MethodName);
        }

        [Fact]
        public void AssignmentToAnUnrelatedVariable_IsIgnored()
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
        string unrelated;
        unrelated = ""x"";
        return new DtoModel { Name = unrelated };
    }
}";
            var compilation = RoslynTestHelper.Compile(source);
            var create = RoslynTestHelper.GetMethod(compilation, "Factory", "Create");
            var context = RoslynTestHelper.BuildContext(compilation, create);
            var node = RoslynTestHelper.GetSyntaxNode(create);
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
            var unrelatedAssignment = node.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .First(a => a.ToString() == "unrelated = \"x\"");

            _sut.MarkIfRequiredProperty(context, unrelatedAssignment, semanticModel, methodName: null);

            Assert.Empty(context.RequiredProperties[0].SetLocations);
        }
    }
}
