Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'common-local-env.ps1')

$envValues = Import-LocalEnv -RepoRoot $repoRoot
Sync-FrontendEnvironment -RepoRoot $repoRoot -EnvValues $envValues

$frontendRoot = Join-Path $repoRoot 'frontend'

if (-not (Test-Path $frontendRoot)) {
    throw "Frontend folder was not found at: $frontendRoot"
}

if (-not (Test-Path (Join-Path $frontendRoot 'node_modules'))) {
    Write-Host 'node_modules not found. Running npm install first...' -ForegroundColor Yellow
    npm --prefix $frontendRoot install
}

Write-Host 'Loaded .env.local, synced Angular environment, and starting frontend...' -ForegroundColor Green
npm --prefix $frontendRoot start -- --host 127.0.0.1 --port 4200