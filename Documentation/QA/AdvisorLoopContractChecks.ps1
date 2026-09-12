$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $projectDir
try {
    function Assert-Contains([string]$path, [string]$needle, [string]$label) {
        $text = Get-Content -Raw -LiteralPath $path
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

    foreach ($status in @('AwaitingGuest', 'GuestSubmitted', 'Accepted', 'Declined', 'Closed')) {
        Assert-Contains $state $status "status $status"
    }
    foreach ($needle in @('BuildInviteCode', 'ValidateCode', 'SubmitSuggestion', 'AcceptSuggestion', 'DeclineSuggestion', 'Reset')) {
        Assert-Contains $state $needle $needle
    }
    foreach ($needle in @('OpenGuestAdvisorPanel', 'SubmitAdvisorSuggestion', 'ShowAdvisorReviewPanel', 'RecordAdvisorEcho', '撤销邀约', '本机接力码', '请填写昵称并选择建议', '理由可选')) {
        Assert-Contains $invite $needle $needle
    }
    foreach ($removed in @('请填写昵称、选择建议，并写下一句理由。', '本机离线接力', '仅当前运行实例可用；邀请码不会连接网络。', '请在聊天中回复建议的选项和理由。')) {
        if ((Get-Content -Raw -LiteralPath $invite).IndexOf($removed, [System.StringComparison]::Ordinal) -ge 0) {
            throw "Obsolete advisor UI text remains: $removed"
        }
    }
    $inviteText = Get-Content -Raw -LiteralPath $invite
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
    Assert-Contains $report 'CreateAdvisorEchoCard' 'report advisor echo card'
    Assert-Contains $manager 'inviteCoCreationUI?.ResetSession()' 'run reset clears invite session'
    Write-Output 'PASS: Advisor loop contract checks.'
} finally { Pop-Location }
