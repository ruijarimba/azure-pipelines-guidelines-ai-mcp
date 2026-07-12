[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\integration-smoke')
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$cliProject = Join-Path $repositoryRoot 'tools\AzurePipelines.Guidelines.Cli\AzurePipelines.Guidelines.Cli.csproj'
$cliDll = Join-Path $repositoryRoot 'tools\AzurePipelines.Guidelines.Cli\bin\Release\net10.0\AzurePipelines.Guidelines.Cli.dll'
$fixturesRoot = Join-Path $repositoryRoot 'tests\AzurePipelines.Guidelines.Integration.Tests\Fixtures\PipelineRepositories'

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

dotnet build $cliProject --configuration Release --nologo
if ($LASTEXITCODE -ne 0) {
    throw 'CLI build failed.'
}

$repositories = Get-ChildItem $fixturesRoot -Directory | Sort-Object Name
foreach ($repository in $repositories) {
    $reportPath = Join-Path $OutputDirectory "$($repository.Name).json"

    dotnet $cliDll analyze $repository.FullName --format json --output $reportPath --soft-fail
    if ($LASTEXITCODE -ne 0) {
        throw "CLI analysis failed for $($repository.Name)."
    }

    $report = Get-Content $reportPath -Raw | ConvertFrom-Json
    $summary = $report.summary
    Write-Host "$($repository.Name): $($summary.filesScanned) file(s), $($summary.totalViolations) diagnostic(s) -> $reportPath"
}
