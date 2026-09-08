<#
.SYNOPSIS
Starts the published MCP container through Docker Compose.

.DESCRIPTION
Use this script when testing the published container path or the HTTP transport.
Docker Compose pulls the configured image before starting the container in the background.
Use run-mcp-local.ps1 when a client needs a directly started stdio process instead.

.EXAMPLE
./run-mcp-compose.ps1

.NOTES
Requires Docker Desktop with its Linux engine running. The service uses HTTP transport
on localhost:8080/mcp. The command changes local Docker state by pulling an image and
starting a background container.
Use docker compose down separately when the service is no longer needed.
#>
[CmdletBinding()]

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Test-CommandAvailable {
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

if (-not (Test-CommandAvailable -Name "docker")) {
    throw "Docker was not found on PATH. Install Docker Desktop and try again."
}

# Confirm that Docker Desktop and the daemon are reachable before trying to start the service.
& docker info *> $null
if ($LASTEXITCODE -ne 0) {
    if (Test-CommandAvailable -Name "wsl.exe") {
        Write-Error "Docker is not running. WSL is available, but Docker Desktop or its WSL integration must be started manually."
    }
    else {
        Write-Error "Docker is not running and WSL was not found. Start Docker Desktop and enable its WSL 2 backend if required."
    }

    exit 1
}

& docker compose version *> $null
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose is not available. Update Docker Desktop and try again."
}

# Pull the latest configured image before starting the service so local testing uses the
# current published image rather than a stale local copy.
& docker compose pull
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& docker compose up --detach
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "MCP HTTP service is available at http://localhost:8080/mcp." -ForegroundColor Green
