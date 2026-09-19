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

$saveData = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/RunSaveData.cs')
$saveManager = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/LocalSaveManager.cs')
$dialogue = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/DialogueSystem.cs')
$resources = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/ResourceManager.cs')
$archiveUi = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/SaveArchiveUI.cs')
$exitUi = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/GameplayExitUI.cs')

function From-CodePoints {
    param([int[]]$Codes)
    return (-join ($Codes | ForEach-Object { [char]$_ }))
}

$archivePrefix = From-CodePoints @(0x5B58, 0x6863)
$archiveLabels = @(
    (From-CodePoints @(0x67E5, 0x770B, 0x5B58, 0x6863)),
    (From-CodePoints @(0x519B, 0x5E10, 0x5B58, 0x6863)),
    (From-CodePoints @(0x81EA, 0x52A8, 0x68C0, 0x67E5, 0x70B9)),
    ($archivePrefix + ' 1'),
    ($archivePrefix + ' 2'),
    ($archivePrefix + ' 3')
)
$exitLabels = @(
    (From-CodePoints @(0x4FDD, 0x5B58, 0x8FDB, 0x5EA6)),
    (From-CodePoints @(0x76F4, 0x63A5, 0x9000, 0x51FA)),
    (From-CodePoints @(0x786E, 0x5B9A, 0x8981, 0x9000, 0x51FA, 0x6E38, 0x620F, 0x5417, 0xFF1F))
)
$savedProgressText = From-CodePoints @(0x8FDB, 0x5EA6, 0x5DF2, 0x4FDD, 0x5B58)
$removedArchiveCopy = @(
    (From-CodePoints @(0x6E38, 0x620F, 0x63A8, 0x8FDB, 0x540E, 0x81EA, 0x52A8, 0x8BB0, 0x5F55)),
    (From-CodePoints @(0x5C1A, 0x672A, 0x4FDD, 0x5B58)),
    (From-CodePoints @(0x9009, 0x62E9, 0x4E00, 0x4E2A, 0x5DF2, 0x6709, 0x5B58, 0x6863, 0x7EE7, 0x7EED, 0x6218, 0x5C40)),
    (From-CodePoints @(0x9009, 0x62E9, 0x7A7A, 0x69FD, 0x4FDD, 0x5B58))
)

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

foreach ($label in $archiveLabels) {
    Require-Text $archiveUi ([regex]::Escape($label)) "Save UI label is missing: $label"
}
foreach ($label in $exitLabels) {
    Require-Text $exitUi ([regex]::Escape($label)) "Exit confirmation label is missing: $label"
}
Require-Text $exitUi 'SaveCurrentProgress' 'Exit confirmation is not wired to save-and-resume.'
Require-Text $exitUi ([regex]::Escape($savedProgressText)) 'Save confirmation text is missing.'
Require-Text $exitUi 'ResumeAfterSave' 'Save confirmation does not resume the current run.'
Require-Text $exitUi '67\.701f' 'Exit button width does not match the inspected coordinate.'
Require-Text $exitUi '69\.2f' 'Exit button height does not match the inspected coordinate.'
if ($removedArchiveCopy | Where-Object { $archiveUi -match [regex]::Escape($_) }) {
    throw 'Save archive still contains removed helper copy.'
}

Write-Output 'PASS: Local save contract checks.'
