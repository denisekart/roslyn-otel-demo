using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TelemetrySourceGenerator.SyntaxHelpers;

namespace TelemetrySourceGenerator;

internal static class TelemetryDecoratorGeneratorExtensions
{
    internal static IncrementalGeneratorInitializationContext RegisterOutputForDecoratedInterfaces(this IncrementalGeneratorInitializationContext initContext)
    {
        var publicInterfacesProvider = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                (node, _) => node is InterfaceDeclarationSyntax decl && decl.Modifiers.Any(SyntaxKind.PublicKeyword),
                (node, _) => (node: (InterfaceDeclarationSyntax)node.Node, semanticModel: node.SemanticModel))
            .Select((x, c) => (
                identifier: x.node.Identifier,
                rootNode: x.node.SyntaxTree.GetRoot(c),
                declaredType: x.semanticModel.GetDeclaredSymbol(x.node) as ITypeSymbol
            ));
        var combinedProviders = publicInterfacesProvider.Combine(initContext.AssemblyInfoProvider());

        initContext.RegisterSourceOutput(combinedProviders,
            (ctx, source) =>
            {
                var (node, (assemblyName, _)) = source;

                // members defined in a base type (if any) and not overriden in current class
                var additionalBaseMembers = node.declaredType?.Interfaces
                    .SelectMany(x => x.GetMembers())
                    .Select(x => x.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MemberDeclarationSyntax)
                    .Where(x => x?.Modifiers.Any(SyntaxKind.NewKeyword) is false)
                    .ToArray();

                var rewriter = new SimpleInterfaceDecoratorSyntaxRewriter(
                    node.identifier,
                    methodName =>
                        StaticStatements.BeforeOperationStatement(assemblyName!, $"Decorated.{{_baseImplementationType.Name}}.{methodName}"),
                    StaticStatements.FailedOperationStatement,
                    StaticStatements.AfterOperationStatement,
                    additionalBaseMembers);
                var generatedDecorator = rewriter
                    .Visit(node.rootNode)
                    .NormalizeWhitespace()
                    .ToFullString();

                ctx.AddSource($"{rewriter.HintSafeGeneratedClassName}.g.cs", generatedDecorator.ToEnableNullableDirectiveDecoratedString());
            });

        return initContext;
    }

    internal static IncrementalGeneratorInitializationContext RegisterOutputForDecoratedInterfaceTypeMaps(this IncrementalGeneratorInitializationContext initContext)
    {
        // all classes in this compilation that implement any interface
        var classesImplementingAnInterfaceProvider = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                (node, _) => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 },
                (node, _) => (node: (ClassDeclarationSyntax)node.Node, node.SemanticModel))
            .Collect();

        // all public interfaces
        var publicInterfacesProvider = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                (node, _) => node is InterfaceDeclarationSyntax decl && decl.Modifiers.Any(SyntaxKind.PublicKeyword),
                (node, _) => (InterfaceDeclarationSyntax)node.Node);

        // combined relevant syntax in assembly
        var relevantSyntaxProvider = publicInterfacesProvider
            .Combine(classesImplementingAnInterfaceProvider)
            .Select((x, c) => (
                identifier: x.Left.Identifier,
                typeParameters: x.Left.TypeParameterList,
                relatedImplementations: x.Right,
                rootNode: x.Left.SyntaxTree.GetRoot(c))
            )
            .Collect();

        var combinedSources = relevantSyntaxProvider
            .Combine(initContext.AssemblyInfoProvider());

        initContext.RegisterSourceOutput(combinedSources,
            (ctx, source) =>
            {
                var (syntax, (assemblyName, _)) = source;
                var typeMap = TextGeneration.GenerateTypeMapTexts(syntax).Distinct();
                var generatedSource = StaticSources.DecoratedTypesMapSource(assemblyName!, typeMap);

                ctx.AddSource("RegistrationExtensionsTypeMap.g.cs", generatedSource);
            });

        return initContext;
    }

    internal static IncrementalGeneratorInitializationContext RegisterOutputForDependencyInjectionExtensions(this IncrementalGeneratorInitializationContext initContext)
    {
        var supportingOperationsProvider = initContext.MetadataReferencesProvider
            .Where(r => r.Display?.Contains("Microsoft.Extensions.DependencyInjection") is true)
            .Collect()
            .Select((x, _) => new
            {
                supportsServiceCollectionOperations = x.Any(d =>
                    d.Display?.Contains("Microsoft.Extensions.DependencyInjection") is true),
                supportsServiceProviderOperations = x.Any(d
                    => d.Display?.Contains("Microsoft.Extensions.DependencyInjection.dll") is true)
            });
        var combinedProvider = initContext.AssemblyInfoProvider()
            .Combine(supportingOperationsProvider);

        initContext.RegisterSourceOutput(combinedProvider,
            (ctx, source) =>
            {
                var ((assemblyName, _), operations) = source;

                if (operations.supportsServiceCollectionOperations)
                {
                    var generatedSource = StaticSources.DecoratorExtensionsSource(assemblyName!);
                    ctx.AddSource("ServiceCollectionDecorationExtensions.g.cs", generatedSource);
                }

                if (operations.supportsServiceProviderOperations)
                {
                    var generatedSource = StaticSources.BuildServiceProviderSource(assemblyName!);
                    ctx.AddSource("ServiceProviderTelemetryExtensions.g.cs", generatedSource);
                }
            });

        return initContext;
    }
    
    internal static IncrementalGeneratorInitializationContext RegisterOutputForTelemetry(this IncrementalGeneratorInitializationContext initContext)
    {
        var existingTelemetryTypeProvider = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                (x, _) => x is ClassDeclarationSyntax { Identifier: { ValueText: "DefaultActivitySource" } } decl && decl.Modifiers.Any(SyntaxKind.StaticKeyword),
                (x, _) => x)
            .Collect();
        var combinedSource = existingTelemetryTypeProvider.Combine(initContext.AssemblyInfoProvider());

        initContext.RegisterSourceOutput(combinedSource,
            (ctx, source) =>
            {
                var (existingType, (assemblyName, assemblyVersion)) = source;
                var hasExistingTelemetryTypeDefined = !existingType.IsEmpty
                    && existingType.Any(x => x.SemanticModel.GetDeclaredSymbol(x.Node)?.ContainingNamespace.Name == assemblyName);
                if (hasExistingTelemetryTypeDefined)
                {
                    return;
                }

                var generatedSource = StaticSources.DefaultActivitySourceSource(assemblyName!, assemblyName!, assemblyVersion?.ToString(3) ?? "unspecified");
                ctx.AddSource("DefaultActivitySource.g.cs", generatedSource);
            });

        return initContext;
    }

    internal static IncrementalGeneratorInitializationContext RegisterOutputForWebApplicationExtensions(this IncrementalGeneratorInitializationContext initContext)
    {
        var referencesProvider = initContext.MetadataReferencesProvider
            .Where(r => r.Display?.Contains("Microsoft.AspNetCore") is true)
            .Collect()
            .Select((x, _)
                => x.Any(d => d.Display?.Contains("Microsoft.AspNetCore") is true));

        var combinedSource = initContext.AssemblyInfoProvider()
            .Combine(referencesProvider);

        initContext.RegisterSourceOutput(combinedSource,
            (ctx, source) =>
            {
                var ((assemblyName, _), supportsWebApplicationBuilderOperations) = source;
                if (!supportsWebApplicationBuilderOperations)
                {
                    return;
                }

                var generatedSource = StaticSources.BuildWebHostBuilderSource(assemblyName!);
                ctx.AddSource("WebApplicationTelemetryExtensions.g.cs", generatedSource);
            });

        return initContext;
    }

    internal static IncrementalGeneratorInitializationContext RegisterOutputForInterceptors(this IncrementalGeneratorInitializationContext initContext)
    {
        var combinedProvider = initContext.InterceptorsAggregateProvider();

        initContext.RegisterSourceOutput(combinedProvider,
            (ctx, source) =>
            {
                var (method, symbol, invocations, compilation, assemblyName) = source;

                // find all applicable call sites
                var callers = FindAllCallSites(invocations, symbol);

                if (callers.Length == 0)
                {
                    return;
                }

                // generate an interceptor for each call site
                foreach (var caller in callers)
                {
                    var container = caller.Symbol!.ContainingType;

                    var parameters = GenerateInterceptorParameters(container, caller);
                    var interceptedCall = GenerateInterceptedCall(method, out var isAsync);
                    var modifiers = GenerateInterceptorModifiers(method, isAsync);
                    var interceptorAttributeList = GenerateInterceptorAttributeList(caller, compilation, out var callPosition);
                    var usingDirectives = GenerateUsingDirectives(method);

                    //lang=cs
                    var generated =
                        $$"""
                          #nullable enable
                          namespace {{container.ContainingNamespace}}
                          {
                              {{usingDirectives}}
                              
                              file static class Interceptor
                              {
                                  {{interceptorAttributeList}}
                                  public static {{modifiers}} {{caller.Symbol.ReturnType}} Intercepts{{method.Identifier}}({{parameters}})
                                  {
                                      {{StaticStatements.BeforeOperationStatement(assemblyName!, $"Intercepted.{{@source.GetType().Name}}.{method.Identifier}")}}
                                      try
                                      {
                                          {{interceptedCall}}
                                      }
                                      catch (Exception exception)
                                      {
                                          {{StaticStatements.FailedOperationStatement}}
                                          throw;
                                      }
                                      finally
                                      {
                                          {{StaticStatements.AfterOperationStatement}}
                                      }
                                  }
                              }
                          }
                          {{StaticSources.InterceptsLocationAttributeSource}}
                          #nullable restore
                          """;

                    ctx.AddSource($"Interceptor{container.Name}{method.Identifier}ForCallSite{caller.Symbol.ContainingSymbol.Name}_L{callPosition.Line+1}_C{callPosition.Character+1}.g.cs", generated);
                }
            });

        return initContext;

        (InvocationExpressionSyntax Invocation, IMethodSymbol? Symbol)[] FindAllCallSites(ImmutableArray<(InvocationExpressionSyntax Invocation, ISymbol? CallingSymbol)> invocations, ISymbol? symbol)
        {
            return invocations
                .Where(x => x.CallingSymbol?.Equals(symbol, SymbolEqualityComparer.Default) is true
                    || (x.CallingSymbol is IMethodSymbol c && c.OriginalDefinition.Equals(symbol, SymbolEqualityComparer.Default)))
                .Where(x => x.Invocation.Expression is MemberAccessExpressionSyntax)
                .Select(x => (x.Invocation, Symbol: x.CallingSymbol as IMethodSymbol))
                .ToArray();
        }

        string GenerateInterceptorParameters(INamedTypeSymbol container, (InvocationExpressionSyntax Invocation, IMethodSymbol? Symbol) caller)
        {
            return string.Join(", ",
                new[] { $"this {container!.ToDisplayString()} @source" }
                    .Concat(caller.Symbol!.Parameters
                        .Select(p => $"{p.Type} {p.Name}")));
        }

        string GenerateInterceptedCall(MethodDeclarationSyntax method, out bool isAsync)
        {
            var interceptedMethodInvocation = $"@source.{method.Identifier}({string.Join(", ", method.ParameterList.Parameters.Select(x => x.Identifier))});";
            (var interceptedCall, isAsync) = TextGeneration.GetMethodCallInvocationWithReturnKind(method, interceptedMethodInvocation);
            return interceptedCall;
        }

        string GenerateInterceptorModifiers(MethodDeclarationSyntax method, bool isAsync)
        {
            var modifiers = string.Join(" ",
                method.Modifiers
                    .Select(x => x.Kind())
                    .Except(RemovableMethodModifiers)
                    .Union(isAsync
                        ? new[] { SyntaxKind.AsyncKeyword }
                        : Enumerable.Empty<SyntaxKind>())
                    .Select(SyntaxFactory.Token));
            return modifiers;
        }

        string GenerateInterceptorAttributeList((InvocationExpressionSyntax Invocation, IMethodSymbol? Symbol) caller, Compilation compilation, out LinePosition callPosition)
        {
            var normalizedInvocationPath = TextGeneration.GetInterceptorFilePath(caller.Invocation.SyntaxTree, compilation);
            callPosition = ((MemberAccessExpressionSyntax)caller.Invocation.Expression).Name.GetLocation().GetLineSpan().StartLinePosition;
            return StaticSources.InterceptsLocationAttributeUsageSource(normalizedInvocationPath, callPosition.Line + 1, callPosition.Character + 1);
        }

        string GenerateUsingDirectives(MethodDeclarationSyntax method)
        {
            return string.Join("\n",
                method.SyntaxTree
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(x => $"{x}"));
        }
    }

    #region Helper methods

    private static readonly SyntaxKind[] RemovableMethodModifiers =
    {
        SyntaxKind.PublicKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.StaticKeyword
    };

    private static IncrementalValuesProvider<(MethodDeclarationSyntax Method, ISymbol? Symbol, ImmutableArray<(InvocationExpressionSyntax Invocation, ISymbol? CallingSymbol)> Invocations, Compilation Compilation, string? Name)> InterceptorsAggregateProvider(this IncrementalGeneratorInitializationContext initContext)
    {
        var combinedMetadataProvider = initContext
            .AssemblyInfoProvider()
            .Combine(initContext.TargetFrameworksProvider())
            .Combine(initContext.CompilationProvider)
            .Select((x, _) => (
                AssemblyInfo: x.Left.Left,
                Targets: x.Left.Right,
                Compilation: x.Right));

        var interceptableMembersProvider = initContext.SyntaxProvider.CreateSyntaxProvider(
                (x, _) =>
                    x is TypeDeclarationSyntax decl
                    && (decl.IsKind(SyntaxKind.ClassDeclaration) || decl.IsKind(SyntaxKind.InterfaceDeclaration))
                    && decl.Modifiers.Any(SyntaxKind.PublicKeyword)
                    && (decl.IsKind(SyntaxKind.InterfaceDeclaration) || decl.Members.Any(m => m.Modifiers.Any(SyntaxKind.PublicKeyword))),
                (x, _) => (SyntaxNode: (TypeDeclarationSyntax)x.Node, x.SemanticModel))
            .SelectMany((x, _) => x.SyntaxNode.Members
                .Where(m =>
                    m is MethodDeclarationSyntax
                    && (x.SyntaxNode is InterfaceDeclarationSyntax || m.Modifiers.Any(SyntaxKind.PublicKeyword)))
                .Select(m => (
                    Method: (MethodDeclarationSyntax)m,
                    Symbol: x.SemanticModel.GetDeclaredSymbol(m))));

        var interceptableMemberCallersProvider = initContext.SyntaxProvider.CreateSyntaxProvider(
                (x, _) => x is InvocationExpressionSyntax,
                (x, _) => (Invocation: (InvocationExpressionSyntax)x.Node, CallingSymbol: x.SemanticModel.GetSymbolInfo(x.Node).Symbol))
            .Collect();

        var combinedProvider = interceptableMembersProvider
            .Combine(interceptableMemberCallersProvider)
            .Where(x => x.Right.Any())
            .Combine(combinedMetadataProvider)
            .Select((x, _) => (
                x.Left.Left.Method,
                x.Left.Left.Symbol,
                Invocations: x.Left.Right,
                x.Right.Compilation,
                x.Right.AssemblyInfo.Name));
        return combinedProvider;
    }

    private static IncrementalValueProvider<(string? Name, Version Version)> AssemblyInfoProvider(this IncrementalGeneratorInitializationContext initContext)
    {
        var assemblyInfoSource = initContext.CompilationProvider
            .Select((x, _) => (name: x.AssemblyName, version: x.Assembly.Identity.Version));

        return assemblyInfoSource;
    }

    private static IncrementalValueProvider<(bool Net7, bool Net8)> TargetFrameworksProvider(this IncrementalGeneratorInitializationContext initContext)
    {
        var targetsSource = initContext.MetadataReferencesProvider
            .Collect()
            .Select((x, _) =>
            {
                return (
                    Net7: x.Any(m => m.Display?.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}net7.0{Path.DirectorySeparatorChar}") is true),
                    Net8: x.Any(m => m.Display?.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}net8.0{Path.DirectorySeparatorChar}") is true)
                );
            });
        return targetsSource;
    }

    #endregion
}
