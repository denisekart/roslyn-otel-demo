using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TelemetrySourceGeneratorTests7;

public static class Driver
{
    public static Compilation CreateCompilation(string source) => CSharpCompilation.Create(
        assemblyName: "compilation",
        syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
        references: new[]
        {
            MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceProvider).GetTypeInfo().Assembly.Location)
        },
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
    );

    private static GeneratorDriver CreateDriver(Compilation compilation, params IIncrementalGenerator[] generators) => CSharpGeneratorDriver.Create(
        generators: generators.Select(x => x.AsSourceGenerator()),
        additionalTexts: ImmutableArray<AdditionalText>.Empty,
        parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
        optionsProvider: null
    );

    public static Compilation RunGenerators(Compilation compilation, out ImmutableArray<Diagnostic> diagnostics, params IIncrementalGenerator[] generators)
    {
        CreateDriver(compilation, generators).RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out diagnostics);
        return updatedCompilation;
    }

    public static (Compilation compilation, ImmutableArray<Diagnostic> diagnostics) RunGenerator<T>(this string source) where T : IIncrementalGenerator, new()
    {
        var compilation = CreateCompilation(source);
        var driver = CreateDriver(compilation, new T());

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

        return (updatedCompilation, diagnostics);
    }
}
