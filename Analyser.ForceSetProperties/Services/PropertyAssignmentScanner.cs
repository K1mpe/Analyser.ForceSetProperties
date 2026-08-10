using System.Collections.Generic;
using System.Linq;
using Analyser.ForceSetProperties.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyser.ForceSetProperties.Services
{
    public class PropertyAssignmentScanner
    {
        public void Scan(ContextModel context, Compilation compilation)
        {
            if (context.IsFullySet)
                return;

            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            ScanNode(context, context.TargetNode, compilation, methodName: null, visited);
        }

        public void ScanNode(ContextModel context, SyntaxNode node, Compilation compilation, string? methodName, HashSet<IMethodSymbol> visited)
        {
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            MarkAssignments(context, node, semanticModel, methodName);

            if (context.IsFullySet)
                return;

            MarkWithExpressionCopiedProperties(context, node, semanticModel);

            if (context.IsFullySet)
                return;

            foreach (var (callee, arguments) in FindTraceableCallees(node, semanticModel))
            {
                if (context.IsFullySet)
                    return;

                if (!visited.Add(callee))
                    continue;

                if (arguments != null && IsPositionalRecordConstructor(callee))
                {
                    MarkPositionalConstructorArguments(context, callee, arguments, callee.Name);
                    continue;
                }

                var calleeNode = GetDeclaringNode(callee);
                if (calleeNode == null)
                    continue;

                ScanNode(context, calleeNode, compilation, callee.Name, visited);
            }
        }


        public void MarkAssignments(ContextModel context, SyntaxNode node, SemanticModel semanticModel, string? methodName)
        {
            foreach (var assignment in node.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>())
            {
                if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                    continue;

                MarkIfRequiredProperty(context, assignment, semanticModel, methodName);

                if (context.IsFullySet)
                    return;

            }
        }

        public void MarkIfRequiredProperty(ContextModel context, AssignmentExpressionSyntax assignment, SemanticModel semanticModel, string? methodName)
        {
            var leftSymbol = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
            if (leftSymbol == null)
                return;

            var requiredProperty = context.RequiredProperties
                .FirstOrDefault(p => SatisfiesRequiredProperty(p.Symbol, leftSymbol));

            if (requiredProperty == null)
                return;

            var lineSpan = assignment.GetLocation().GetLineSpan();
            requiredProperty.SetLocations.Add(new SetLocation(lineSpan.Path, lineSpan.StartLinePosition.Line + 1, methodName));
        }

        /// <summary>
        /// True when assigning <paramref name="leftSymbol"/> satisfies <paramref name="requiredSymbol"/>
        /// - either because they're literally the same symbol, or because <paramref name="requiredSymbol"/>
        /// is an interface property and <paramref name="leftSymbol"/> is the concrete property that
        /// implements it (Known-issues.md #5): when the destination type is an interface, an assignment
        /// like <c>new DtoModel { Name = x }</c> resolves to `DtoModel.Name` - a distinct symbol from
        /// `IDto.Name`, even though it implements it - so raw symbol equality alone would never match.
        /// </summary>
        public bool SatisfiesRequiredProperty(IPropertySymbol requiredSymbol, ISymbol leftSymbol)
        {
            if (SymbolEqualityComparer.Default.Equals(requiredSymbol, leftSymbol))
                return true;

            if (requiredSymbol.ContainingType?.TypeKind != TypeKind.Interface || leftSymbol is not IPropertySymbol leftProperty)
                return false;

            var implementation = leftProperty.ContainingType?.FindImplementationForInterfaceMember(requiredSymbol);
            return SymbolEqualityComparer.Default.Equals(implementation, leftProperty);
        }

        /// <summary>
        /// A `with { ... }` expression copies every property from its source instance except the
        /// ones it explicitly overrides - so any required property not named in the initializer is
        /// satisfied by that copy, not missing. Only applies to a with-expression whose own type is
        /// exactly the destination type being validated.
        /// </summary>
        public void MarkWithExpressionCopiedProperties(ContextModel context, SyntaxNode node, SemanticModel semanticModel)
        {
            foreach (var withExpression in node.DescendantNodesAndSelf().OfType<WithExpressionSyntax>())
            {
                if (context.IsFullySet)
                    return;

                var withExpressionType = semanticModel.GetTypeInfo(withExpression).Type;
                if (!SymbolEqualityComparer.Default.Equals(withExpressionType, context.DestinationType))
                    continue;

                var explicitlySetNames = new HashSet<string>(withExpression.Initializer.Expressions
                    .OfType<AssignmentExpressionSyntax>()
                    .Select(assignment => semanticModel.GetSymbolInfo(assignment.Left).Symbol?.Name)
                    .Where(name => name != null)
                    .Select(name => name!));

                var lineSpan = withExpression.GetLocation().GetLineSpan();

                foreach (var property in context.RequiredProperties)
                {
                    if (property.IsSet || explicitlySetNames.Contains(property.Name))
                        continue;

                    property.SetLocations.Add(new SetLocation(lineSpan.Path, lineSpan.StartLinePosition.Line + 1, "with"));
                }
            }
        }

        /// <summary>
        /// Marks the required properties that correspond, by name, to a positional record/record
        /// struct primary constructor's parameters - the compiler synthesizes that mapping with no
        /// assignment syntax to find, so it has to be handled from the call site's arguments directly.
        /// </summary>
        public void MarkPositionalConstructorArguments(ContextModel context, IMethodSymbol constructor, ArgumentListSyntax argumentList, string? methodName)
        {
            for (var i = 0; i < argumentList.Arguments.Count; i++)
            {
                var argument = argumentList.Arguments[i];
                var parameter = ResolveParameter(constructor, argument, i);
                if (parameter == null)
                    continue;

                var requiredProperty = context.RequiredProperties.FirstOrDefault(p => p.Name == parameter.Name);
                if (requiredProperty == null)
                    continue;

                var lineSpan = argument.GetLocation().GetLineSpan();
                requiredProperty.SetLocations.Add(new SetLocation(lineSpan.Path, lineSpan.StartLinePosition.Line + 1, methodName));

                if (context.IsFullySet)
                    return;
            }
        }

        public IParameterSymbol? ResolveParameter(IMethodSymbol constructor, ArgumentSyntax argument, int position)
        {
            if (argument.NameColon != null)
            {
                return constructor.Parameters.FirstOrDefault(p => p.Name == argument.NameColon.Name.Identifier.Text);
            }

            return position < constructor.Parameters.Length ? constructor.Parameters[position] : null;
        }

        /// <summary>
        /// True for a record or record struct's primary constructor specifically - the only
        /// constructor whose declaring syntax is the type declaration itself rather than a
        /// dedicated constructor declaration, since positional parameters have no user-writable
        /// constructor body at all for the compiler to synthesize property assignments into.
        /// </summary>
        public bool IsPositionalRecordConstructor(IMethodSymbol constructor)
        {
            if (constructor.MethodKind != MethodKind.Constructor)
                return false;

            return constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is RecordDeclarationSyntax;
        }

        public IEnumerable<(IMethodSymbol Callee, ArgumentListSyntax? Arguments)> FindTraceableCallees(SyntaxNode node, SemanticModel semanticModel)
        {
            foreach (var invocation in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && CanTrace(method))
                {
                    yield return (method, invocation.ArgumentList);
                }
            }

            // BaseObjectCreationExpressionSyntax covers both `new Person(...)` (ObjectCreationExpressionSyntax)
            // and target-typed `new(...)` (ImplicitObjectCreationExpressionSyntax) - the latter resolves to
            // the exact same constructor symbol and needs to be traced the same way.
            foreach (var creation in node.DescendantNodesAndSelf().OfType<BaseObjectCreationExpressionSyntax>())
            {
                if (semanticModel.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor && CanTrace(constructor))
                {
                    yield return (constructor, creation.ArgumentList);
                }
            }

            foreach (var initializer in node.DescendantNodesAndSelf().OfType<ConstructorInitializerSyntax>())
            {
                if (semanticModel.GetSymbolInfo(initializer).Symbol is IMethodSymbol chainedConstructor && CanTrace(chainedConstructor))
                {
                    yield return (chainedConstructor, initializer.ArgumentList);
                }
            }
        }

        public bool CanTrace(IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.DelegateInvoke)
                return false;

            if (method.IsVirtual || method.IsAbstract || method.IsOverride)
                return false;

            return method.ContainingType?.TypeKind != TypeKind.Interface;
        }

        public SyntaxNode? GetDeclaringNode(IMethodSymbol method)
        {
            return method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        }
    }
}
