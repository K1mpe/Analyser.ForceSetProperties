using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.Extensions;

public static class AssertExtensions
{
    public static ContextModel ContainsProperties(this ContextModel context, params string[] propertyNames)
    {
        context.RequiredProperties.ContainsProperties(propertyNames);
        return context;
    }
    public static void ContainsProperties(this IEnumerable<RequiredProperty> properties, params string[] propertyNames)
    {
        foreach(var propertyName in propertyNames)
        {
            if (!properties.Any(p => p.Name == propertyName))
            {
                throw new Exception($"Expected property '{propertyName}' was not found.");
            }
        }
    }

    public static ContextModel HasDestinationType(this ContextModel context, string destinationType, Compilation compilation)
    {
        var destinationTypeSymbol = compilation.GetTypeByMetadataName(destinationType);
        Assert.Equal(destinationTypeSymbol, context.DestinationType, SymbolEqualityComparer.Default);
        return context;
    }

    public static IReadOnlyList<ContextModel> CreateContextModel(this Compilation compilation, string className, string methodName)
    {
        var create = RoslynTestHelper.GetMethod(compilation, className, methodName);
        var attribute = RoslynTestHelper.GetForceSetPropertiesAttribute(create);
        var node = RoslynTestHelper.GetSyntaxNode(create);

        var builder = new ContextBuilder();
        var contexts = builder.Build(create, node, attribute, compilation);
        return contexts;
    }
}
