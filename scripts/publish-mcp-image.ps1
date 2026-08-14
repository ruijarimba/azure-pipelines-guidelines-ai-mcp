<#
.SYNOPSIS
Builds and publishes the MCP container image to Docker Hub.

.DESCRIPTION
Use this script only when an approved Docker image release is ready to publish.
The script validates configuration and platform support before logging in or starting
a build, then publishes the multi-architecture latest tag configured by DOCKERHUB_IMAGE.
For local builds and validation, use build-mcp-image.ps1 instead.

.PARAMETER EnvironmentFile
The path to the Docker Hub environment file. Defaults to .env at the repository root.

.EXAMPLE
./publish-mcp-image.ps1

.EXAMPLE
./publish-mcp-image.ps1 -EnvironmentFile .env.publish

.NOTES
Requires Docker Desktop with its Linux engine, Docker Buildx, and an environment file
containing Docker Hub settings. The token is passed to Docker through stdin and is never
written to output or the image. The publish is this script's only remote side effect.
Copy .env.example to .env and set DOCKERHUB_USERNAME, DOCKERHUB_TOKEN, and
DOCKERHUB_IMAGE before running this script. The Docker Hub token grants remote
registry access and must not be committed or passed as a command-line argument.
#>
[CmdletBinding()]
param(
    [string]$EnvironmentFile = ".env"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$environmentPath = if ([System.IO.Path]::IsPathRooted($EnvironmentFile)) {
    $EnvironmentFile
}
else {
    Join-Path $repoRoot $EnvironmentFile
}

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

    # ConvertFrom-StringData handles blank lines, comments, whitespace, and the first
    # equals sign. Normalize optional matching quotes because Docker settings are simple
    # scalar values rather than shell expressions or multiline dotenv values.
    $values = ConvertFrom-StringData -StringData (Get-Content -LiteralPath $Path -Raw) -ErrorAction Stop
    foreach ($key in @($values.Keys)) {
        $value = ([string]$values[$key]).Trim()
        if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        elseif ($value.Length -ge 2 -and $value.StartsWith("'") -and $value.EndsWith("'")) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        $values[$key] = $value
    }

    return $values
}

function Test-DockerHubConfiguration {
    param(
        [Parameter(Mandatory)]
        [string]$Username,
        [Parameter(Mandatory)]
        [string]$Token,
        [Parameter(Mandatory)]
        [string]$Image
    )

    # Validate the shape without echoing any configured value.
    if ($Username -notmatch '^[a-z0-9][a-z0-9_-]{2,254}$') {
        throw "DOCKERHUB_USERNAME must be a valid Docker Hub username."
    }

    if ($Token -notmatch '^dckr_pat_.+$') {
        throw "DOCKERHUB_TOKEN must be a Docker Hub personal access token starting with 'dckr_pat_'."
    }

    if ($Image -notmatch '^[a-z0-9]+(?:[._-][a-z0-9]+)*/[a-z0-9]+(?:[._-][a-z0-9]+)*$') {
        throw "DOCKERHUB_IMAGE must be a Docker Hub repository in the form 'username/repository' without a tag."
    }
}

function Test-DockerPrerequisites {
    # Check the daemon explicitly so a stopped Docker Desktop instance fails
    # before the script attempts authentication.
    if ($null -eq (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw "Docker was not found on PATH. Install Docker Desktop and try again."
    }

    & docker info --format '{{.ServerVersion}}' *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Desktop is not running or its Linux engine is unavailable. Start Docker Desktop and try again."
    }

    & docker buildx version *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Buildx is unavailable. Update Docker Desktop and try again."
    }

    # Bootstrap the active builder and verify both image platforms are available.
    $builderOutput = & docker buildx inspect --bootstrap 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Buildx could not initialize a builder. Check that Docker Desktop is running and try again."
    }

    $platforms = ($builderOutput | Where-Object { $_ -match '^Platforms:' }) -join ' '
    if ($platforms -notmatch 'linux/amd64' -or $platforms -notmatch 'linux/arm64') {
        throw "Docker Buildx must support linux/amd64 and linux/arm64 for this publish."
    }
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

Test-DockerHubConfiguration -Username $username -Token $token -Image $image
Test-DockerPrerequisites

Set-Location $repoRoot

# Never add the token to command arguments or diagnostic output.
$token | & docker login --username $username --password-stdin
if ($LASTEXITCODE -ne 0) {
    throw "Docker Hub login failed. Check DOCKERHUB_USERNAME and DOCKERHUB_TOKEN in .env."
}

$imageTag = "${image}:latest"

# Publish the latest multi-architecture image for Docker Hub consumers.
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
