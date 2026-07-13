[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$ImageTag = "adog-mcp:local"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

& docker build --tag $ImageTag $repoRoot

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
