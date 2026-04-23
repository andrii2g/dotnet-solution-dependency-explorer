param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Push-Location $scriptDir
try {
    dotnet run --project ./src/DependencyExplorer/DependencyExplorer.csproj -- @Arguments
}
finally {
    Pop-Location
}
