[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "tools\AzurePipelines.Guidelines.Mcp.Host\AzurePipelines.Guidelines.Mcp.Host.csproj"

Set-Location $repoRoot
& dotnet run --project $projectPath --configuration $Configuration -- @args

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
