# 阶段 06 集成验收手册

## 自动检查

在仓库根目录执行：

```powershell
$qa = 'Unity/官渡之战/Documentation/QA'
powershell -ExecutionPolicy Bypass -File "$qa/DeploymentConsistencyChecks.ps1"
powershell -ExecutionPolicy Bypass -File "$qa/RedactedExportChecks.ps1"
Get-ChildItem "$qa/*Checks.ps1" | ForEach-Object {
  powershell -ExecutionPolicy Bypass -File $_.FullName
}
node server/public-advisor/integration-test.js
node server/historian-ai/run-eval.js
```

通过条件：所有契约检查成功；集成回归 `10/10`；史料评测 `10/10`。

## 远程服务烟测

在任意联网电脑执行：

```powershell
curl.exe -i http://81.70.40.146:8080/healthz
```

通过条件：返回 `HTTP/1.1 200 OK` 和 `"ok":true`。

再用无 BOM JSON 测试史料接口：

```powershell
$json = '{"question":"乌巢为什么重要？","context":"官渡战役"}'
[IO.File]::WriteAllText("$env:TEMP\historian-test.json", $json, [Text.UTF8Encoding]::new($false))
curl.exe -sS -X POST "http://81.70.40.146:8080/api/historian/ask" `
  -H "Content-Type: application/json" --data-binary "@$env:TEMP\historian-test.json"
```

通过条件：`ok:true`；在线模型可用时 `mode:grounded_model`；模型不可用时 `mode:grounded_fallback` 且答案含来源编号。

## 公网军议双设备验收

1. Windows Unity 主机创建会话并显示二维码。
2. 手机扫码打开 `/join/...` 页面。
3. 手机提交一次昵称、选项和理由。
4. Unity 主机轮询收到建议。
5. 主机点击采纳或拒绝。
6. 导出的 RunRecord 含 `AdvisorSuggested` 和决策结果。

记录：日期、手机型号/浏览器、会话 ID 后四位、结果和失败截图；不要记录 token、API Key 或完整二维码 URL。

## 故障回退验收

| 场景 | 操作 | 通过条件 |
|---|---|---|
| AI 不可用 | 临时将模型地址设为不可达 | 游戏显示带引用的本地回答 |
| AI 超时 | 将 `OPENAI_TIMEOUT_MS` 设为 500 | 请求在有限时间内回退 |
| 公网断开 | 禁止客户端访问 8080 | 主线和本地参谋流程可继续 |
| 会话过期 | 等待 TTL 或删除会话 | 服务返回 `session_expired`，主线不崩溃 |
| 服务重启 | `systemctl restart guandu-public-advisor` | `/healthz` 恢复 200；旧短期会话失效可接受 |

## 当前证据与未完成项

- [ ] `BuildEnvironmentChecks.ps1` 在 Unity 2022.3.62f3c1 构建机通过
- [x] 14 项契约检查
- [x] 本地 relay 集成回归 10/10
- [x] 史料固定评测 10/10
- [x] 腾讯云公网健康检查
- [x] DeepSeek 实际 `grounded_model` 响应
- [x] 脱敏 RunRecord 样例：`Documentation/QA/SampleRunRecord.redacted.json`
- [x] 更新部署包上传后的远程回归
- [ ] Unity Game View 固定演示路径
- [ ] 两台真实设备完成二维码闭环
- [ ] 干净 Unity Windows 构建

冻结前完整清单见 `Phase6FreezeChecklist.md`。
