$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$report = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/BattleReportUI.cs')
$analyzer = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/BattleReportAnalyzer.cs')
$finalUi = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/FinalUIManager.cs')
$chart = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/ResourceTrendGraphic.cs')
$plot = Get-Content -Raw -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/ResourceTrendPlotUI.cs')

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
Require-Text $report 'BuildTrendTimeline\(history\)' 'Report is not using the real resource timeline.'
Require-Text $report 'TrendLines' 'Report has no visible trend-line surface.'
Require-Text $report 'ResourceTrendPlotUI' 'Report has no stable UI trend renderer.'
Require-Text $report 'ResourceTrendViewMode\.Compact' 'Report does not use a compact trend layout.'
Require-Text $report 'ResourceTrendViewMode\.Detail' 'Report does not use a detail trend layout.'
Require-Text $report 'TrendDetailOverlay' 'Report has no trend detail overlay.'
Require-Text $report 'OpenTrendDetail' 'Trend detail button is not connected.'
Require-Text $report 'CloseTrendDetail' 'Trend detail overlay has no close lifecycle.'
Require-Text $report 'CreateResourceSummary' 'Report does not expose the merged resource summary.'
Require-Text $report 'UpdateResourceSummary' 'Detail resource summary is not refreshed from the selected timeline.'
Require-Text $report 'FormatResourceSummaryValue' 'Merged resource summary is not data-driven.'
Require-Text $report 'FormatDelta' 'Report does not calculate endpoint deltas.'
Require-Text $report 'value - startValue' 'Troop endpoint delta is not data-driven.'
if ($report -match '风险数值越高，局势越危险|风险 ↑ =|风险 ↑') {
    throw 'Report still contains the removed risk explanation.'
}
if ($report -match 'CreateLegend|FormatResourceReadout|终点状态|终点摘要|数据来源') {
    throw 'Report still contains a redundant endpoint readout, independent legend, or source note.'
}
Require-Text $report '四项资源趋势 · 真实节点记录", 40f' 'Compact trend title was not enlarged.'
Require-Text $report '资源趋势详情 · 决策前后节点", 46f' 'Detail trend title was not enlarged.'
Require-Text $report 'compact \? 27f : 29f' 'Merged resource summary typography was not enlarged.'
Require-Text $report 'CreateCard\("ResourceTrend", y, 560f\)' 'Compact trend card was not given more vertical room.'
Require-Text $report 'CardTextLeft, \.12f\), new Vector2\(\.975f, \.70f\)' 'Compact trend plot was not made taller.'
Require-Text $report '查看详情", 28f' 'Compact detail button was not made more readable.'
Require-Text $report 'new Vector2\(\.80f, \.045f\), new Vector2\(\.975f, \.115f\)' 'Compact detail button was not moved upward.'
Require-Text $report 'new Vector2\(\.07f, \.07f\), new Vector2\(\.94f, \.72f\)' 'Detail trend plot was not expanded upward.'
Require-Text $plot 'new\[\] \{ 100, 75, 50, 25, 0 \}' 'Detail trend plot has no 0/25/50/75/100 ticks.'
Require-Text $plot 'GetPointLabel' 'Trend plot has no real x-axis label adapter.'
Require-Text $plot 'GetLabelIndices' 'Trend plot has no x-axis label sampling.'
Require-Text $plot 'MarkerGlyphs' 'Trend plot has no distinguishable markers.'
Require-Text $plot 'CreateDashedLine' 'Risk series has no distinct line treatment.'
Require-Text $plot '"□"' 'Risk marker is not the hollow square used to distinguish overlapping values.'
Require-Text $plot 'ShortNodeLabel' 'Trend plot does not normalize long node names into short labels.'
Require-Text $plot 'return "决策" [+] index' 'Long unknown node names do not fall back to a readable decision label.'
Require-Text $plot 'detail \? 31f : 29f' 'Trend y-axis typography was not visibly enlarged.'
Require-Text $plot 'detail \? 32f : 30f' 'Trend x-axis typography was not visibly enlarged.'
Require-Text $plot 'LabelColor = new Color\(\.88f, \.90f, \.89f, 1f\)' 'Trend labels were not brightened to readable gray-white.'
Require-Text $plot 'label\.fontWeight = FontWeight\.SemiBold' 'Trend axis labels were not given a sufficiently strong weight.'
Require-Text $plot 'label\.enableAutoSizing = false' 'Trend labels can still shrink to solve layout.'
if ($report -match 'CreateResourceBarChart') { throw 'Report still overlays endpoint bars over the resource trend.' }
foreach ($valueLabel in @('兵力', '粮草', '计策', '风险')) {
    Require-Text $report ([regex]::Escape($valueLabel)) "Resource label is missing: $valueLabel"
}
if ($report -match 'CreateDecisionCard|本局实际决策与资源变化') { throw 'Report still contains the removed decision-number card.' }
Require-Text $report 'CardBadgeSlot' 'Report cards do not reserve a left-side badge image slot.'
Require-Text $report 'CardBadgeRight = \.16f' 'Report badge slot was not narrowed for trend-card space.'
Require-Text $report 'CardTextLeft = \.18f' 'Report text column was not widened after narrowing the badge slot.'
foreach ($badgeField in @('statsCardBadge', 'tendencyCardBadge', 'resourceTrendCardBadge', 'cultureCardBadge', 'advisorEchoCardBadge')) {
    Require-Text $report $badgeField "Report badge field is missing: $badgeField"
}
Require-Text $report 'body\.overflowMode\s*=\s*TextOverflowModes\.Overflow' 'Culture card may still ellipsize the ending note.'
Require-Text $finalUi 'CreateBattleReportButton' 'Ending report button is not created.'
Require-Text $finalUi 'OpenBattleReport' 'Ending report button is not connected.'
Require-Text $finalUi 'battleReportUI\.OnClosed' 'Report close lifecycle is not connected.'
foreach ($badgeField in @('reportStatsBadge', 'reportTendencyBadge', 'reportResourceTrendBadge', 'reportCultureBadge', 'reportAdvisorEchoBadge')) {
    Require-Text $finalUi $badgeField "FinalUIManager report badge field is missing: $badgeField"
}

Write-Output 'PASS: Battle report contract checks.'
