$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$replayPath = Join-Path $projectRoot 'Assets/Scripts/UI/BattleReplayUI.cs'
$reportPath = Join-Path $projectRoot 'Assets/Scripts/UI/BattleReportUI.cs'
$trackerPath = Join-Path $projectRoot 'Assets/Scripts/Managers/RunHistoryTracker.cs'
foreach ($path in @($replayPath, $reportPath, $trackerPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing replay file: $path" }
}
$replay = Get-Content -LiteralPath $replayPath -Raw -Encoding UTF8
$report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8
$tracker = Get-Content -LiteralPath $trackerPath -Raw -Encoding UTF8

foreach ($token in @('ExportHistory', 'RunEventTypes.DecisionMade', 'RunEventTypes.EndingReached',
        'StartPlayback', 'StopPlayback', 'SelectPrevious', 'SelectNext',
        'SeekFirstDecision', 'SeekEnding', 'OnClosed')) {
    if ($replay -notmatch [regex]::Escape($token)) { throw "Missing replay hook: $token" }
}
if ($report -notmatch 'OnReplayRequested') { throw 'BattleReportUI has no replay entry point.' }
if ($tracker -notmatch 'public RunHistoryData ExportHistory') { throw 'Replay has no history source.' }

Write-Output 'PASS: Battle replay contract and report entry checks.'
