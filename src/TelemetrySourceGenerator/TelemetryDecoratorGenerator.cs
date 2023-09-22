using Microsoft.CodeAnalysis;

namespace TelemetrySourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class TelemetryDecoratorGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context
            .RegisterOutputForTelemetry()
            .RegisterOutputForDecoratedInterfaces()
            .RegisterOutputForDecoratedInterfaceTypeMaps()
            .RegisterOutputForDependencyInjectionExtensions()
            .RegisterOutputForWebApplicationExtensions();
    }
}