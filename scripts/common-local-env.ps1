Set-StrictMode -Version Latest

function Import-LocalEnv {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $envFile = Join-Path $RepoRoot '.env.local'

    if (-not (Test-Path $envFile)) {
        throw "Missing .env.local at: $envFile"
    }

    $values = @{}
    $lines = Get-Content -Path $envFile

    foreach ($line in $lines) {
        $trimmed = $line.Trim()

        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#')) {
            continue
        }

        $separatorIndex = $trimmed.IndexOf('=')
        if ($separatorIndex -lt 1) {
            continue
        }

        $key = $trimmed.Substring(0, $separatorIndex).Trim()
        $value = $trimmed.Substring($separatorIndex + 1).Trim()

        if ($key.Length -eq 0) {
            continue
        }

        $values[$key] = $value
        Set-Item -Path "Env:$key" -Value $value
    }

    return $values
}

function Sync-FrontendEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [hashtable]$EnvValues
    )

    $frontendEnvFile = Join-Path $RepoRoot 'frontend/src/environments/environment.development.ts'

    if (-not (Test-Path $frontendEnvFile)) {
        throw "Missing Angular development environment file: $frontendEnvFile"
    }

    $apiUrl = 'http://localhost:5000/api/v1'
    if ($EnvValues.ContainsKey('CORE_FAMILY_API_URL') -and -not [string]::IsNullOrWhiteSpace($EnvValues['CORE_FAMILY_API_URL'])) {
        $apiUrl = $EnvValues['CORE_FAMILY_API_URL']
    }

    $safeApiUrl = $apiUrl.Replace("'", "\\'")

    $content = @(
        'export const environment = {',
        "  production: false,",
        "  apiUrl: '$safeApiUrl'",
        '};'
    )

    Set-Content -Path $frontendEnvFile -Value $content -Encoding utf8
}