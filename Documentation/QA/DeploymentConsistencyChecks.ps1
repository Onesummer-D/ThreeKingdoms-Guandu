$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$invite = Join-Path $projectRoot 'Assets/Scripts/UI/InviteCoCreationUI.cs'
$historian = Join-Path $projectRoot 'Assets/Scripts/UI/HistorianAgentClient.cs'
$server = Join-Path $projectRoot 'server/public-advisor/server.js'
$historianServer = Join-Path $projectRoot 'server/historian-ai/grounding.js'
foreach ($path in @($invite, $historian, $server, $historianServer)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing deployment artifact: $path" }
}
$inviteText = Get-Content -Raw $invite
$historianText = Get-Content -Raw $historian
$serverText = Get-Content -Raw $server
$historianServerText = Get-Content -Raw $historianServer
$urls = @([regex]::Matches($inviteText, 'https?://[^"\s]+') | ForEach-Object Value) + @([regex]::Matches($historianText, 'https?://[^"\s]+') | ForEach-Object Value)
if ($urls.Count -lt 2 -or ($urls | Select-Object -Unique).Count -ne 1) { throw "Unity clients do not share one public advisor base URL." }
if ($urls[0] -notmatch '^http://81\.70\.40\.146:8080$') { throw "Unexpected configured public advisor URL: $($urls[0])" }
foreach ($token in @('OPENAI_TIMEOUT_MS', 'AbortController', 'grounded_fallback')) {
    if ($historianServerText -notmatch [regex]::Escape($token)) { throw "Historian server is missing $token" }
}
if ($serverText -notmatch 'FEFF') { throw 'Server JSON parser does not strip UTF-8 BOM.' }
if ($serverText -notmatch 'clientRequestIds\.has\(requestId\)') { throw 'Advisor idempotency hook is missing.' }
Write-Output "PASS: deployment consistency checks (URL $($urls[0]))."
