<#
.SYNOPSIS
Runs the repository quality gate for the MCP host and container runtime.

.DESCRIPTION
The script restores, builds, and tests the selected configuration, starts both MCP profiles,
then starts and stops Docker Compose. Use this script before committing or pushing changes
that affect code, build settings, Docker behavior, or solution configuration.
It is intentionally broader than a unit-test run because startup and container integration
failures can otherwise remain hidden.

.PARAMETER Configuration
The build configuration to validate. Defaults to Release.

.EXAMPLE
./quality-check.ps1

.EXAMPLE
./quality-check.ps1 -Configuration Debug

.NOTES
Requires the .NET 10 SDK and Docker Desktop. Build outputs and test results are written
under the ignored .artifacts directory.
The Docker Compose container and network are removed during cleanup after the runtime check.
The MCP profile processes are also stopped after their startup checks complete.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
$artifactsRoot = Join-Path $repoRoot ".artifacts"
$testResultsDirectory = Join-Path $artifactsRoot "test-results"
# Keep test result files with the other generated artifacts instead of the repository root.
New-Item -ItemType Directory -Path $testResultsDirectory -Force | Out-Null

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$ScriptBlock
    )

    Write-Host "`n==> $Name" -ForegroundColor Cyan
    & $ScriptBlock
}

function Start-McpProfile {
    param(
        [Parameter(Mandatory)]
        [string]$Profile,
        [Parameter(Mandatory)]
        [string]$Configuration,
        [Parameter(Mandatory)]
        [string]$StandardOutputPath,
        [Parameter(Mandatory)]
        [string]$StandardErrorPath
    )

    $projectPath = Join-Path $repoRoot "tools/AzurePipelines.Guidelines.Mcp.Host"
    $arguments = "run --no-build --no-restore --configuration $Configuration --project `"$projectPath`" --launch-profile $Profile"

    Start-Process `
        -FilePath "dotnet" `
        -ArgumentList $arguments `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $StandardOutputPath `
        -RedirectStandardError $StandardErrorPath `
        -PassThru
}

function Wait-ForMcpStdio {
    param(
        [Parameter(Mandatory)]
        [System.Diagnostics.Process]$Process,
        [Parameter(Mandatory)]
        [string]$StandardErrorPath,
        [int]$TimeoutSeconds = 15
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            $errorOutput = Get-Content -Path $StandardErrorPath -Raw -ErrorAction SilentlyContinue
            throw "MCP stdio profile exited before startup. $errorOutput"
        }

        $errorOutput = Get-Content -Path $StandardErrorPath -Raw -ErrorAction SilentlyContinue
        if ($errorOutput -match "MCP stdio server is running") {
            return
        }

        Start-Sleep -Milliseconds 250
    }

    throw "MCP stdio profile did not report startup within $TimeoutSeconds seconds."
}

function Wait-ForMcpSse {
    param(
        [Parameter(Mandatory)]
        [System.Diagnostics.Process]$Process,
        [int]$Port = 5050,
        [int]$TimeoutSeconds = 15
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            throw "MCP SSE profile exited before listening on port $Port."
        }

        $client = [System.Net.Sockets.TcpClient]::new()
        try {
            $connectTask = $client.ConnectAsync("127.0.0.1", $Port)
            $connected = $connectTask.Wait(500)
            if ($connected -and $client.Connected) {
                return
            }
        }
        finally {
            $client.Dispose()
        }

        Start-Sleep -Milliseconds 250
    }

    throw "MCP SSE profile did not listen on port $Port within $TimeoutSeconds seconds."
}

function Wait-ForComposeContainer {
    param(
        [int]$TimeoutSeconds = 45
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $client = [System.Net.Sockets.TcpClient]::new()
        try {
            $connectTask = $client.ConnectAsync("127.0.0.1", 8080)
            $connected = $connectTask.Wait(500)
            if ($connected -and $client.Connected) {
                return
            }
        }
        finally {
            $client.Dispose()
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Docker Compose service did not become reachable on port 8080 within $TimeoutSeconds seconds."
}

function Test-ComposeRuntime {
    # Build and run the image locally so this gate validates the current source tree.
    # The finally block below always removes the temporary Compose container and network.
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw "Docker was not found on PATH. Install Docker Desktop and try again."
    }

    & docker compose config --quiet
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose config failed."
    }

    # Build the image locally so the quality gate validates the repository runtime
    # without requiring a published Docker Hub image to exist in the current environment.
    & docker compose build --no-cache
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose build failed."
    }

    & docker compose up --detach
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose up failed."
    }

    try {
        Wait-ForComposeContainer
        Write-Host "Docker Compose runtime started successfully." -ForegroundColor Green
    }
    finally {
        & docker compose down --remove-orphans
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose down failed."
        }
    }
}

function Test-McpProfile {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("stdio", "SSE")]
        [string]$Profile,
        [Parameter(Mandatory)]
        [string]$Configuration
    )

    $temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "adog-mcp-quality-check-$([Guid]::NewGuid())"
    New-Item -ItemType Directory -Path $temporaryDirectory -Force | Out-Null
    $standardOutputPath = Join-Path $temporaryDirectory "stdout.log"
    $standardErrorPath = Join-Path $temporaryDirectory "stderr.log"
    $process = $null

    try {
        $process = Start-McpProfile `
            -Profile $Profile `
            -Configuration $Configuration `
            -StandardOutputPath $standardOutputPath `
            -StandardErrorPath $standardErrorPath

        if ($Profile -eq "stdio") {
            Wait-ForMcpStdio -Process $process -StandardErrorPath $standardErrorPath
        }
        else {
            Wait-ForMcpSse -Process $process
        }

        Write-Host "MCP $Profile profile started successfully." -ForegroundColor Green
    }
    finally {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            $process.WaitForExit(5000) | Out-Null
        }

        if (Test-Path $temporaryDirectory) {
            Remove-Item -Path $temporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Invoke-Step -Name "Restore" -ScriptBlock {
    dotnet restore
}

Invoke-Step -Name "Build" -ScriptBlock {
    dotnet build --no-restore --configuration $Configuration
}

Invoke-Step -Name "Test" -ScriptBlock {
    dotnet test --no-build --configuration $Configuration --no-restore --results-directory $testResultsDirectory
}

Invoke-Step -Name "MCP startup" -ScriptBlock {
    Test-McpProfile -Profile "stdio" -Configuration $Configuration
    Test-McpProfile -Profile "SSE" -Configuration $Configuration
}

Invoke-Step -Name "Docker Compose runtime" -ScriptBlock {
    Test-ComposeRuntime
}

Write-Host "`nQuality checks completed successfully." -ForegroundColor Green
