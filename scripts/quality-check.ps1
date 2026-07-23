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

Invoke-Step -Name "Restore" -ScriptBlock {
    dotnet restore
}

Invoke-Step -Name "Build" -ScriptBlock {
    dotnet build --no-restore --configuration $Configuration
}

Invoke-Step -Name "Test" -ScriptBlock {
    dotnet test --no-build --configuration $Configuration --no-restore
}

Write-Host "`nQuality checks completed successfully." -ForegroundColor Green
