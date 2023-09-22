using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TelemetrySourceGenerator.SyntaxHelpers;

public static class StaticStatements
{
    public static StatementSyntax BeforeOperationStatement(string @namespace, string activityName) 
        => ParseStatement($"var activity = {@namespace}.DefaultActivitySource.ActivitySource.StartActivity($\"{activityName}\");");

    public static readonly StatementSyntax FailedOperationStatement 
        = ParseStatement("activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, exception.Message);");

    public static readonly StatementSyntax AfterOperationStatement 
        = ParseStatement("activity?.SetStatus(activity.Status == System.Diagnostics.ActivityStatusCode.Unset ? System.Diagnostics.ActivityStatusCode.Ok : activity.Status, activity.StatusDescription).Dispose();");
}
