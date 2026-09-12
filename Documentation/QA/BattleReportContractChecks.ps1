$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$report = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/BattleReportUI.cs')
$analyzer = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/BattleReportAnalyzer.cs')
$finalUi = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/FinalUIManager.cs')
$chart = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/ResourceTrendGraphic.cs')

function Require-Text {
    param([string]$Text, [string]$Pattern, [string]$Message)
    if ($Text -notmatch $Pattern) { throw $Message }
}

foreach ($label in @('战绩报告', '恭喜通关', '本局决策倾向', '四项资源趋势', '史官简注', '分享海报', '海报已保存', '打开图片', '打开文件夹', '复制路径')) {
    Require-Text $report ([regex]::Escape($label)) "Report label is missing: $label"
}
Require-Text $report 'GetReachedEndingLabel\(\)' 'Report does not reuse ending label.'
Require-Text $report 'GetReachedEndingSprite\(\)' 'Report does not reuse ending image.'
Require-Text $report 'GetReachedEndingEvaluation\(\)' 'Report does not reuse ending evaluation.'
Require-Text $report 'GetReachedEndingSummary\(\)' 'Report does not reuse ending summary.'
Require-Text $report 'heroText, 34f,\s*TextAlignmentOptions\.Center' 'Ending summary beside the portrait is not centered.'
Require-Text $report '\\n理由：' 'Advisor reason is not placed on its own line.'
Require-Text $report 'EncodeToPNG\(\)' 'Report does not export a PNG.'
Require-Text $report 'Environment\.SpecialFolder\.MyPictures' 'PNG export does not target the local pictures directory.'
Require-Text $report 'lastPosterPath' 'PNG export does not expose the full saved path.'
Require-Text $report 'ProcessStartInfo' 'PNG export has no local open action.'
if ($report -match '官渡战绩报告|依据：') { throw 'Report still contains the removed title or evidence prefix.' }
foreach ($ending in @(200314, 300416, 500217, 500313, 500320, 500411, 500418)) {
    Require-Text $report ("case\s+" + $ending + '\s*:') "Culture explanation missing for ending $ending."
}
if ($report -match '排名|百分位|通关率|二维码') { throw 'Report contains unsupported fabricated statistics or QR claim.' }
if ($report -match '4C 接通真实建议后') { throw 'Report contains removed 4C placeholder.' }

foreach ($tendency in @('稳慎', '奇谋', '果决', '纳谏')) {
    Require-Text $analyzer ([regex]::Escape($tendency)) "Tendency explanation missing: $tendency"
}
Require-Text $analyzer 'evidence' 'Tendency evidence is not exposed.'
Require-Text $chart 'SetData' 'Resource trend chart has no data binding.'
Require-Text $chart 'value\s*/\s*100f' 'Resource trend chart is not normalized to the 0-100 scale.'
Require-Text $report 'CreateResourceBarChart' 'Report has no native resource visualization fallback.'
foreach ($valueLabel in @('兵力', '粮草', '计策', '风险')) {
    Require-Text $report ([regex]::Escape($valueLabel)) "Resource bar label is missing: $valueLabel"
}
if ($report -match 'CreateDecisionCard|本局实际决策与资源变化') { throw 'Report still contains the removed decision-number card.' }
Require-Text $finalUi 'CreateBattleReportButton' 'Ending report button is not created.'
Require-Text $finalUi 'OpenBattleReport' 'Ending report button is not connected.'
Require-Text $finalUi 'battleReportUI\.OnClosed' 'Report close lifecycle is not connected.'

Write-Output 'PASS: Battle report contract checks.'
