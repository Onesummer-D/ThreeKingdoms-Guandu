$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$report = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/BattleReportUI.cs')
$analyzer = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/BattleReportAnalyzer.cs')
$finalUi = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/Managers/FinalUIManager.cs')
$chart = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/ResourceTrendGraphic.cs')
$plot = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectDir 'Assets/Scripts/UI/ResourceTrendPlotUI.cs')

function Require-Text {
    param([string]$Text, [string]$Pattern, [string]$Message)
    if ($Text -notmatch $Pattern) { throw $Message }
}

function From-CodePoints {
    param([int[]]$Codes)
    return (-join ($Codes | ForEach-Object { [char]$_ }))
}

$reportLabels = @(
    (From-CodePoints @(0x6218, 0x7EE9, 0x62A5, 0x544A)),
    (From-CodePoints @(0x606D, 0x559C, 0x901A, 0x5173)),
    (From-CodePoints @(0x672C, 0x5C40, 0x51B3, 0x7B56, 0x503E, 0x5411)),
    (From-CodePoints @(0x56DB, 0x9879, 0x8D44, 0x6E90, 0x8D8B, 0x52BF)),
    (From-CodePoints @(0x53F2, 0x5B98, 0x7B80, 0x6CE8)),
    (From-CodePoints @(0x5206, 0x4EAB, 0x6D77, 0x62A5)),
    (From-CodePoints @(0x6D77, 0x62A5, 0x5DF2, 0x4FDD, 0x5B58)),
    (From-CodePoints @(0x6253, 0x5F00, 0x56FE, 0x7247)),
    (From-CodePoints @(0x6253, 0x5F00, 0x6587, 0x4EF6, 0x5939)),
    (From-CodePoints @(0x590D, 0x5236, 0x8DEF, 0x5F84))
)
$removedTitleValues = @(
    (From-CodePoints @(0x5B98, 0x6E21, 0x6218, 0x7EE9, 0x62A5, 0x544A)),
    (From-CodePoints @(0x4F9D, 0x636E, 0xFF1A))
)
$removedTitlePattern = ($removedTitleValues | ForEach-Object { [regex]::Escape($_) }) -join '|'
$unsupportedStatsValues = @(
    (From-CodePoints @(0x6392, 0x540D)),
    (From-CodePoints @(0x767E, 0x5206, 0x4F4D)),
    (From-CodePoints @(0x901A, 0x5173, 0x7387)),
    (From-CodePoints @(0x4E8C, 0x7EF4, 0x7801))
)
$unsupportedStatsPattern = ($unsupportedStatsValues | ForEach-Object { [regex]::Escape($_) }) -join '|'
$placeholderPattern = '4C ' + [regex]::Escape((From-CodePoints @(0x63A5, 0x901A, 0x771F, 0x5B9E, 0x5EFA, 0x8BAE, 0x540E)))
$tendencies = @(
    (From-CodePoints @(0x7A33, 0x614E)),
    (From-CodePoints @(0x5947, 0x8C0B)),
    (From-CodePoints @(0x679C, 0x51B3)),
    (From-CodePoints @(0x7EB3, 0x8C0F))
)
$riskExplanationValues = @(
    (From-CodePoints @(0x98CE, 0x9669, 0x6570, 0x503C, 0x8D8A, 0x9AD8, 0xFF0C, 0x5C40, 0x52BF, 0x8D8A, 0x9AD8, 0x5371, 0x9669)),
    ((From-CodePoints @(0x98CE, 0x9669)) + ' ' + [char]0x2191 + ' ='),
    ((From-CodePoints @(0x98CE, 0x9669)) + ' ' + [char]0x2191)
)
$riskExplanationPattern = ($riskExplanationValues | ForEach-Object { [regex]::Escape($_) }) -join '|'
$redundantReadoutValues = @(
    (From-CodePoints @(0x7EC8, 0x70B9, 0x72B6, 0x6001)),
    (From-CodePoints @(0x7EC8, 0x70B9, 0x6458, 0x8981)),
    (From-CodePoints @(0x6570, 0x636E, 0x6765, 0x6E90))
)
$redundantReadoutPattern = ($redundantReadoutValues | ForEach-Object { [regex]::Escape($_) }) -join '|'
$trendCompactTitle = From-CodePoints @(0x56DB, 0x9879, 0x8D44, 0x6E90, 0x8D8B, 0x52BF)
$trendDetailTitle = From-CodePoints @(0x8D44, 0x6E90, 0x8D8B, 0x52BF, 0x8BE6, 0x60C5)
$realNodeRecord = From-CodePoints @(0x771F, 0x5B9E, 0x8282, 0x70B9, 0x8BB0, 0x5F55)
$decisionBeforeAfterNode = From-CodePoints @(0x51B3, 0x7B56, 0x524D, 0x540E, 0x8282, 0x70B9)
$detailButtonLabel = From-CodePoints @(0x67E5, 0x770B, 0x8BE6, 0x60C5)
$decisionWord = From-CodePoints @(0x51B3, 0x7B56)
$middleDot = [char]0x00B7
$advisorReasonLabel = From-CodePoints @(0x7406, 0x7531, 0xFF1A)
$hollowSquare = [char]0x25A1
$removedDecisionCardPattern = 'CreateDecisionCard|' + [regex]::Escape((From-CodePoints @(0x672C, 0x5C40, 0x5B9E, 0x9645, 0x51B3, 0x7B56, 0x4E0E, 0x8D44, 0x6E90, 0x53D8, 0x5316)))
$resourceLabels = @(
    (From-CodePoints @(0x5175, 0x529B)),
    (From-CodePoints @(0x7CAE, 0x8349)),
    (From-CodePoints @(0x8BA1, 0x7B56)),
    (From-CodePoints @(0x98CE, 0x9669))
)

foreach ($label in $reportLabels) {
    Require-Text $report ([regex]::Escape($label)) "Report label is missing: $label"
}
Require-Text $report 'GetReachedEndingLabel\(\)' 'Report does not reuse ending label.'
Require-Text $report 'GetReachedEndingSprite\(\)' 'Report does not reuse ending image.'
Require-Text $report 'GetReachedEndingEvaluation\(\)' 'Report does not reuse ending evaluation.'
Require-Text $report 'GetReachedEndingSummary\(\)' 'Report does not reuse ending summary.'
Require-Text $report 'heroText, 34f,\s*TextAlignmentOptions\.Center' 'Ending summary beside the portrait is not centered.'
Require-Text $report ('\\n' + [regex]::Escape($advisorReasonLabel)) 'Advisor reason is not placed on its own line.'
Require-Text $report 'EncodeToPNG\(\)' 'Report does not export a PNG.'
Require-Text $report 'Environment\.SpecialFolder\.MyPictures' 'PNG export does not target the local pictures directory.'
Require-Text $report 'lastPosterPath' 'PNG export does not expose the full saved path.'
Require-Text $report 'ProcessStartInfo' 'PNG export has no local open action.'
if ($report -match $removedTitlePattern) { throw 'Report still contains the removed title or evidence prefix.' }
foreach ($ending in @(200314, 300416, 500217, 500313, 500320, 500411, 500418)) {
    Require-Text $report ("case\s+" + $ending + '\s*:') "Culture explanation missing for ending $ending."
}
if ($report -match $unsupportedStatsPattern) { throw 'Report contains unsupported fabricated statistics or QR claim.' }
if ($report -match $placeholderPattern) { throw 'Report contains removed 4C placeholder.' }

foreach ($tendency in $tendencies) {
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
if ($report -match $riskExplanationPattern) {
    throw 'Report still contains the removed risk explanation.'
}
if ($report -match ('CreateLegend|FormatResourceReadout|' + $redundantReadoutPattern)) {
    throw 'Report still contains a redundant endpoint readout, independent legend, or source note.'
}
Require-Text $report ([regex]::Escape($trendCompactTitle) + '\s+' + [regex]::Escape($middleDot) + '\s+' + [regex]::Escape($realNodeRecord) + '", 40f') 'Compact trend title was not enlarged.'
Require-Text $report ([regex]::Escape($trendDetailTitle) + '\s+' + [regex]::Escape($middleDot) + '\s+' + [regex]::Escape($decisionBeforeAfterNode) + '", 46f') 'Detail trend title was not enlarged.'
Require-Text $report 'compact \? 27f : 29f' 'Merged resource summary typography was not enlarged.'
Require-Text $report 'CreateCard\("ResourceTrend", y, 560f\)' 'Compact trend card was not given more vertical room.'
Require-Text $report 'CardTextLeft, \.12f\), new Vector2\(\.975f, \.70f\)' 'Compact trend plot was not made taller.'
Require-Text $report ([regex]::Escape($detailButtonLabel) + '", 28f') 'Compact detail button was not made more readable.'
Require-Text $report 'new Vector2\(\.80f, \.045f\), new Vector2\(\.975f, \.115f\)' 'Compact detail button was not moved upward.'
Require-Text $report 'new Vector2\(\.07f, \.07f\), new Vector2\(\.94f, \.72f\)' 'Detail trend plot was not expanded upward.'
Require-Text $plot 'new\[\] \{ 100, 75, 50, 25, 0 \}' 'Detail trend plot has no 0/25/50/75/100 ticks.'
Require-Text $plot 'GetPointLabel' 'Trend plot has no real x-axis label adapter.'
Require-Text $plot 'GetLabelIndices' 'Trend plot has no x-axis label sampling.'
Require-Text $plot 'MarkerGlyphs' 'Trend plot has no distinguishable markers.'
Require-Text $plot 'CreateDashedLine' 'Risk series has no distinct line treatment.'
Require-Text $plot ('"' + [regex]::Escape($hollowSquare) + '"') 'Risk marker is not the hollow square used to distinguish overlapping values.'
Require-Text $plot 'ShortNodeLabel' 'Trend plot does not normalize long node names into short labels.'
Require-Text $plot ('return "' + [regex]::Escape($decisionWord) + '" [+] index') 'Long unknown node names do not fall back to a readable decision label.'
Require-Text $plot 'detail \? 31f : 29f' 'Trend y-axis typography was not visibly enlarged.'
Require-Text $plot 'detail \? 32f : 30f' 'Trend x-axis typography was not visibly enlarged.'
Require-Text $plot 'LabelColor = new Color\(\.88f, \.90f, \.89f, 1f\)' 'Trend labels were not brightened to readable gray-white.'
Require-Text $plot 'label\.fontWeight = FontWeight\.SemiBold' 'Trend axis labels were not given a sufficiently strong weight.'
Require-Text $plot 'label\.enableAutoSizing = false' 'Trend labels can still shrink to solve layout.'
if ($report -match 'CreateResourceBarChart') { throw 'Report still overlays endpoint bars over the resource trend.' }
foreach ($valueLabel in $resourceLabels) {
    Require-Text $report ([regex]::Escape($valueLabel)) "Resource label is missing: $valueLabel"
}
if ($report -match $removedDecisionCardPattern) { throw 'Report still contains the removed decision-number card.' }
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
