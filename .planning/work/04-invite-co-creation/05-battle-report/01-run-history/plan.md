# 4B.1 · 真实本局日志

Status: complete
Active Subtask: none

## 步骤

1. [x] 定义可 JSON 序列化的资源快照、决策记录与本局日志数据。
2. [x] 在选项真正执行时只记录一次选项文字、节点、幕次和前后资源。
3. [x] 记录有效游玩时长与已显示节点，不把主菜单停留计入。
4. [x] 新局清空日志；读档恢复日志且不触发剧情副作用。
5. [x] 将日志写入 `RunSaveData`，手动档、自动档和结局档共用。
6. [x] 自动检查日志字段、存档接线和重复记录保护。

## Spec Compliance

| Req ID | Requirement Summary | Status | Verification |
|---|---|---|---|
| FR-051 | 决策与资源日志真实可追溯 | ✓ met | `RunHistoryContractChecks.ps1` 通过 |
| FR-049 | 日志进入安全存档 | ✓ met | 存档捕获/恢复接线检查通过 |
| NFR-035 | 恢复不重放选择副作用 | ✓ met | 恢复方法副作用静态检查通过 |

Compliance Status: VERIFIED

## Errors

| Attempt | Error | Resolution |
|---|---|---|
