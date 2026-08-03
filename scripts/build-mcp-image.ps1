# Build a local MCP image for development and validation. This is separate from
# the published Compose runtime and does not require the publish-only .env file.
[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$ImageTag = "adog-mcp:local"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

# Build a local image for development. This is separate from the published Compose runtime.
& docker build --tag $ImageTag $repoRoot

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
