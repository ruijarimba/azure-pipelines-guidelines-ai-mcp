<#
.SYNOPSIS
Builds a local MCP Docker image for development and validation.

.DESCRIPTION
Use this script when you need to build an image from the current source tree for
local validation or client testing without authenticating to a registry. The image
is tagged locally and is not pushed to Docker Hub.

.PARAMETER ImageTag
The local Docker image tag. Defaults to adog-mcp:local.

.EXAMPLE
./build-mcp-image.ps1

.EXAMPLE
./build-mcp-image.ps1 -ImageTag adog-mcp:test

.NOTES
Requires Docker Desktop with its Linux engine running. The publish-only .env file
is not required. The repository root is used as the Docker build context so the
Dockerfile can copy all required project files. Use publish-mcp-image.ps1 only when
a registry push is intended.
#>
[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$ImageTag = "adog-mcp:local"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

# Use the repository root as the Docker build context so the Dockerfile can copy the
# project files it needs. The default tag is intended for local Compose or client tests.
& docker build --tag $ImageTag $repoRoot

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
