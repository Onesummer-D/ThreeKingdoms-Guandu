$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managerRoot = Join-Path $projectRoot 'Assets/Scripts/Managers'
$trackerPath = Join-Path $managerRoot 'RunHistoryTracker.cs'
$dataPath = Join-Path $managerRoot 'RunEventData.cs'
$historyPath = Join-Path $managerRoot 'RunHistoryData.cs'
$diagnosticsPath = Join-Path $managerRoot 'RunHistoryDiagnostics.cs'
$finalUiPath = Join-Path $managerRoot 'FinalUIManager.cs'

foreach ($path in @($trackerPath, $dataPath, $historyPath, $diagnosticsPath, $finalUiPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing file: $path" }
}

$data = Get-Content -LiteralPath $dataPath -Raw -Encoding UTF8
$tracker = Get-Content -LiteralPath $trackerPath -Raw -Encoding UTF8
$history = Get-Content -LiteralPath $historyPath -Raw -Encoding UTF8
$diagnostics = Get-Content -LiteralPath $diagnosticsPath -Raw -Encoding UTF8
$finalUi = Get-Content -LiteralPath $finalUiPath -Raw -Encoding UTF8

$eventNames = @(
    'RunStarted', 'NodeShown', 'DecisionMade', 'ResourceChanged',
    'MiniGameStarted', 'MiniGameAction', 'MiniGameCompleted',
    'AdvisorSuggested', 'SceneStateChanged', 'EndingReached', 'RunCompleted'
)

foreach ($name in $eventNames) {
    if ($data -notmatch "public const string $name") { throw "Missing event constant: $name" }
    if ($tracker -notmatch "RunEventTypes\.$name") { throw "Missing tracker hook: $name" }
}

foreach ($field in @('schemaVersion', 'runId', 'seed', 'participants', 'events', 'miniGames', 'ending')) {
    if ($history -notmatch "public .* $field") { throw "Missing RunHistoryData field: $field" }
}

foreach ($method in @('ExportJson', 'TryValidate', 'BuildSummary')) {
    if ($diagnostics -notmatch "public static .* $method") { throw "Missing diagnostics API: $method" }
}

foreach ($method in @('RecordMiniGameStarted', 'RecordMiniGameAction', 'RecordMiniGameCompleted')) {
    if ($tracker -notmatch "public void $method") { throw "Missing tracker API: $method" }
}
foreach ($hook in @('HandleNodeShown', 'HandleOptionSelected', 'HandleResourceChanged')) {
    if ($tracker -notmatch "private void $hook") { throw "Missing tracker event handler: $hook" }
}

if (($finalUi -split 'RecordMiniGameStarted\(').Count -ne 2) {
    throw 'FinalUIManager must record exactly one mini-game start hook.'
}
if (($finalUi -split 'RecordMiniGameCompleted\(').Count -ne 2) {
    throw 'FinalUIManager must record exactly one mini-game completion hook.'
}

Write-Output "PASS: RunEvent protocol and integration checks ($($eventNames.Count) event types)."
