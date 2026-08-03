# Build and publish the MCP container image to Docker Hub using the publish-only
# .env file for credentials and repository settings.
[CmdletBinding()]
param(
    [string]$EnvironmentFile = ".env"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$environmentPath = Join-Path $repoRoot $EnvironmentFile

# Load publish-only Docker Hub settings. The runtime Compose file is intentionally
# independent from this environment file so credentials stay out of normal runs.

function Read-EnvironmentFile {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Environment file '$Path' was not found. Copy .env.example to .env and set the Docker Hub values."
    }

    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^\s*(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>.*)\s*$') {
            $value = $Matches.value.Trim()
            if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
                $value = $value.Substring(1, $value.Length - 2)
            }
            elseif ($value.Length -ge 2 -and $value.StartsWith("'") -and $value.EndsWith("'")) {
                $value = $value.Substring(1, $value.Length - 2)
            }

            $values[$Matches.key] = $value
        }
    }

    return $values
}

function Get-RequiredValue {
    param(
        [Parameter(Mandatory)]
        [hashtable]$Values,
        [Parameter(Mandatory)]
        [string]$Name
    )

    if (-not $Values.ContainsKey($Name) -or [string]::IsNullOrWhiteSpace($Values[$Name])) {
        throw "The '$Name' value is missing from the environment file."
    }

    return $Values[$Name]
}

function Invoke-Docker {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & docker @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Docker command failed with exit code $LASTEXITCODE."
    }
}

$values = Read-EnvironmentFile -Path $environmentPath
$username = Get-RequiredValue -Values $values -Name "DOCKERHUB_USERNAME"
$token = Get-RequiredValue -Values $values -Name "DOCKERHUB_TOKEN"
$image = Get-RequiredValue -Values $values -Name "DOCKERHUB_IMAGE"

# Fail fast when the publish configuration is incomplete or still uses the template token.

if ($token -eq "replace-with-a-docker-hub-access-token") {
    throw "Replace the placeholder DOCKERHUB_TOKEN value in .env with a Docker Hub access token."
}

if ($null -eq (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker was not found on PATH. Install Docker Desktop and try again."
}

Set-Location $repoRoot

# Ensure Buildx is available before we attempt the multi-architecture publish.
Invoke-Docker -Arguments @("buildx", "version")

$token | & docker login --username $username --password-stdin
if ($LASTEXITCODE -ne 0) {
    throw "Docker Hub login failed. Check DOCKERHUB_USERNAME and DOCKERHUB_TOKEN in .env."
}

$imageTag = "${image}:latest"

# Publish a multi-architecture image tagged as latest for Docker Hub consumers.
Invoke-Docker -Arguments @(
    "buildx",
    "build",
    "--platform",
    "linux/amd64,linux/arm64",
    "--tag",
    $imageTag,
    "--push",
    "."
)

Write-Host "Published $imageTag for linux/amd64 and linux/arm64." -ForegroundColor Green
