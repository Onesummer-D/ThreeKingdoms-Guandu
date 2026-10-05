# Unity 干净构建命令

目标版本：Unity `2022.3.62f3c1`。

## 1. 版本与结构检查

```powershell
powershell -ExecutionPolicy Bypass -File .\Documentation\QA\BuildEnvironmentChecks.ps1
```

## 2. 无界面 Windows 构建

将 `UNITY_EXE` 改为构建机上的 Unity.exe 路径：

```powershell
$UNITY_EXE = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
$PROJECT = (Get-Location).Path
$BUILD = Join-Path $PROJECT '..\..\outputs\phase6_windows_build'
$LOG = Join-Path $PROJECT '..\..\outputs\phase6_windows_build.log'
New-Item -ItemType Directory -Force (Split-Path $BUILD) | Out-Null
& $UNITY_EXE -batchmode -nographics -quit `
  -projectPath $PROJECT `
  -buildWindows64Player "$BUILD\Guandu.exe" `
  -logFile $LOG
if ($LASTEXITCODE -ne 0) { throw "Unity build failed: $LASTEXITCODE; see $LOG" }
if (-not (Test-Path "$BUILD\Guandu.exe")) { throw "Missing build executable: $BUILD\Guandu.exe" }
Get-ChildItem $BUILD -Recurse | Measure-Object -Property Length -Sum
```

## 3. 证据留存

保存以下内容到 `outputs/phase6_windows_build_evidence.txt`：

- Unity 版本输出
- `BuildEnvironmentChecks.ps1` 输出
- 构建日志末尾的成功行
- 构建目录文件总数和总字节数
- 启动后固定演示路径的截图/录屏文件名

不要把 API Key、SSH 凭据、二维码完整 URL 或用户本地绝对路径放入材料。
