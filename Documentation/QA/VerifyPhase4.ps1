param([string]$UnityData = 'D:/Onesummer_D/Software/CS/Unity/2022.3.62f3c1/Editor/Data')
$ErrorActionPreference = 'Stop'
$projectDir = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $projectDir
try {
    $qaDir = Join-Path $projectDir 'Temp/Phase4Checks'
    New-Item -ItemType Directory -Force -Path $qaDir | Out-Null
    $compiler = Join-Path $UnityData 'DotNetSdkRoslyn/csc.dll'
    $runner = Join-Path $UnityData 'NetCoreRuntime/dotnet.exe'
    $refs = @("$UnityData/NetStandard/ref/2.1.0/netstandard.dll")
    $refs += @('CoreModule','UIModule','InputLegacyModule','TextRenderingModule','AudioModule','IMGUIModule','JSONSerializeModule','ImageConversionModule') | ForEach-Object { "$UnityData/Managed/UnityEngine/UnityEngine.$_.dll" }
    $refs += @('UnityEngine.UI','Unity.TextMeshPro','Assembly-CSharp') | ForEach-Object { "Library/ScriptAssemblies/$_.dll" }
    $referenceArgs = $refs | ForEach-Object { '-r:' + $_ }
    $sourceFiles = @(
        'Assets/Scripts/Data/DialogueDataSO.cs',
        'Assets/Scripts/Managers/DataLoader.cs',
        'Assets/AudioManager.cs',
        'Assets/Scripts/Managers/DialogueSystem.cs',
        'Assets/Scripts/Managers/ResourceManager.cs',
        'Assets/Scripts/Managers/GameData.cs',
        'Assets/Scripts/Managers/EndingManager.cs',
        'Assets/Scripts/Managers/IfLineManager.cs',
        'Assets/Scripts/Managers/RunHistoryData.cs',
        'Assets/Scripts/Managers/RunHistoryTracker.cs',
        'Assets/Scripts/Managers/RunSaveData.cs',
        'Assets/Scripts/Managers/LocalSaveManager.cs',
        'Assets/Scripts/UI/CampaignChapterResolver.cs',
        'Assets/Scripts/UI/BattleReportAnalyzer.cs',
        'Assets/Scripts/UI/ResourceTrendGraphic.cs',
        'Assets/Scripts/UI/ResourceTrendPlotUI.cs',
        'Assets/Scripts/UI/BattleReportUI.cs',
        'Assets/Scripts/UI/CampaignMapUI.cs',
        'Assets/Scripts/UI/InviteSessionState.cs',
        'Assets/Scripts/UI/InviteCoCreationUI.cs',
        'Assets/Scripts/UI/SaveArchiveUI.cs',
        'Assets/Scripts/UI/RuntimeIconGraphic.cs',
        'Assets/Scripts/UI/GameplayExitUI.cs',
        'Assets/Scripts/UI/HomeHubUI.cs',
        'Assets/Scripts/UI/StartButtonLabelColorFix.cs',
        'Assets/Scripts/Managers/FinalUIManager.cs',
        'Assets/Scripts/Managers/VisualDirector.cs'
    )
    & $runner $compiler -nologo -nostdlib+ -target:library -nowarn:0436 "-out:$qaDir/Phase4UI.dll" @referenceArgs @sourceFiles
    if ($LASTEXITCODE -ne 0) { throw 'Targeted UI compilation failed.' }
    Write-Output 'PASS: Targeted UI compilation. This is not a full Unity build or visual test.'
    $runtimeDir = "$UnityData/NetCoreRuntime/shared/Microsoft.NETCore.App/6.0.21"
    $runtimeRefs = @('System.Private.CoreLib','System.Console','System.Runtime') | ForEach-Object { "-r:$runtimeDir/$_.dll" }
    & $runner $compiler -nologo -nostdlib+ -target:exe "-out:$qaDir/ChapterResolverChecks.dll" @runtimeRefs 'Assets/Scripts/UI/CampaignChapterResolver.cs' 'Documentation/QA/ChapterResolverChecks.cs'
    if ($LASTEXITCODE -ne 0) { throw 'Logic check compilation failed.' }
    & $runner exec --runtimeconfig 'Documentation/QA/ChapterResolverChecks.runtimeconfig.json' "$qaDir/ChapterResolverChecks.dll"
    if ($LASTEXITCODE -ne 0) { throw 'Chapter checks failed.' }
    & $runner $compiler -nologo -nostdlib+ -target:exe "-out:$qaDir/BattleReportAnalyzerChecks.dll" @runtimeRefs 'Assets/Scripts/Managers/RunHistoryData.cs' 'Assets/Scripts/UI/BattleReportAnalyzer.cs' 'Documentation/QA/BattleReportAnalyzerChecks.cs'
    if ($LASTEXITCODE -ne 0) { throw 'Battle report analyzer compilation failed.' }
    & $runner exec --runtimeconfig 'Documentation/QA/ChapterResolverChecks.runtimeconfig.json' "$qaDir/BattleReportAnalyzerChecks.dll"
    if ($LASTEXITCODE -ne 0) { throw 'Battle report analyzer checks failed.' }
    & $runner $compiler -nologo -nostdlib+ -target:exe "-out:$qaDir/AdvisorStateChecks.dll" @runtimeRefs 'Assets/Scripts/UI/InviteSessionState.cs' 'Documentation/QA/AdvisorStateChecks.cs'
    if ($LASTEXITCODE -ne 0) { throw 'Advisor state check compilation failed.' }
    & $runner exec --runtimeconfig 'Documentation/QA/ChapterResolverChecks.runtimeconfig.json' "$qaDir/AdvisorStateChecks.dll"
    if ($LASTEXITCODE -ne 0) { throw 'Advisor state checks failed.' }
    & 'Documentation/QA/SaveContractChecks.ps1'
    if ($LASTEXITCODE -ne 0) { throw 'Save contract checks failed.' }
    & 'Documentation/QA/HomeHubContractChecks.ps1'
    if ($LASTEXITCODE -ne 0) { throw 'Home/about/settings contract checks failed.' }
    & 'Documentation/QA/RunHistoryContractChecks.ps1'
    if ($LASTEXITCODE -ne 0) { throw 'Run history contract checks failed.' }
    & 'Documentation/QA/BattleReportContractChecks.ps1'
    if ($LASTEXITCODE -ne 0) { throw 'Battle report contract checks failed.' }
    & 'Documentation/QA/AdvisorLoopContractChecks.ps1'
    if ($LASTEXITCODE -ne 0) { throw 'Advisor loop contract checks failed.' }
} finally { Pop-Location }
