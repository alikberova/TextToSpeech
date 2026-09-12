# ./start-test-env.ps1

$repoRoot = Split-Path -Parent $PSScriptRoot

$envFile = Join-Path $repoRoot ".env"
$testEnvFile = Join-Path $repoRoot ".env.test"
$tempEnvFile = Join-Path $env:TEMP "tts-env-$([Guid]::NewGuid()).env"

try {
    Get-Content $envFile, $testEnvFile | Set-Content $tempEnvFile

    $env:OVERRIDE_ENV_FILE = $tempEnvFile

    Push-Location $repoRoot

    docker network create elk 2>$null
    docker compose --env-file $tempEnvFile up --build
}
finally {
    Pop-Location -ErrorAction SilentlyContinue
    Remove-Item Env:OVERRIDE_ENV_FILE -ErrorAction SilentlyContinue
    Remove-Item $tempEnvFile -Force -ErrorAction SilentlyContinue
}
