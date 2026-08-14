<#
.SYNOPSIS
Starts the MCP host directly for local stdio development.

.DESCRIPTION
Use this script when an MCP client starts the server as a local child process or when
Arguments after the script parameters are passed to the host process.

.PARAMETER Configuration
The build configuration to run. Valid values are Debug and Release. Defaults to Release.

.EXAMPLE
./run-mcp-local.ps1

.EXAMPLE
./run-mcp-local.ps1 -Configuration Debug -- --transport stdio

.NOTES
Requires the .NET 10 SDK and a restored repository. The selected configuration controls
the build output under .artifacts.
The host keeps stdout reserved for MCP protocol traffic and writes diagnostics to stderr.
The script does not start an HTTP listener unless a transport argument requests one.
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "tools\AzurePipelines.Guidelines.Mcp.Host\AzurePipelines.Guidelines.Mcp.Host.csproj"

Set-Location $repoRoot

# Run the host directly rather than using the container runtime. The host keeps stdout
# reserved for MCP protocol traffic and writes diagnostics to stderr.
& dotnet run --project $projectPath --configuration $Configuration -- @args

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
