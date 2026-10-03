param(
    [string]$Server = 'ubuntu@81.70.40.146',
    [string]$IdentityFile = '',
    [int]$Port = 8080
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path (Join-Path $PSScriptRoot '.')).Path
$remote = "$Server`:guandu-public-advisor"
$sshArgs = @('-o', 'StrictHostKeyChecking=accept-new')
$scpArgs = @('-o', 'StrictHostKeyChecking=accept-new')
if ($IdentityFile) {
    $sshArgs += @('-i', $IdentityFile)
    $scpArgs += @('-i', $IdentityFile)
}

ssh @sshArgs $Server "mkdir -p ~/guandu-public-advisor"
scp @scpArgs -r (Join-Path $source '*') $remote
$remoteCommand = "cd ~/guandu-public-advisor && PORT=$Port PUBLIC_ORIGIN=http://81.70.40.146:$Port bash deploy-ubuntu.sh"
ssh @sshArgs $Server $remoteCommand
