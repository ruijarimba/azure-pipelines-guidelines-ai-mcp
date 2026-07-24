[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

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
        [string]$StandardOutputPath,
        [Parameter(Mandatory)]
        [string]$StandardErrorPath
    )

    $projectPath = Join-Path $repoRoot "tools/AzurePipelines.Guidelines.Mcp.Host"
    $arguments = "run --no-build --no-restore --project `"$projectPath`" --launch-profile $Profile"

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

function Test-McpProfile {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("stdio", "SSE")]
        [string]$Profile
    )

    $temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "adog-mcp-quality-check-$([Guid]::NewGuid())"
    New-Item -ItemType Directory -Path $temporaryDirectory -Force | Out-Null
    $standardOutputPath = Join-Path $temporaryDirectory "stdout.log"
    $standardErrorPath = Join-Path $temporaryDirectory "stderr.log"
    $process = $null

    try {
        $process = Start-McpProfile `
            -Profile $Profile `
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
    dotnet test --no-build --configuration $Configuration --no-restore
}

Invoke-Step -Name "MCP startup" -ScriptBlock {
    Test-McpProfile -Profile "stdio"
    Test-McpProfile -Profile "SSE"
}

Write-Host "`nQuality checks completed successfully." -ForegroundColor Green
