$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$visualPath = Join-Path $projectRoot 'Assets/Scripts/Managers/VisualDirector.cs'
if (-not (Test-Path -LiteralPath $visualPath)) { throw "Missing file: $visualPath" }
$visual = Get-Content -LiteralPath $visualPath -Raw -Encoding UTF8

foreach ($tier in @('Calm', 'Alert', 'Crisis')) {
    if ($visual -notmatch "VisualTier\.$tier|\b$tier\b") { throw "Missing visual tier: $tier" }
}
foreach ($token in @('AudioManager.Instance.PlayAlert', 'CameraShake', 'ApplySceneElements',
        'RunHistoryTracker.Instance.RecordSceneState', 'cameraShake', 'sceneIntensity')) {
    if ($visual -notmatch [regex]::Escape($token)) { throw "Missing visual response hook: $token" }
}

Write-Output 'PASS: VisualDirector three-tier response hooks are present.'
