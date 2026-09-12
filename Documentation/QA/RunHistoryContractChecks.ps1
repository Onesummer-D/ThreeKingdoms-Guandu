$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$dataText = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/RunHistoryData.cs')
$trackerText = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/RunHistoryTracker.cs')
$saveText = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/LocalSaveManager.cs')
$payloadText = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/RunSaveData.cs')

function Require-Text {
    param([string]$Text, [string]$Pattern, [string]$Message)
    if ($Text -notmatch $Pattern) { throw $Message }
}

foreach ($field in @('activeSeconds', 'shownNodeOrder', 'decisions', 'resourceTimeline', 'nodeId', 'optionIndex', 'optionText', 'before', 'after')) {
    Require-Text $dataText ("\b" + $field + "\b") "Run history field is missing: $field"
}

Require-Text $trackerText 'OnOptionSelected\s*\+=' 'Decision event is not observed.'
Require-Text $trackerText 'OnResourceChanged\s*\+=' 'Resource event is not observed.'
Require-Text $trackerText 'AppendSnapshot\(nodeId,\s*"决策' 'Every actual decision must remain on the trend x-axis.'
Require-Text $trackerText 'last\.nodeId\s*==\s*nodeId\s*&&\s*last\.optionIndex\s*==\s*optionIndex' 'Duplicate decision guard is missing.'
Require-Text $trackerText 'RestoreHistory' 'Run history restore is missing.'
Require-Text $payloadText 'RunHistoryData\s+history' 'Save payload does not contain run history.'
Require-Text $saveText 'history\s*=\s*RunHistoryTracker\.Instance' 'Run history is not captured into saves.'
Require-Text $saveText 'RestoreHistory\(data\.history' 'Run history is not restored from saves.'

$restoreStart = $trackerText.IndexOf('public void RestoreHistory', [System.StringComparison]::Ordinal)
$restoreEnd = $trackerText.IndexOf('private void HandleNodeShown', $restoreStart, [System.StringComparison]::Ordinal)
if ($restoreStart -lt 0 -or $restoreEnd -lt 0) { throw 'Cannot inspect RestoreHistory.' }
$restoreSlice = $trackerText.Substring($restoreStart, $restoreEnd - $restoreStart)
if ($restoreSlice -match 'ModifyResource|HandleOptionSelected|ShowDialogueNode') {
    throw 'Run history restore replays gameplay.'
}

Write-Output 'PASS: Run history contract checks.'
