[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Normalize-Text {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Text,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot
    )

    $normalized = $Text -replace "`r`n", "`n" -replace "`r", "`n"
    $normalized = $normalized.Replace($RepositoryRoot.Replace("\", "/"), ".")
    $normalized = $normalized.Replace($RepositoryRoot, ".")

    $lines = $normalized.Split("`n") | ForEach-Object { $_.TrimEnd() }
    $normalized = [string]::Join("`n", $lines)
    $normalized = [regex]::Replace($normalized, "(`n){3,}", "`n`n")
    return $normalized.Trim() + "`n"
}

function Compare-NormalizedFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ExpectedPath,

        [Parameter(Mandatory = $true)]
        [string] $ActualPath,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot
    )

    if (-not (Test-Path $ExpectedPath)) {
        throw "Missing snapshot file: $ExpectedPath"
    }

    if (-not (Test-Path $ActualPath)) {
        throw "Missing generated file: $ActualPath"
    }

    $expected = Normalize-Text -Text ([IO.File]::ReadAllText($ExpectedPath)) -RepositoryRoot $RepositoryRoot
    $actual = Normalize-Text -Text ([IO.File]::ReadAllText($ActualPath)) -RepositoryRoot $RepositoryRoot

    if ($expected -eq $actual) {
        return
    }

    Write-Host "Mismatch: $ActualPath" -ForegroundColor Red
    $diff = Compare-Object ($expected -split "`n") ($actual -split "`n") -SyncWindow 2 |
        Select-Object -First 20

    foreach ($entry in $diff) {
        $prefix = if ($entry.SideIndicator -eq "=>") { "+" } else { "-" }
        Write-Host "$prefix $($entry.InputObject)" -ForegroundColor Yellow
    }

    throw "Snapshot comparison failed for $ActualPath"
}

function Invoke-Analyzer {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ToolDll,

        [Parameter(Mandatory = $true)]
        [string] $SolutionPath,

        [Parameter(Mandatory = $true)]
        [string] $OutputDirectory
    )

    if (Test-Path $OutputDirectory) {
        Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    & dotnet $ToolDll analyze --solution $SolutionPath --output $OutputDirectory --level all --verbose
    if ($LASTEXITCODE -ne 0) {
        throw "Analyzer failed for $SolutionPath"
    }
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repositoryRoot "src\DependencyExplorer\DependencyExplorer.csproj"
$toolOutput = Join-Path $repositoryRoot "artifacts\validation\tool"
$validationRoot = Join-Path $repositoryRoot "artifacts\validation\runs"
$examplesRoot = Join-Path $repositoryRoot "docs\examples"

Write-Host "Publishing analyzer..." -ForegroundColor Cyan
& dotnet publish $projectPath -c Debug -o $toolOutput /p:UseAppHost=false
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed"
}

$toolDll = Join-Path $toolOutput "DependencyExplorer.dll"

$layeredSolution = Join-Path $repositoryRoot "samples\Fixtures\LayeredSample\LayeredSample.slnx"
$mixedSolution = Join-Path $repositoryRoot "samples\Fixtures\MixedLegacySample\MixedLegacySample.slnx"
$cycleSolution = Join-Path $repositoryRoot "samples\Fixtures\CycleSample\CycleSample.slnx"

$layeredOutput = Join-Path $validationRoot "LayeredSample"
$mixedOutput = Join-Path $validationRoot "MixedLegacySample"
$cycleOutput = Join-Path $validationRoot "CycleSample"

Invoke-Analyzer -ToolDll $toolDll -SolutionPath $layeredSolution -OutputDirectory $layeredOutput
Invoke-Analyzer -ToolDll $toolDll -SolutionPath $mixedSolution -OutputDirectory $mixedOutput
Invoke-Analyzer -ToolDll $toolDll -SolutionPath $cycleSolution -OutputDirectory $cycleOutput

Compare-NormalizedFile -ExpectedPath (Join-Path $examplesRoot "LayeredSample\graph-projects.mmd") -ActualPath (Join-Path $layeredOutput "graph-projects.mmd") -RepositoryRoot $repositoryRoot
Compare-NormalizedFile -ExpectedPath (Join-Path $examplesRoot "LayeredSample\graph-namespaces.mmd") -ActualPath (Join-Path $layeredOutput "graph-namespaces.mmd") -RepositoryRoot $repositoryRoot
Compare-NormalizedFile -ExpectedPath (Join-Path $examplesRoot "LayeredSample\summary.md") -ActualPath (Join-Path $layeredOutput "summary.md") -RepositoryRoot $repositoryRoot
Compare-NormalizedFile -ExpectedPath (Join-Path $examplesRoot "MixedLegacySample\violations.md") -ActualPath (Join-Path $mixedOutput "violations.md") -RepositoryRoot $repositoryRoot
Compare-NormalizedFile -ExpectedPath (Join-Path $examplesRoot "CycleSample\summary.md") -ActualPath (Join-Path $cycleOutput "summary.md") -RepositoryRoot $repositoryRoot
Compare-NormalizedFile -ExpectedPath (Join-Path $examplesRoot "CycleSample\violations.md") -ActualPath (Join-Path $cycleOutput "violations.md") -RepositoryRoot $repositoryRoot

Write-Host "Fixture validation passed." -ForegroundColor Green
