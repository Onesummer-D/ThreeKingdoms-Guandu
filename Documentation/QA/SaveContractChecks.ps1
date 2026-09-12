$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent

function Require-Text {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Message
    )
    if ($Text -notmatch $Pattern) { throw $Message }
}

function Get-MethodSlice {
    param(
        [string]$Text,
        [string]$StartMarker,
        [string]$EndMarker
    )
    $start = $Text.IndexOf($StartMarker, [System.StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing method marker: $StartMarker" }
    $end = $Text.IndexOf($EndMarker, $start + $StartMarker.Length, [System.StringComparison]::Ordinal)
    if ($end -lt 0) { throw "Missing method boundary: $EndMarker" }
    return $Text.Substring($start, $end - $start)
}

$saveData = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/RunSaveData.cs')
$saveManager = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/LocalSaveManager.cs')
$dialogue = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/DialogueSystem.cs')
$resources = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/ResourceManager.cs')
$archiveUi = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/SaveArchiveUI.cs')
$exitUi = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/GameplayExitUI.cs')

Require-Text $saveData 'CurrentFormatVersion\s*=\s*1' 'Save format version is missing.'
Require-Text $saveData 'CurrentDialogueVersion' 'Dialogue data version is missing.'
foreach ($field in @('currentNodeId', 'visitedNodeOrder', 'selectedChoices', 'troop', 'food', 'strategy', 'risk', 'unlockedIfLines', 'endingId')) {
    Require-Text $saveData ("\b" + $field + "\b") "Save payload is missing $field."
}

Require-Text $saveManager 'ManualSlotCount\s*=\s*3' 'Manual save slot count changed.'
Require-Text $saveManager 'AutoSlotId\s*=\s*"auto"' 'Auto checkpoint slot is missing.'
Require-Text $saveManager 'File\.Replace\(tempPath, path, backupPath\)' 'Atomic replacement with backup is missing.'
Require-Text $saveManager '500308\s*&&\s*nodeId\s*!=\s*500406' 'Transient routing nodes are not excluded.'
foreach ($endingNode in @(200314, 300416, 500217, 500313, 500320, 500411, 500418)) {
    Require-Text $saveManager ("\{\s*" + $endingNode + "\s*,") "Ending node $endingNode is not archived."
}

$restore = Get-MethodSlice $saveManager 'public bool TryRestore' 'public bool SlotExists'
if ($restore -match 'ModifyResource|ApplyResourceEffects|ShowDialogueNode|HandleOptionSelected') {
    throw 'Save restore replays a gameplay side effect.'
}
Require-Text $restore 'RestoreSnapshot' 'Resource snapshot is not restored passively.'
Require-Text $restore 'RestoreSelectedChoices' 'Selected choices are not restored.'
Require-Text $restore 'DialogueSystem\.Instance\.RestoreRunState' 'Dialogue visit order is not restored.'

$resourceRestore = Get-MethodSlice $resources 'public void RestoreSnapshot' 'public void ResetAllResources'
if ($resourceRestore -match 'ModifyResource|OnResourceChanged|OnResourceHitZero') {
    throw 'Resource snapshot restore emits gameplay resource events.'
}

$dialogueRestore = Get-MethodSlice $dialogue 'public bool RestoreRunState' 'void ApplyResourceEffects'
if ($dialogueRestore -match 'ShowDialogueNode|ApplyResourceEffects|HandleOptionSelected') {
    throw 'Dialogue restore replays a node or an option.'
}
Require-Text $dialogueRestore 'OnDialogueNodeShown\?\.Invoke\(currentNodeId\)' 'Restored dialogue does not refresh the UI once.'

foreach ($label in @('查看存档', '军帐存档', '自动检查点', '存档 1', '存档 2', '存档 3')) {
    Require-Text $archiveUi ([regex]::Escape($label)) "Save UI label is missing: $label"
}
foreach ($label in @('保存进度', '直接退出', '确定要退出游戏吗？')) {
    Require-Text $exitUi ([regex]::Escape($label)) "Exit confirmation label is missing: $label"
}
Require-Text $exitUi 'SaveCurrentProgress' 'Exit confirmation is not wired to save-and-resume.'
Require-Text $exitUi '进度已保存' 'Save confirmation text is missing.'
Require-Text $exitUi 'ResumeAfterSave' 'Save confirmation does not resume the current run.'
Require-Text $exitUi '67\.701f' 'Exit button width does not match the inspected coordinate.'
Require-Text $exitUi '69\.2f' 'Exit button height does not match the inspected coordinate.'
if ($archiveUi -match '游戏推进后自动记录|尚未保存|选择一个已有存档继续战局|选择空槽保存') {
    throw 'Save archive still contains removed helper copy.'
}

Write-Output 'PASS: Local save contract checks.'
