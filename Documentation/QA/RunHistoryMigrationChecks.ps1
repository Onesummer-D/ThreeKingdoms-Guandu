$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managerRoot = Join-Path $projectRoot 'Assets/Scripts/Managers'
$historyPath = Join-Path $managerRoot 'RunHistoryData.cs'
$trackerPath = Join-Path $managerRoot 'RunHistoryTracker.cs'
$fixturePath = Join-Path $PSScriptRoot 'RunHistoryLegacyFixture.json'

$history = Get-Content -LiteralPath $historyPath -Raw -Encoding UTF8
$tracker = Get-Content -LiteralPath $trackerPath -Raw -Encoding UTF8
$fixture = Get-Content -LiteralPath $fixturePath -Raw -Encoding UTF8 | ConvertFrom-Json

foreach ($field in @('shownNodeOrder', 'decisions', 'resourceTimeline', 'advisorEchoes')) {
    if ($history -notmatch "public .* $field") { throw "Legacy field removed: $field" }
    if ($null -eq $fixture.$field) { throw "Fixture missing legacy field: $field" }
}
foreach ($field in @('schemaVersion', 'runId', 'seed', 'participants', 'events', 'miniGames', 'ending')) {
    if ($history -notmatch "public .* $field") { throw "New history field missing: $field" }
}
foreach ($token in @('NormalizeHistory', 'EnsureCollections', 'RebuildLegacyEvents', 'legacy history migrated', 'CloneHistory')) {
    if ($tracker -notmatch [regex]::Escape($token)) { throw "Migration token missing: $token" }
}
if ($tracker -notmatch 'RunEventTypes\.RunStarted') { throw 'Legacy migration must create a RunStarted event.' }
if ($tracker -notmatch 'RunEventTypes\.DecisionMade') { throw 'Legacy migration must create DecisionMade events.' }

Write-Output 'PASS: legacy RunHistory fixture and migration hooks are present.'
