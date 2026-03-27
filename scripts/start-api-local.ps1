Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'common-local-env.ps1')

$envValues = Import-LocalEnv -RepoRoot $repoRoot

if (-not $envValues.ContainsKey('ASPNETCORE_ENVIRONMENT') -or [string]::IsNullOrWhiteSpace($envValues['ASPNETCORE_ENVIRONMENT'])) {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
}

$apiProject = Join-Path $repoRoot 'backend/CoreFamily.API'

if (-not (Test-Path $apiProject)) {
    throw "API project folder was not found at: $apiProject"
}

Write-Host "Loaded .env.local and starting API..." -ForegroundColor Green
Set-Location $apiProject
dotnet run --urls http://localhost:5000