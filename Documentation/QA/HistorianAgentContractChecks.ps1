$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$root = $projectRoot
$corpus = Join-Path $root 'server/historian-ai/corpus.json'
$grounding = Join-Path $root 'server/historian-ai/grounding.js'
$eval = Join-Path $root 'server/historian-ai/eval.jsonl'
foreach ($path in @($corpus, $grounding, $eval)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing historian AI artifact: $path" }
}
$docs = Get-Content -LiteralPath $corpus -Raw -Encoding UTF8 | ConvertFrom-Json
if ($docs.Count -lt 8) { throw "Historian corpus is too small: $($docs.Count)" }
$groundingText = Get-Content -LiteralPath $grounding -Raw -Encoding UTF8
foreach ($token in @('retrieve', 'buildGroundedPrompt', 'validateAnswer', 'fallbackAnswer', 'OPENAI_API_KEY', 'grounded_refusal')) {
    if ($groundingText -notmatch [regex]::Escape($token)) { throw "Missing AI grounding hook: $token" }
}
Write-Output "PASS: historian AI corpus and grounding contract checks ($($docs.Count) cards)."
