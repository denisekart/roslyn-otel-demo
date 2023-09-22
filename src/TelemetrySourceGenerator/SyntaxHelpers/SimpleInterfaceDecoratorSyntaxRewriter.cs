using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TelemetrySourceGenerator.SyntaxHelpers;

public class SimpleInterfaceDecoratorSyntaxRewriter(SyntaxToken interfaceIdentifier,
    Func<string, StatementSyntax?>? beforeMethodCall,
    StatementSyntax? onExceptionInMethodCall,
    StatementSyntax? afterMethodCall,
    MemberDeclarationSyntax?[]? additionalBaseMembers = null)
    : CSharpSyntaxRewriter
{
    public string HintSafeGeneratedClassName => _generatedClassIdentifier.ValueText.Replace("_", string.Empty);

    private readonly SyntaxToken _generatedClassIdentifier = Identifier(GenerateClassName(interfaceIdentifier));

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var bodyImplementation = GenerateMethodBody(node, out var isAsync);

        var catchClause = CatchClause()
            .WithDeclaration(CatchDeclaration(IdentifierName("System.Exception"))
                .WithIdentifier(Identifier("exception")));
        if (onExceptionInMethodCall is not null)
        {
            catchClause = catchClause.WithBlock(Block()
                .AddStatements(onExceptionInMethodCall, ThrowStatement()));
        }

        var finallyBlock = afterMethodCall is null ? null : Block(afterMethodCall);
        var bodyTryFinallyWrapper = TryStatement(bodyImplementation, SingletonList(catchClause), FinallyClause(finallyBlock));
        var body = Block();
        if (beforeMethodCall is not null && beforeMethodCall.Invoke(node.Identifier.ValueText) is { } statement)
        {
            body = body.AddStatements(statement);
        }

        body = body.AddStatements(bodyTryFinallyWrapper);

        if (isAsync)
        {
            node = node.AddModifiers(Token(SyntaxKind.AsyncKeyword));
        }

        return node
            .WithBody(body)
            .WithSemicolonToken(Token(SyntaxKind.None))
            .AddModifiers(Token(SyntaxKind.PublicKeyword));
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.Identifier != _generatedClassIdentifier)
        {
            // stop parsing, we do not need this branch
            return null;
        }

        return base.VisitClassDeclaration(node);
    }

    public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        if (node.Identifier != interfaceIdentifier)
        {
            // stop parsing, we do not need this branch
            return null;
        }

        var isGenericType = node.TypeParameterList?.Parameters.Any() is true;
        var baseImplementationType = isGenericType
            ? GenericName(interfaceIdentifier, TypeArgumentList(SeparatedList(node.TypeParameterList!.Parameters.Select(p => ParseTypeName(p.Identifier.ValueText)))))
            : ParseTypeName(interfaceIdentifier.ValueText);
        var constructor = ConstructorDeclaration(_generatedClassIdentifier)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("baseImplementation"))
                    .WithType(baseImplementationType))
            .AddBodyStatements(
                ParseStatement("_baseImplementation = baseImplementation;"),
                ParseStatement("_baseImplementationType = baseImplementation.GetType();"));
        var baseImplementationField = FieldDeclaration(
                VariableDeclaration(baseImplementationType)
                    .AddVariables(VariableDeclarator("_baseImplementation")))
            .AddModifiers(Token(SyntaxKind.PrivateKeyword),
                Token(SyntaxKind.ReadOnlyKeyword));
        var baseImplementationTypeField = FieldDeclaration(
                VariableDeclaration(IdentifierName("System.Type"))
                    .AddVariables(VariableDeclarator("_baseImplementationType")))
            .AddModifiers(Token(SyntaxKind.PrivateKeyword),
                Token(SyntaxKind.ReadOnlyKeyword));

        var classDeclaration = ClassDeclaration(_generatedClassIdentifier)
            .WithTypeParameterList(node.TypeParameterList)
            .WithMembers(node.Members)
            .AddMembers(constructor)
            .AddMembers(baseImplementationField)
            .AddMembers(baseImplementationTypeField)
            .WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(
                BaseList(
                    SingletonSeparatedList<BaseTypeSyntax>(
                        SimpleBaseType(
                            baseImplementationType
                        )
                    )
                )
            );

        if (additionalBaseMembers?.Any() is true)
        {
            classDeclaration = classDeclaration.AddMembers(additionalBaseMembers!);
        }

        return base.VisitClassDeclaration(classDeclaration);
    }

    public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        return null;
    }

    private static BlockSyntax GenerateMethodBody(MethodDeclarationSyntax method, out bool isAsync)
    {
        isAsync = false;

        var arguments = ArgumentList(
            SeparatedList<ArgumentSyntax>(
                method.ParameterList.Parameters
                    .Select(p => Argument(IdentifierName(p.Identifier.ValueText)))
                    .SelectMany(p => new[]
                    {
                        Token(SyntaxKind.CommaToken),
                        (SyntaxNodeOrToken)p
                    })
                    .Skip(1)
            )
        );

        var invocation = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("_baseImplementation"),
                IdentifierName(method.Identifier)
            )
        ).WithArgumentList(arguments);

        var isGenericTask = method.ReturnType is GenericNameSyntax { Identifier.ValueText: "Task" };
        if (isGenericTask)
        {
            isAsync = true;
            return Block(ReturnStatement(AwaitExpression(invocation)));
        }

        var isTask = method.ReturnType is IdentifierNameSyntax { Identifier.ValueText: "Task" };
        if (isTask)
        {
            isAsync = true;
            return Block(ExpressionStatement(AwaitExpression(invocation)));
        }

        var doesNotReturn = method.ReturnType is PredefinedTypeSyntax pt && pt.Keyword.IsKind(SyntaxKind.VoidKeyword);

        return doesNotReturn
            ? Block(ExpressionStatement(invocation))
            : Block(ReturnStatement(invocation));
    }

    public static string? GenerateNamespace(SyntaxNode tree)
    {
        var candidate =
            tree.DescendantNodesAndSelf().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name
            ?? tree.DescendantNodesAndSelf().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault()?.Name;

        return candidate?.GetText().ToString();
    }

    public static string GenerateClassName(SyntaxToken identifier)
    {
        return $"__Decorated_{identifier.ValueText}";
    }
}
