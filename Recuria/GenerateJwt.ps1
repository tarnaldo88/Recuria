param(
    [Parameter(Mandatory = $false)]
    [string]$OrgId,

    [Parameter(Mandatory = $false)]
    [string]$UserId = "00000000-0000-0000-0000-000000000002",

    [Parameter(Mandatory = $false)]
    [int]$Hours = 4
)

$ErrorActionPreference = "Stop"

$configPath = Join-Path $PSScriptRoot "Recuria.Api\appsettings.Development.json"
if (-not (Test-Path $configPath)) {
    throw "Config not found: $configPath"
}

$config = Get-Content -Raw $configPath | ConvertFrom-Json
$issuer = $config.Jwt.Issuer
$audience = $config.Jwt.Audience
$key = $config.Jwt.SigningKey

if ([string]::IsNullOrWhiteSpace($issuer)) { throw "Jwt:Issuer is missing." }
if ([string]::IsNullOrWhiteSpace($audience)) { throw "Jwt:Audience is missing." }
if ([string]::IsNullOrWhiteSpace($key)) { throw "Jwt:SigningKey is missing." }
if ($key.Length -lt 32) { throw "Jwt:SigningKey must be at least 32 characters." }

if ([string]::IsNullOrWhiteSpace($OrgId)) {
    $OrgId = [Guid]::NewGuid().ToString()
}

$now = Get-Date
$iat = [int][double]::Parse($now.ToUniversalTime().Subtract([datetime]'1970-01-01').TotalSeconds)
$exp = [int][double]::Parse($now.AddHours($Hours).ToUniversalTime().Subtract([datetime]'1970-01-01').TotalSeconds)

$header = @{ alg = "HS256"; typ = "JWT" } | ConvertTo-Json -Compress
$payload = @{
    iss = $issuer
    aud = $audience
    sub = $UserId
    org_id = $OrgId
    iat = $iat
    exp = $exp
} | ConvertTo-Json -Compress

function Base64UrlEncode([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$headerB64 = Base64UrlEncode([Text.Encoding]::UTF8.GetBytes($header))
$payloadB64 = Base64UrlEncode([Text.Encoding]::UTF8.GetBytes($payload))
$unsigned = "$headerB64.$payloadB64"

$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = [Text.Encoding]::UTF8.GetBytes($key)
$signature = Base64UrlEncode($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned)))

$jwt = "$unsigned.$signature"

Write-Output $jwt
