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

            foreach (var callee in FindTraceableCallees(node, semanticModel))
            {
                if (context.IsFullySet)
                    return;

                if (!visited.Add(callee))
                    continue;

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
                .FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(p.Symbol, leftSymbol));

            if (requiredProperty == null)
                return;

            var lineSpan = assignment.GetLocation().GetLineSpan();
            requiredProperty.SetLocations.Add(new SetLocation(lineSpan.Path, lineSpan.StartLinePosition.Line + 1, methodName));
        }

        public IEnumerable<IMethodSymbol> FindTraceableCallees(SyntaxNode node, SemanticModel semanticModel)
        {
            foreach (var invocation in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && CanTrace(method))
                {
                    yield return method;
                }
            }

            foreach (var creation in node.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>())
            {
                if (semanticModel.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor && CanTrace(constructor))
                {
                    yield return constructor;
                }
            }

            foreach (var initializer in node.DescendantNodesAndSelf().OfType<ConstructorInitializerSyntax>())
            {
                if (semanticModel.GetSymbolInfo(initializer).Symbol is IMethodSymbol chainedConstructor && CanTrace(chainedConstructor))
                {
                    yield return chainedConstructor;
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
