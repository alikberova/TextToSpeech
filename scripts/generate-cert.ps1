# ./generate-cert.ps1

$repoRoot = Split-Path -Parent $PSScriptRoot
$certDir = Join-Path $repoRoot "TextToSpeech.Web\nginx\certs"

$openssl = if ($IsWindows) {
    "C:\Program Files\Git\usr\bin\openssl.exe"
} else {
    "openssl"
}

if (-not (Get-Command $openssl -ErrorAction SilentlyContinue)) {
    throw "OpenSSL was not found."
}

New-Item -ItemType Directory -Force -Path $certDir | Out-Null

& $openssl req -x509 -newkey rsa:2048 -nodes `
    -keyout (Join-Path $certDir "privkey.pem") `
    -out (Join-Path $certDir "fullchain.pem") `
    -days 365 `
    -subj "/CN=localhost" `
    -addext "subjectAltName=DNS:localhost"

if ($LASTEXITCODE -ne 0) {
    throw "OpenSSL certificate generation failed with exit code $LASTEXITCODE."
}