# Start the MCP host directly for stdio-based local development. This remains
# separate from the container runtime path.
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "tools\AzurePipelines.Guidelines.Mcp.Host\AzurePipelines.Guidelines.Mcp.Host.csproj"

Set-Location $repoRoot

# Run the host directly for stdio-based local development rather than using the container runtime.
& dotnet run --project $projectPath --configuration $Configuration -- @args

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
