$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$required = @('Assets', 'Packages', 'ProjectSettings', 'Assets/Scripts')
foreach ($path in $required) {
    if (-not (Test-Path (Join-Path $projectRoot $path))) { throw "Unity project path missing: $path" }
}
$unity = Get-Command Unity.exe -ErrorAction SilentlyContinue
$hub = Get-Command UnityHub.exe -ErrorAction SilentlyContinue
if (-not $unity -and -not $hub) {
    Write-Output 'PARTIAL: Unity editor executable is not available on this machine.'
    Write-Output 'NEXT: run this check on the Unity 2022.3.62f3c1 build machine.'
    exit 2
}
Write-Output "PASS: Unity project structure found; editor command available."
