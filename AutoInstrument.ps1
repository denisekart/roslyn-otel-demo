# Download the module
$module_url = "https://raw.githubusercontent.com/open-telemetry/opentelemetry-dotnet-instrumentation/v1.0.1/OpenTelemetry.DotNet.Auto.psm1"
$download_path = Join-Path $env:temp "OpenTelemetry.DotNet.Auto.psm1"
Invoke-WebRequest -Uri $module_url -OutFile $download_path -UseBasicParsing

# Import the module to use its functions
Import-Module $download_path

# Install core files (online vs offline method)
Install-OpenTelemetryCore

# Set up the instrumentation for the current PowerShell session
Register-OpenTelemetryForCurrentSession -OTelServiceName "GitHubStatsWebApi"

$Env:ASPNETCORE_URLS = "http://localhost:5003"
$Env:ASPNETCORE_CONTENTROOT = ".\src\GitHubStatsWebApi\bin\Debug\net7.0"

# Run your application with instrumentation
.\src\GitHubStatsWebApi\bin\Debug\net7.0\GitHubStatsWebApi.exe