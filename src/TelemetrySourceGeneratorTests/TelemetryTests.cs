using TelemetrySourceGenerator;

namespace TelemetrySourceGeneratorTests7;

public class TelemetryTests
{
    [Fact]
    public void ShouldGenerateDefaultActivitySource()
    {
        // Act
        var (compilation, diagnostics) =
            //lang=cs
            """
            public interface IFoo{}
            """.RunGenerator<TelemetryDecoratorGenerator>();

        // Assert
        diagnostics.Should().BeEmpty();
        compilation.SyntaxTrees.Should().ContainSingle(x => x.ToString().Contains("public static class DefaultActivitySource"));
    }
}
