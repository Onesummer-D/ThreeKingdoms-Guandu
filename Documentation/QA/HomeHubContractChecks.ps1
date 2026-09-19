$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$homeUi = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/HomeHubUI.cs')
$finalUi = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/FinalUIManager.cs')
$startFix = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/StartButtonLabelColorFix.cs')
$inviteUi = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/InviteCoCreationUI.cs')
$exitUi = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/GameplayExitUI.cs')
$scene = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scenes/Guanduuuu.unity')
$audio = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/AudioManager.cs')

function Require-Text {
    param([string]$Text, [string]$Pattern, [string]$Message)
    if ($Text -notmatch $Pattern) { throw $Message }
}

function From-CodePoints {
    param([int[]]$Codes)
    return (-join ($Codes | ForEach-Object { [char]$_ }))
}

$homeLabels = @(
    (From-CodePoints @(0x5173, 0x4E8E, 0x6E38, 0x620F)),
    (From-CodePoints @(0x8BA4, 0x8BC6, 0x4EBA, 0x7269)),
    (From-CodePoints @(0x73A9, 0x6CD5, 0x4ECB, 0x7ECD)),
    (From-CodePoints @(0x80CC, 0x666F, 0x97F3, 0x4E50)),
    (From-CodePoints @(0x97F3, 0x6548, 0x97F3, 0x91CF)),
    (From-CodePoints @(0x753B, 0x9762, 0x4EAE, 0x5EA6)),
    (From-CodePoints @(0x753B, 0x9762, 0x6A21, 0x5F0F, 0xFF1A)),
    'GlobalMuteButton'
)
$aboutContents = @(
    (From-CodePoints @(0x66F9, 0x64CD)),
    (From-CodePoints @(0x8BB8, 0x6538)),
    (From-CodePoints @(0x8881, 0x7ECD)),
    ('7 ' + (From-CodePoints @(0x6761, 0x7ED3, 0x5C40, 0x7EBF))),
    ('10' + [char]0x2014 + '15 ' + (From-CodePoints @(0x5206, 0x949F))),
    (From-CodePoints @(0x519B, 0x8BAE, 0x9080, 0x7EA6)),
    (From-CodePoints @(0x5F53, 0x524D, 0x7248, 0x672C, 0x5F00, 0x653E, 0x66F9, 0x64CD, 0x8DEF, 0x7EBF))
)
$removedStatus = @(
    ('Unity ' + (From-CodePoints @(0x7F16, 0x8F91, 0x5668, 0x5185, 0x4E0D, 0x5207, 0x6362, 0x7A97, 0x53E3))),
    ('60 ' + (From-CodePoints @(0x5E27)) + ' ·'),
    ('120 ' + (From-CodePoints @(0x5E27)) + ' ·')
)
$fullscreenText = From-CodePoints @(0x5168, 0x5C4F, 0x663E, 0x793A)

foreach ($label in $homeLabels) {
    Require-Text $homeUi ([regex]::Escape($label)) "Home/settings label is missing: $label"
}

foreach ($content in $aboutContents) {
    Require-Text $homeUi ([regex]::Escape($content)) "About-game content is missing: $content"
}

Require-Text $homeUi 'SetBGMVolume\(value\)' 'BGM slider is not connected.'
Require-Text $homeUi 'SetEffectsVolume\(value\)' 'Effects slider is not connected.'
Require-Text $homeUi 'Screen\.fullScreen\s*=\s*true' 'Fullscreen default is not enforced.'
Require-Text $homeUi 'Screen\.fullScreenMode\s*=\s*FullScreenMode\.FullScreenWindow' 'Fullscreen window mode is not enforced.'
Require-Text $homeUi 'ApplyQualityPreset' 'Quality control is not connected.'
Require-Text $homeUi 'DisplayQualityPreset' 'Quality preset is not persistent.'
Require-Text $homeUi 'RuntimeIconGraphic\.IconKind\.Gear' 'Gear icon is missing.'
Require-Text $homeUi 'titleText\.fontStyle\s*=\s*FontStyles\.Bold' 'About card titles are not bold.'
Require-Text $homeUi 'TextAlignmentOptions\.Center' 'About card alignment is not centered.'
Require-Text $homeUi 'AudioListener\.volume' 'Global mute is not connected.'
Require-Text $homeUi 'DisplayBrightness' 'Brightness preference is not persistent.'
Require-Text $startFix 'spriteSwap\.normalColor\s*=\s*Color\.white' 'Start button white-label guard is missing.'
Require-Text $startFix '\[DefaultExecutionOrder\(10000\)\]' 'Start button white-label guard does not run after legacy UI state scripts.'
$startObject = [regex]::Match($scene, '(?s)--- !u!1 &1883389921\r?\n.*?--- !u!1 &1884196181')
if (-not $startObject.Success) { throw 'StartButton scene block is missing.' }
Require-Text $startObject.Value 'm_Name: StartButton' 'StartButton scene object is missing.'
$startText = [regex]::Match($scene, '(?s)--- !u!114 &1655160250\r?\n.*?(?=--- !u!)')
if (-not $startText.Success) { throw 'StartButton label component is missing.' }
Require-Text $startText.Value 'm_fontColor: \{r: 1, g: 1, b: 1, a: 1\}' 'StartButton serialized label is not white.'
$startSwap = [regex]::Match($scene, '(?s)--- !u!114 &1883389926\r?\n.*?(?=--- !u!)')
if ($startSwap.Success) {
    Require-Text $startSwap.Value 'normalColor: \{r: 1, g: 1, b: 1, a: 1\}' 'StartButton serialized normal label state is not white.'
    Require-Text $startSwap.Value 'm_Enabled: 0' 'StartButton legacy color override is still enabled.'
} elseif ($startObject.Value -match '1883389926') {
    throw 'StartButton still references the legacy color override component.'
}
Require-Text $inviteUi 'lowestCenter\.y\s*-\s*optionSpacing' 'Invite button is not aligned to the lowest visible option.'
Require-Text $inviteUi 'GetRectWorldHeight' 'Invite button does not handle one-option nodes.'
Require-Text $inviteUi 'HasUsableOptions\(node\)' 'Invite button is not driven by actual selectable nodes.'
Require-Text $inviteUi 'rect\.anchorMin\s*=\s*new Vector2\(\.5f, \.44f\)' 'Invite button fallback anchor is not below the decision options.'
Require-Text $inviteUi 'private void LateUpdate\(\)' 'Invite button does not retry alignment after layout.'
Require-Text $inviteUi 'rect\.sizeDelta\s*=\s*new Vector2\(300f, 80f\)' 'Invite button is not using the compact four-character width.'
Require-Text $inviteUi 'SyncDecisionInviteLabel\(parent\)' 'Invite label does not inherit the option label sizing.'
Require-Text $finalUi 'public void StartReplay\(\)' 'Ending replay does not have a dedicated direct-start entry point.'
Require-Text $finalUi 'public void StartReplay\(\)\s*\{\s*BeginNewRun\(\);' 'Ending replay does not start a fresh run directly.'
Require-Text $exitUi 'manager\.StartReplay\(\)' 'Ending primary action is not wired to the direct replay entry point.'
Require-Text $finalUi 'clickAreaButton\.onClick\.RemoveListener\(OnClickAreaClicked\)' 'Replay does not re-arm the dialogue click listener.'
Require-Text $finalUi 'backgroundIntroClickArea\.onClick\.RemoveListener\(OnBackgroundIntroClicked\)' 'Replay does not re-arm the background-intro click listener.'
Require-Text $finalUi 'clickAreaButton\.transform\.SetAsLastSibling\(\)' 'Replay click-through layer is not restored above dialogue graphics.'
Require-Text $audio 'EffectsVolume' 'Effects volume is not persistent.'
Require-Text $audio 'public float GetBGMVolume\(\)' 'BGM setting cannot be read by UI.'
if ($removedStatus | Where-Object { $homeUi -match [regex]::Escape($_) }) {
    throw 'Settings still contains removed helper status lines.'
}
if ($homeUi -match ('CreateToggle|' + [regex]::Escape($fullscreenText))) {
    throw 'The removed fullscreen checkbox is still visible in settings.'
}
if ($audio -match 'currentBGMVolume\s*<=\s*0\.01f') {
    throw 'Saved mute is still overwritten during startup.'
}

Write-Output 'PASS: Home/about/settings contract checks.'
