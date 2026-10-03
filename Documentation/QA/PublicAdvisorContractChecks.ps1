$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$clientPath = Join-Path $projectRoot 'Assets/Scripts/UI/PublicAdvisorClient.cs'
$sessionPath = Join-Path $projectRoot 'Assets/Scripts/UI/InviteSessionState.cs'
$uiPath = Join-Path $projectRoot 'Assets/Scripts/UI/InviteCoCreationUI.cs'
$serverPath = Join-Path $projectRoot 'server/public-advisor/server.js'
foreach ($path in @($clientPath, $sessionPath, $uiPath, $serverPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing public advisor file: $path" }
}
$client = Get-Content -LiteralPath $clientPath -Raw -Encoding UTF8
$session = Get-Content -LiteralPath $sessionPath -Raw -Encoding UTF8
$ui = Get-Content -LiteralPath $uiPath -Raw -Encoding UTF8
$server = Get-Content -LiteralPath $serverPath -Raw -Encoding UTF8

foreach ($token in @('BeginSession', 'SuggestionReceived', 'SubmitDecision', 'StopSession',
        'UnityWebRequest', 'X-Host-Token', 'DownloadHandlerTexture(true)')) {
    if ($client -notmatch [regex]::Escape($token)) { throw "Missing Unity transport hook: $token" }
}
foreach ($token in @('PublicAdvisorClient', 'StartPublicAdvisorSession',
        'RunHistoryTracker.Instance.CurrentRunId', 'HandlePublicSuggestionReceived')) {
    if ($ui -notmatch [regex]::Escape($token)) { throw "Missing UI integration hook: $token" }
}
if ($session -notmatch 'SubmitCloudSuggestion') { throw 'Local session has no cloud suggestion import path.' }
foreach ($token in @('POST', "parts[1] !== 'sessions'", 'suggestions', 'decision',
        'SESSION_TTL_SECONDS', 'clientRequestId', 'joinUrl', 'qr.png', 'historianAudit', 'session_expired')) {
    if ($server -notmatch [regex]::Escape($token)) { throw "Missing server contract token: $token" }
}

Write-Output 'PASS: public advisor server and Unity transport contract checks.'
