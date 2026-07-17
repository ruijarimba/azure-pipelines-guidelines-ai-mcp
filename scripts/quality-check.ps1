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
    if (Test-Path ./coverage) {
        Remove-Item ./coverage -Recurse -Force
    }

    dotnet test --no-build --configuration $Configuration --no-restore `
        --collect:"XPlat Code Coverage" --results-directory ./coverage
}

Invoke-Step -Name "Coverage" -ScriptBlock {
    [hashtable]$lineHits = @{}
    $reports = @(Get-ChildItem ./coverage -Recurse -Filter coverage.cobertura.xml)

    if ($reports.Count -eq 0) {
        throw "No Cobertura coverage reports were generated."
    }

    foreach ($report in $reports) {
        [xml]$coverage = Get-Content $report.FullName

        foreach ($package in @($coverage.coverage.packages.package)) {
            $projectName = [string]$package.name
            $project = @("Core", "Parsing", "Rules", "Analysis", "Mcp", "Cli") |
                Where-Object { $projectName -match "AzurePipelines\.Guidelines\.$_" } |
                Select-Object -First 1

            if (-not $project) {
                continue
            }

            foreach ($class in @($package.classes.class)) {
                $file = ([string]$class.filename).Replace('/', '\')
                if ($file -match '(^|[/\\])(obj|bin|tests)([/\\]|$)') {
                    continue
                }

                $sourceRoot = if ($project -eq "Cli") { "tools" } else { "src" }
                $marker = "AzurePipelines.Guidelines.$project"
                $markerIndex = $file.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase)
                if ($markerIndex -ge 0) {
                    $file = "$sourceRoot\$marker" + $file.Substring($markerIndex + $marker.Length)
                } else {
                    $file = "$sourceRoot\$marker\$file"
                }

                foreach ($line in @($class.lines.line)) {
                    $key = "$file|$([string]$line.number)"
                    $hits = [int]$line.hits
                    if (-not $lineHits.ContainsKey($key) -or $hits -gt $lineHits[$key]) {
                        $lineHits[$key] = $hits
                    }
                }
            }
        }
    }

    $totalLines = $lineHits.Count
    $coveredLines = @($lineHits.Values | Where-Object { $_ -gt 0 }).Count
    $coveragePercent = if ($totalLines -eq 0) { 0 } else { 100 * $coveredLines / $totalLines }
    Write-Host ("Production line coverage: {0:N2}% ({1}/{2})" -f $coveragePercent, $coveredLines, $totalLines)

    if ($coveragePercent -le 95) {
        throw "Production line coverage must be strictly greater than 95%; measured {0:N2}%." -f $coveragePercent
    }
}

Write-Host "`nQuality checks completed successfully." -ForegroundColor Green
