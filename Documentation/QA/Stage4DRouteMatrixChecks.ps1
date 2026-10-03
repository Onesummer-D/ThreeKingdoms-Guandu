$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$assetPath = Join-Path $projectRoot 'Assets\Scripts\Data\GuanduDialogueData.asset'
if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) {
    throw 'GuanduDialogueData.asset was not found.'
}

# Parse the small Unity YAML asset line-by-line. This avoids a large multiline
# regex, which is noticeably slow in Windows PowerShell 5.
$lines = @(Get-Content -LiteralPath $assetPath -Encoding UTF8)
$nodeBlocks = @{}
$nodeIds = @{}
$currentId = $null
$currentLines = New-Object System.Collections.Generic.List[string]

foreach ($line in $lines) {
    $nodeMatch = [regex]::Match($line, '^\s*-\s*nodeId:\s*(\d+)\s*$')
    if ($nodeMatch.Success) {
        if ($null -ne $currentId) {
            $nodeBlocks[$currentId] = @($currentLines)
        }
        $currentId = $nodeMatch.Groups[1].Value
        $nodeIds[$currentId] = $true
        $currentLines = New-Object System.Collections.Generic.List[string]
        $currentLines.Add($line)
    } elseif ($null -ne $currentId) {
        $currentLines.Add($line)
    }
}
if ($null -ne $currentId) {
    $nodeBlocks[$currentId] = @($currentLines)
}

function Decode-AssetText([string] $value) {
    $trimmed = $value.Trim()
    if ($trimmed.StartsWith('"') -and $trimmed.EndsWith('"')) {
        try {
            return ($trimmed | ConvertFrom-Json)
        } catch {
            return $trimmed.Trim('"')
        }
    }
    return $trimmed.Trim("'")
}

function Get-Options([string] $nodeId) {
    if (-not $nodeBlocks.ContainsKey($nodeId)) {
        throw ('Missing dialogue node ' + $nodeId + '.')
    }
    $block = @($nodeBlocks[$nodeId])
    $starts = @()
    for ($i = 0; $i -lt $block.Count; $i++) {
        if ($block[$i] -match '^\s*-\s*optionText:\s*(.*)$') {
            $starts += $i
        }
    }
    $options = @()
    for ($n = 0; $n -lt $starts.Count; $n++) {
        $start = $starts[$n]
        $end = if ($n + 1 -lt $starts.Count) { $starts[$n + 1] } else { $block.Count }
        $textMatch = [regex]::Match($block[$start], '^\s*-\s*optionText:\s*(.*)$')
        $nextNodeId = $null
        for ($i = $start + 1; $i -lt $end; $i++) {
            $nextMatch = [regex]::Match($block[$i], '^\s*nextNodeId:\s*(\d+)\s*$')
            if ($nextMatch.Success) {
                $nextNodeId = $nextMatch.Groups[1].Value
                break
            }
        }
        if ($null -eq $nextNodeId) {
            throw ('Node ' + $nodeId + ' has an option without nextNodeId.')
        }
        $options += [pscustomobject]@{
            Text = Decode-AssetText $textMatch.Groups[1].Value
            NextNodeId = $nextNodeId
        }
    }
    return @($options)
}

$anchors = @(
    [pscustomobject]@{ Id = '1001'; ExpectedOptions = 3 },
    [pscustomobject]@{ Id = '2001'; ExpectedOptions = 2 },
    [pscustomobject]@{ Id = '3001'; ExpectedOptions = 3 },
    [pscustomobject]@{ Id = '4001'; ExpectedOptions = 2 },
    [pscustomobject]@{ Id = '5001'; ExpectedOptions = 3 }
)

$matrix = New-Object System.Collections.Generic.List[string]
$matrix.Add('# Stage 4D ending route matrix')
$matrix.Add('')
$matrix.Add('Generated from `GuanduDialogueData.asset`; use the option text and next node as the replay starting point.')
$matrix.Add('')
$matrix.Add('| Anchor | Option count | Replay options |')
$matrix.Add('| --- | ---: | --- |')

foreach ($anchor in $anchors) {
    $options = Get-Options $anchor.Id
    if ($options.Count -ne $anchor.ExpectedOptions) {
        throw ('Node ' + $anchor.Id + ' expected ' + $anchor.ExpectedOptions + ' options, found ' + $options.Count + '.')
    }
    $parts = @()
    foreach ($option in $options) {
        if (-not $nodeIds.ContainsKey($option.NextNodeId)) {
            throw ('Node ' + $anchor.Id + ' points to missing node ' + $option.NextNodeId + '.')
        }
        $parts += ('`' + $option.NextNodeId + '` ' + $option.Text)
    }
    $matrix.Add(('| `' + $anchor.Id + '` | ' + $options.Count + ' | ' + ($parts -join '<br>') + ' |'))
    Write-Output ('Anchor ' + $anchor.Id + ': ' + $options.Count + ' options -> ' + (($options | ForEach-Object { $_.NextNodeId }) -join ', '))
}

$matrix.Add('')
$matrix.Add('## Ending checkpoints')
$matrix.Add('')
$matrix.Add('| Checkpoint | Expected route or gate |')
$matrix.Add('| --- | --- |')
$matrix.Add('| IF1 | `200314` unlocks `200309` |')
$matrix.Add('| IF2 | `300416` unlocks `300410` |')
$matrix.Add('| Historical victory | `500217` unlocks `500211` |')
$matrix.Add('| IF3 | `500313` unlocks `500309` |')
$matrix.Add('| IF4 | `500320` unlocks `500317` |')
$matrix.Add('| IF5 | `500411` unlocks `500407` |')
$matrix.Add('| IF6 | `500418` unlocks `500414` |')
$matrix.Add('| Dynamic gate A | `500308`: troop > 70 and food > 50 -> `500317`; otherwise -> `500309` |')
$matrix.Add('| Dynamic gate B | `500406`: low risk + strong troop/food -> `500407`; high risk + weak troop/food -> `500414`; otherwise -> `500407` |')
$matrix.Add('| Egg | `500214` -> `500215` -> `500217`; recap badge is gated by the egg flag |')

$outputPath = Join-Path $projectRoot 'Documentation\QA\Stage4DRouteMatrix.md'
$matrix | Set-Content -LiteralPath $outputPath -Encoding UTF8
Write-Output ('PASS: Stage 4D route matrix checks (' + $anchors.Count + ' anchors; matrix written to ' + $outputPath + ').')
