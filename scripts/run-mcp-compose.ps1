# Start the published MCP container through Docker Compose and report a clear
# diagnostic when Docker or the WSL integration is unavailable.
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

# Pull the latest published image and then start the service in the background.
& docker compose pull
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& docker compose up --detach
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "MCP HTTP service is available at http://localhost:8080/mcp." -ForegroundColor Green
