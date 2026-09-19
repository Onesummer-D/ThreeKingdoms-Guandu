$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $projectDir
try {
    function Assert-Contains([string]$path, [string]$needle, [string]$label) {
        $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $path
        if ($text.IndexOf($needle, [System.StringComparison]::Ordinal) -lt 0) {
            throw "Missing advisor-loop contract: $label"
        }
    }

    $state = 'Assets/Scripts/UI/InviteSessionState.cs'
    $invite = 'Assets/Scripts/UI/InviteCoCreationUI.cs'
    $history = 'Assets/Scripts/Managers/RunHistoryData.cs'
    $tracker = 'Assets/Scripts/Managers/RunHistoryTracker.cs'
    $report = 'Assets/Scripts/UI/BattleReportUI.cs'
    $manager = 'Assets/Scripts/Managers/FinalUIManager.cs'
    $campaignMap = 'Assets/Scripts/UI/CampaignMapUI.cs'
    $dialogueAsset = 'Assets/Scripts/Data/GuanduDialogueData.asset'

    function From-CodePoints {
        param([int[]]$Codes)
        return (-join ($Codes | ForEach-Object { [char]$_ }))
    }

    $inviteTextNeedles = @(
        'OpenGuestAdvisorPanel', 'SubmitAdvisorSuggestion', 'ShowAdvisorReviewPanel', 'RecordAdvisorEcho',
        (From-CodePoints @(0x64A4, 0x9500, 0x9080, 0x7EA6)),
        (From-CodePoints @(0x672C, 0x673A, 0x63A5, 0x529B, 0x7801)),
        (From-CodePoints @(0x8BF7, 0x586B, 0x5199, 0x6635, 0x79F0, 0x5E76, 0x9009, 0x62E9, 0x5EFA, 0x8BAE)),
        (From-CodePoints @(0x7406, 0x7531, 0x53EF, 0x9009)),
        'Status == InviteSessionStatus.Accepted', 'HasUsableOptions', 'GetRectWorldHeight'
    )
    $removedInviteText = @(
        (From-CodePoints @(0x8BF7, 0x586B, 0x5199, 0x6635, 0x79F0, 0x3001, 0x9009, 0x62E9, 0x5EFA, 0x8BAE, 0xFF0C, 0x5E76, 0x5199, 0x4E0B, 0x4E00, 0x53E5, 0x7406, 0x7531, 0x3002)),
        (From-CodePoints @(0x672C, 0x673A, 0x79BB, 0x7EBF, 0x63A5, 0x529B)),
        (From-CodePoints @(0x4EC5, 0x5F53, 0x524D, 0x8FD0, 0x884C, 0x5B9E, 0x4F8B, 0x53EF, 0x7528, 0xFF1B, 0x9080, 0x8BF7, 0x7801, 0x4E0D, 0x4F1A, 0x8FDE, 0x63A5, 0x7F51, 0x7EDC, 0x3002)),
        (From-CodePoints @(0x8BF7, 0x5728, 0x804A, 0x5929, 0x4E2D, 0x56DE, 0x590D, 0x5EFA, 0x8BAE, 0x7684, 0x9009, 0x9879, 0x548C, 0x7406, 0x7531, 0x3002))
    )

    foreach ($status in @('AwaitingGuest', 'GuestSubmitted', 'Accepted', 'Declined', 'Closed')) {
        Assert-Contains $state $status "status $status"
    }
    foreach ($needle in @('BuildInviteCode', 'ValidateCode', 'SubmitSuggestion', 'AcceptSuggestion', 'DeclineSuggestion', 'Reset')) {
        Assert-Contains $state $needle $needle
    }
    foreach ($needle in $inviteTextNeedles) {
        Assert-Contains $invite $needle $needle
    }
    Assert-Contains $campaignMap 'current != null && HasUsableOptions(current)' 'invite snapshot supports every selectable node'
    Assert-Contains $campaignMap '500215' 'campaign recap still knows the egg node'
    foreach ($nodeId in @(1004, 400204, 400306, 500214)) {
        Assert-Contains $dialogueAsset ("nodeId: " + $nodeId) "selectable follow-up node $nodeId is present"
    }
    if ((Get-Content -Raw -Encoding UTF8 -LiteralPath $invite).IndexOf('node.nodeId == 1001', [System.StringComparison]::Ordinal) -ge 0) {
        throw 'Invite visibility still uses the old five-anchor whitelist.'
    }
    foreach ($removed in $removedInviteText) {
        if ((Get-Content -Raw -Encoding UTF8 -LiteralPath $invite).IndexOf($removed, [System.StringComparison]::Ordinal) -ge 0) {
            throw "Obsolete advisor UI text remains: $removed"
        }
    }
    $inviteText = Get-Content -Raw -Encoding UTF8 -LiteralPath $invite
    foreach ($needle in @('field.targetGraphic = background;', 'field.interactable = true;', 'field.readOnly = false;', 'SetSelectedGameObject(null);')) {
        Assert-Contains $invite $needle "input lifecycle $needle"
    }
    $openStart = $inviteText.IndexOf('private void OpenGuestAdvisorPanel()', [System.StringComparison]::Ordinal)
    $openEnd = $inviteText.IndexOf('private void PopulateAdvisorOptions()', $openStart, [System.StringComparison]::Ordinal)
    if ($openStart -lt 0 -or $openEnd -le $openStart) { throw 'Missing advisor panel open method boundary.' }
    $openBlock = $inviteText.Substring($openStart, $openEnd - $openStart)
    if ($openBlock.IndexOf('SetAdvisorPanels(true, false)', [System.StringComparison]::Ordinal) -gt
        $openBlock.IndexOf('ResetAdvisorInput(', [System.StringComparison]::Ordinal)) {
        throw 'Advisor panel must be activated before resetting TMP input fields.'
    }
    Assert-Contains $history 'advisorEchoes' 'history advisor echoes'
    Assert-Contains $tracker 'RecordAdvisorEcho' 'tracker advisor echo'
    if ($tracker -match 'existing\.nodeId\s*==\s*nodeId') {
        throw 'Advisor echoes are still deduplicated by node/option; repeated advisors must remain visible.'
    }
    Assert-Contains $report 'CreateAdvisorEchoCard' 'report advisor echo card'
    Assert-Contains $manager 'inviteCoCreationUI?.ResetSession()' 'run reset clears invite session'
    Write-Output 'PASS: Advisor loop contract checks.'
} finally { Pop-Location }
