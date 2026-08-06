using System.Linq;
using Analyser.ForceSetProperties.Emission;
using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Analyser.ForceSetProperties.Tests.TestHelpers
{
    /// <summary>
    /// Compiles small, self-contained code snippets and hands back the Roslyn symbols
    /// a test needs, so each test can read like "given this code, expect this result".
    /// </summary>
    public static class RoslynTestHelper
    {
        private static readonly MetadataReference[] References = BuildReferences();

        // The attribute is generated source, not a compiled type, so tests include the same
        // source text the generator injects into real consumers rather than referencing a DLL.
        private static readonly SyntaxTree AttributeTree = CSharpSyntaxTree.ParseText(AttributeEmitter.Source);

        private static MetadataReference[] BuildReferences()
        {
            var trustedPlatformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(System.IO.Path.PathSeparator);

            return trustedPlatformAssemblies
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .ToArray();
        }

        public static Compilation Compile(string source)
        {
            var tree = CSharpSyntaxTree.ParseText("using Analyser.ForceSetProperties;\n" + source);

            return CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { AttributeTree, tree },
                references: References,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        public static MetadataReference CompileToReference(string source)
        {
            var compilation = Compile(source);
            using var stream = new System.IO.MemoryStream();
            var result = compilation.Emit(stream);

            if (!result.Success)
            {
                var errors = string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
                throw new System.InvalidOperationException($"Failed to compile reference assembly:\n{errors}");
            }

            stream.Position = 0;
            return MetadataReference.CreateFromStream(stream);
        }

        public static INamedTypeSymbol GetType(this Compilation compilation, string typeName)
        {
            var type = compilation.GetTypeByMetadataName(typeName);
            if (type == null)
            {
                throw new System.InvalidOperationException($"Type '{typeName}' was not found in the compiled source.");
            }

            return type;
        }

        public static ISymbol GetMember(Compilation compilation, string typeName, string memberName)
        {
            var member = GetType(compilation, typeName).GetMembers(memberName).FirstOrDefault();
            if (member == null)
            {
                throw new System.InvalidOperationException($"Member '{memberName}' was not found on type '{typeName}'.");
            }

            return member;
        }

        public static IMethodSymbol GetMethod(this Compilation compilation, string typeName, string methodName)
        {
            return (IMethodSymbol)GetMember(compilation, typeName, methodName);
        }

        public static IPropertySymbol GetProperty(Compilation compilation, string typeName, string propertyName)
        {
            return (IPropertySymbol)GetMember(compilation, typeName, propertyName);
        }

        public static IFieldSymbol GetField(Compilation compilation, string typeName, string fieldName)
        {
            return (IFieldSymbol)GetMember(compilation, typeName, fieldName);
        }

        public static IMethodSymbol GetConstructor(Compilation compilation, string typeName)
        {
            return GetType(compilation, typeName).Constructors.First(c => !c.IsImplicitlyDeclared);
        }

        public static AttributeData GetForceSetPropertiesAttribute(this ISymbol symbol)
        {
            var attribute = symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == AttributeEmitter.AttributeClassName);

            if (attribute == null)
            {
                throw new System.InvalidOperationException($"'{symbol.Name}' is not annotated with [ForceSetProperties].");
            }

            return attribute;
        }

        // Compile() adds the generated attribute source ahead of the test's own source, so the
        // test's tree is always the last one rather than the compilation's only one.
        public static SyntaxTree GetUserSyntaxTree(this Compilation compilation)
        {
            return compilation.SyntaxTrees.Last();
        }

        public static SyntaxNode GetSyntaxNode(ISymbol symbol)
        {
            var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (reference == null)
            {
                throw new System.InvalidOperationException($"'{symbol.Name}' has no declaring syntax in this compilation.");
            }

            return reference.GetSyntax();
        }

        public static ContextModel BuildContext(Compilation compilation, ISymbol target)
        {
            var node = GetSyntaxNode(target);
            var attribute = GetForceSetPropertiesAttribute(target);

            return new ContextBuilder().Build(target, node, attribute, compilation).Single();
        }
    }
}
