# Download the module
$module_url = "https://raw.githubusercontent.com/open-telemetry/opentelemetry-dotnet-instrumentation/v1.0.1/OpenTelemetry.DotNet.Auto.psm1"
$download_path = Join-Path $env:temp "OpenTelemetry.DotNet.Auto.psm1"
Invoke-WebRequest -Uri $module_url -OutFile $download_path -UseBasicParsing

# Import the module to use its functions
Import-Module $download_path

# Install core files (online vs offline method)
Install-OpenTelemetryCore

# Set up the instrumentation for the current PowerShell session
Register-OpenTelemetryForCurrentSession -OTelServiceName "MyServiceDisplayName"

$Env:ASPNETCORE_URLS = "http://localhost:5171"
$Env:ASPNETCORE_CONTENTROOT = "."
$Env:ASPNETCORE_ENVIRONMENT = "Development"
$Env:OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED = 1
$Env:OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED = 1
$Env:OTEL_TRACES_SAMPLER = "always_on"
$Env:OTEL_TRACES_EXPORTER = "otlp"
$Env:OTEL_EXPORTER_OTLP_PROTOCOL = "grpc"
$Env:OTEL_DOTNET_AUTO_TRACES_ENABLED = 1


# Run your application with instrumentation
.\src\GitHubStatsWebApi\bin\Debug\net7.0\GitHubStatsWebApi.exe