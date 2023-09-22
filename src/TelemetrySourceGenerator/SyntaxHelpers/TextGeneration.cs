using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TelemetrySourceGenerator.SyntaxHelpers;

internal static class TextGeneration
{
    /// <summary>
    /// Produces a collection of texts containing a mapping between interfaces (left) and the corresponding generated class names (right)
    /// <code>
    /// { "Namespace.IInterface{TType}", "Namespace.__Decorated_IInterface{TType}" } 
    /// </code>
    /// </summary>
    public static IEnumerable<(string, string)> GenerateTypeMapTexts(ImmutableArray<(SyntaxToken identifier, TypeParameterListSyntax? typeParameters, ImmutableArray<(ClassDeclarationSyntax node, SemanticModel semanticModel)> relatedImplementations, SyntaxNode rootNode)> source) =>
        source.SelectMany(s =>
        {
            var genericTypeVariations = s.relatedImplementations
                .SelectMany(impl => impl.node.BaseList!.Types.Where(baseType => baseType is SimpleBaseTypeSyntax { Type: GenericNameSyntax }).Select(x => (baseType: x, impl.semanticModel)))
                .Select(x => (name: (GenericNameSyntax)x.baseType.Type, x.semanticModel))
                .Where(x => x.name.Identifier.ValueText == s.identifier.ValueText)
                .Select(x => x.name.TypeArgumentList.Arguments.Select(arg => ModelExtensions.GetSymbolInfo(x.semanticModel, arg).Symbol as INamedTypeSymbol))
                .ToArray();
            var ns = SimpleInterfaceDecoratorSyntaxRewriter.GenerateNamespace(s.rootNode);
            var generatedClassName = SimpleInterfaceDecoratorSyntaxRewriter.GenerateClassName(s.identifier);
            var interfaceName = s.identifier.ValueText;

            if (genericTypeVariations is { Length: > 0 })
            {
                return genericTypeVariations
                    .Select(v => string.Join(",", v.Select(a => $"{a!.ContainingNamespace.Name}.{a.Name}")))
                    .Select(v => ($"{ns}.{interfaceName}<{v}>", $"{ns}.{generatedClassName}<{v}>"));
            }

            return new[] { ($"{ns}.{interfaceName}", $"{ns}.{generatedClassName}") };
        });
    
    /// <summary>
    /// Returns a valid intercepted invocation file path normalized and applicable for interception
    /// </summary>
    public static string GetInterceptorFilePath(SyntaxTree tree, Compilation compilation)
    {
        return compilation.Options.SourceReferenceResolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
    }

    internal static (string text, bool isAsync) GetMethodCallInvocationWithReturn(MethodDeclarationSyntax method, string interceptedMethodInvocation)
    {
        return method switch
        {
            _ when method.ReturnType is GenericNameSyntax { Identifier.ValueText: "Task" } => ($"return await {interceptedMethodInvocation}", true),
            _ when method.ReturnType is IdentifierNameSyntax { Identifier.ValueText: "Task" } => ($"await {interceptedMethodInvocation}", true),
            _ when method.ReturnType is PredefinedTypeSyntax p && p.Keyword.IsKind(SyntaxKind.VoidKeyword) => ($"{interceptedMethodInvocation}", false),
            _ => ($"return {interceptedMethodInvocation}", false)
        };
    }
}
