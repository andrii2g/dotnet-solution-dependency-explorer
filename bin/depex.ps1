param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Push-Location $repoRoot
try {
    if ($Arguments.Count -gt 0 -and $Arguments[0] -eq "analyze") {
        $solutionPath = $null

        for ($i = 1; $i -lt $Arguments.Count; $i++) {
            if ($Arguments[$i] -eq "--solution" -and ($i + 1) -lt $Arguments.Count) {
                $solutionPath = $Arguments[$i + 1]
                break
            }

            if ($Arguments[$i] -like "--solution=*") {
                $solutionPath = $Arguments[$i].Substring("--solution=".Length)
                break
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($solutionPath)) {
            dotnet restore $solutionPath
        }
    }

    dotnet run --project ./src/DependencyExplorer/DependencyExplorer.csproj -- @Arguments
}
finally {
    Pop-Location
}
