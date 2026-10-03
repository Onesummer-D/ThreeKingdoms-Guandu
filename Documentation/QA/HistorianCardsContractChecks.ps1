$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$campaignPath = Join-Path $projectRoot 'Assets/Scripts/UI/CampaignMapUI.cs'
if (-not (Test-Path -LiteralPath $campaignPath)) { throw "Missing historian source: $campaignPath" }
$source = Get-Content -LiteralPath $campaignPath -Raw -Encoding UTF8

$cardCount = ([regex]::Matches($source, 'new HistorianCard\(')).Count
if ($cardCount -lt 10 -or $cardCount -gt 15) {
    throw "Historian card count must stay between 10 and 15; found $cardCount."
}
foreach ($token in @(
    'historicalBoundary',
    'OpenHistorianGallery',
    'RebuildHistorianGallery',
    'OpenHistorianCard',
    'historianGalleryOverlay'
)) {
    if ($source -notmatch [regex]::Escape($token)) { throw "Missing historian card hook: $token" }
}
if ($source -notmatch 'button\.interactable = unlocked') {
    throw 'Historian gallery does not gate cards by reached chapter anchors.'
}

Write-Output "PASS: historian gallery contract checks ($cardCount cards)."
