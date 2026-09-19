$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $projectDir
try {
    $campaign = Get-Content -Raw -Encoding UTF8 -LiteralPath 'Assets/Scripts/UI/CampaignMapUI.cs'
    $report = Get-Content -Raw -Encoding UTF8 -LiteralPath 'Assets/Scripts/UI/BattleReportUI.cs'
    $finalUi = Get-Content -Raw -Encoding UTF8 -LiteralPath 'Assets/Scripts/Managers/FinalUIManager.cs'
    $save = Get-Content -Raw -Encoding UTF8 -LiteralPath 'Assets/Scripts/Managers/LocalSaveManager.cs'
    $history = Get-Content -Raw -Encoding UTF8 -LiteralPath 'Assets/Scripts/Managers/RunHistoryTracker.cs'
    $dialogue = Get-Content -Raw -Encoding UTF8 -LiteralPath 'Assets/Scripts/Data/GuanduDialogueData.asset'

    function Require-Text {
        param([string]$Text, [string]$Needle, [string]$Message)
        if ($Text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) { throw $Message }
    }

    function Require-NotText {
        param([string]$Text, [string]$Needle, [string]$Message)
        if ($Text.IndexOf($Needle, [System.StringComparison]::Ordinal) -ge 0) { throw $Message }
    }

    function Get-NodeBlock {
        param([string]$Text, [int]$NodeId)
        $marker = "  - nodeId: $NodeId`r`n"
        $start = $Text.IndexOf($marker, [System.StringComparison]::Ordinal)
        if ($start -lt 0) {
            $marker = "  - nodeId: $NodeId`n"
            $start = $Text.IndexOf($marker, [System.StringComparison]::Ordinal)
        }
        if ($start -lt 0) { throw "Dialogue asset is missing node $NodeId." }
        $next = $Text.IndexOf("  - nodeId:", $start + $marker.Length, [System.StringComparison]::Ordinal)
        if ($next -lt 0) { return $Text.Substring($start) }
        return $Text.Substring($start, $next - $start)
    }

    function Get-MethodBlock {
        param([string]$Text, [string]$StartMarker, [string]$EndMarker)
        $start = $Text.IndexOf($StartMarker, [System.StringComparison]::Ordinal)
        if ($start -lt 0) { throw "Missing method block: $StartMarker" }
        $end = $Text.IndexOf($EndMarker, $start + $StartMarker.Length, [System.StringComparison]::Ordinal)
        if ($end -lt 0) { return $Text.Substring($start) }
        return $Text.Substring($start, $end - $start)
    }

    function Get-NextNodeId {
        param([string]$Block, [int]$NodeId)
        $match = [regex]::Match($Block, 'nextNodeId:\s*(\d+)')
        if (-not $match.Success) { throw "Node $NodeId has no nextNodeId field." }
        return [int]$match.Groups[1].Value
    }

    $anchors = @(1001, 2001, 3001, 4001, 5001)
    $endings = @(200314, 300416, 500217, 500313, 500320, 500411, 500418)
    $unlockNodes = @(200309, 300410, 500211, 500309, 500317, 500407, 500414)
    $sourceLabel = -join @([char]0x53C2, [char]0x8003, [char]0x6765, [char]0x6E90, [char]0xFF1A)
    $gameLabel = -join @([char]0x6E38, [char]0x620F)

    $historianBlock = Get-MethodBlock $campaign 'private static readonly Dictionary<int, HistorianNote>' 'private readonly List<GameObject>'
    foreach ($anchor in $anchors) {
        $anchorBlock = Get-NodeBlock $dialogue $anchor
        Require-Text $anchorBlock 'options:' "Anchor $anchor has no options field."
        Require-NotText $anchorBlock 'options: []' "Anchor $anchor is not selectable in the dialogue asset."
        Require-Text $anchorBlock '- optionText:' "Anchor $anchor has no playable option."

        $noteMarker = "                $anchor,"
        $noteStart = $historianBlock.IndexOf($noteMarker, [System.StringComparison]::Ordinal)
        if ($noteStart -lt 0) { throw "Historian note is missing for anchor $anchor." }
        $noteEnd = $historianBlock.IndexOf("`n            }", $noteStart, [System.StringComparison]::Ordinal)
        if ($noteEnd -lt 0) { throw "Historian note block is incomplete for anchor $anchor." }
        $note = $historianBlock.Substring($noteStart, $noteEnd - $noteStart)
        foreach ($field in @($sourceLabel, 'new HistorianNote(', $gameLabel)) {
            Require-Text $note $field "Historian note for anchor $anchor is missing $field."
        }
        $noteStringCount = [regex]::Matches($note, '(?m)^\s*"').Count
        if ($noteStringCount -lt 4) { throw "Historian note for anchor $anchor does not contain four text fields." }
    }

    $evaluationBlock = Get-MethodBlock $campaign 'private static string GetEndingEvaluation(int nodeId)' 'private static string GetChapterTitle'
    $labelBlock = Get-MethodBlock $campaign 'private static string GetEndingLabel(int nodeId)' 'private static string GetEndingFallbackSummary'
    $cultureBlock = Get-MethodBlock $report 'private static string GetCultureNote(int endingNodeId)' 'private static string FormatDuration'
    $historicalLabel = -join @([char]0x53F2, [char]0x5B9E, [char]0x7ED3, [char]0x5C40)
    $cultureBaselineLabel = -join @([char]0x53F2, [char]0x5B9E, [char]0x80CC, [char]0x666F)
    $fictionalLabel = -join @([char]0x67B6, [char]0x7A7A)
    $labels = New-Object System.Collections.Generic.List[string]
    foreach ($ending in $endings) {
        $endingBlock = Get-NodeBlock $dialogue $ending
        Require-Text $endingBlock 'nextNodeId: 0' "Ending $ending is not a terminal dialogue node."
        Require-Text $campaign "case ${ending}:" "Campaign recap is missing ending $ending."
        Require-Text $finalUi "$ending" "Final UI is missing ending $ending."
        Require-Text $save "{ $ending," "Local save is missing ending archive $ending."
        Require-Text $history "$ending" "Run history is missing terminal node $ending."
        Require-Text $evaluationBlock "case ${ending}: return" "Ending evaluation is missing for $ending."
        Require-Text $cultureBlock "case ${ending}: return" "Culture note is missing for ending $ending."
        $labelPattern = 'case ' + $ending + ': return "([^"]+)"'
        $labelMatch = [regex]::Match($labelBlock, $labelPattern)
        if (-not $labelMatch.Success) { throw "Ending label is missing for $ending." }
        $labels.Add($labelMatch.Groups[1].Value)
    }
    if (($labels | Select-Object -Unique).Count -ne $endings.Count) {
        throw 'Ending labels are not unique across the seven terminal routes.'
    }

    for ($i = 0; $i -lt $endings.Count; $i++) {
        $ending = $endings[$i]
        $unlock = $unlockNodes[$i]
        $cursor = $unlock
        $chain = New-Object System.Collections.Generic.HashSet[int]
        $reached = $false
        for ($hop = 0; $hop -lt 32; $hop++) {
            if ($cursor -eq $ending) { $reached = $true; break }
            if (-not $chain.Add($cursor)) { break }
            $next = Get-NextNodeId (Get-NodeBlock $dialogue $cursor) $cursor
            if ($next -eq 0) { break }
            $cursor = $next
        }
        if (-not $reached) { throw "Unlock node $unlock does not resolve to ending $ending within 32 narrative hops." }
        Require-Text $campaign "case ${unlock}:" "Campaign recap is missing unlock node $unlock."
    }

    Require-Text $cultureBlock ('case 500217: return "' + $historicalLabel) 'Historical ending marker is missing.'
    foreach ($fictionalEnding in @(200314, 300416, 500313, 500320, 500411, 500418)) {
        $fictionalLineStart = $cultureBlock.IndexOf("case ${fictionalEnding}: return", [System.StringComparison]::Ordinal)
        if ($fictionalLineStart -lt 0) { throw "Missing culture line for fictional ending $fictionalEnding." }
        $fictionalLineEnd = $cultureBlock.IndexOf("`n", $fictionalLineStart, [System.StringComparison]::Ordinal)
        if ($fictionalLineEnd -lt 0) { $fictionalLineEnd = $cultureBlock.Length }
        $fictionalLine = $cultureBlock.Substring($fictionalLineStart, $fictionalLineEnd - $fictionalLineStart)
        Require-Text $fictionalLine $cultureBaselineLabel "Ending $fictionalEnding must identify the historical baseline."
        Require-Text $fictionalLine $fictionalLabel "Ending $fictionalEnding must identify the fictional branch."
        Require-NotText $fictionalLine $historicalLabel "Ending $fictionalEnding is incorrectly labelled as a historical ending."
    }

    $route500308 = Get-MethodBlock $finalUi 'void HandleNode500308()' 'void HandleNode500406()'
    Require-Text $route500308 'troop > 70' '500308 troop threshold is missing.'
    Require-Text $route500308 'food > 50' '500308 food threshold is missing.'
    Require-Text $route500308 '500317 : 500309' '500308 does not expose both resource-gated routes.'

    $route500406 = Get-MethodBlock $finalUi 'void HandleNode500406()' 'void ShowUnlockEndingUI(DialogueNode node)'
    Require-Text $route500406 'risk < 70 && troop > 70 && food > 50' '500406 low-risk route gate is incomplete.'
    Require-Text $route500406 'risk > 70 && troop < 70 && food < 50' '500406 high-risk route gate is incomplete.'
    Require-Text $route500406 'else' '500406 fallback route is missing.'
    Require-Text $route500406 'targetNodeId = 500407' '500406 does not expose the IF5 route.'
    Require-Text $route500406 'targetNodeId = 500414' '500406 does not expose the IF6 route.'

    $eggTrigger = Get-NodeBlock $dialogue 500214
    $eggNode = Get-NodeBlock $dialogue 500215
    Require-Text $eggTrigger 'nextNodeId: 500215' 'Egg trigger node 500214 does not lead to 500215.'
    Require-Text $eggTrigger '- optionText:' 'Egg trigger node 500214 is not a selectable decision.'
    Require-Text $eggNode 'achievementSprite: {fileID: 21300000' 'Egg node 500215 has no achievement badge sprite.'
    Require-Text $eggNode 'nextNodeId: 500217' 'Egg node 500215 does not return to the historical ending route.'
    Require-Text $campaign '500215' 'Campaign recap does not include the egg node.'
    Require-Text $campaign 'AchievementBadge' 'Campaign recap has no dedicated egg badge slot.'
    Require-Text $campaign 'beat.nodeId == 500215' 'Campaign recap does not gate the badge to the triggered egg.'

    $ellipsis = [string][char]0x2026
    Require-NotText $cultureBlock $ellipsis 'Known culture notes must not use ellipsis truncation.'
    Require-NotText $cultureBlock '...' 'Known culture notes must not use ASCII ellipsis truncation.'

    Write-Output 'PASS: Stage 4D coverage checks (5 anchors, 7 endings, egg, culture boundaries).'
} finally {
    Pop-Location
}
