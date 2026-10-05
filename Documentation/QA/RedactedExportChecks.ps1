$ErrorActionPreference = 'Stop'
$sample = Join-Path $PSScriptRoot 'SampleRunRecord.redacted.json'
if (-not (Test-Path -LiteralPath $sample)) { throw "Missing redacted export sample: $sample" }
$text = Get-Content -Raw $sample
try { $record = $text | ConvertFrom-Json } catch { throw "Sample is not valid JSON: $($_.Exception.Message)" }
foreach ($field in @('schemaVersion', 'runId', 'events', 'ending')) {
    if ($null -eq $record.$field) { throw "Sample is missing $field" }
}
if ($record.events.Count -lt 2) { throw 'Sample has too few events.' }
$forbidden = @(
    'sk-[A-Za-z0-9]{20,}',
    'Bearer\s+[A-Za-z0-9._-]{12,}',
    'X-Host-Token|X-Join-Token|joinToken|hostToken',
    'C:\\Users\\|D:\\|/home/|/root/',
    '81\.70\.40\.146'
)
foreach ($pattern in $forbidden) {
    if ($text -match $pattern) { throw "Redaction check failed: $pattern" }
}
Write-Output "PASS: redacted export checks ($($record.events.Count) events)."
