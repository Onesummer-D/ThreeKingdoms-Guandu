# 任务 01：事件协议

**Status:** partial — implementation complete, Unity runtime verification pending  
**Spec:** FR-001, NFR-001, NFR-004, AC-001

## 步骤

1. 列出事件类型、公共元数据、序列规则和 payload 版本。
2. 在 Managers/Data 中实现可被 Unity JsonUtility 或等价序列化读取的类型。
3. 提供事件追加、订阅、查询和清理接口，明确主线程和生命周期。
4. 为重复事件、空节点和运行重置写静态检查或最小运行测试。

## 验证

- 事件序列单调递增；每条事件含 runId、schemaVersion、elapsedMs。
- 枚举覆盖 `RunStarted`、`DecisionMade`、`MiniGameEvent`、`EndingReached` 等首期类型。
- ResetRunState 后不会把上一局事件追加到新局。

## Spec Compliance

| Req ID | Status | Evidence |
|---|---|---|
| FR-001 | ✓ met | `RunEventData.cs` 定义版本化公共元数据和 11 类事件名；`RunHistoryTracker.cs` 已挂接事件追加 |
| AC-001 | ~ partial | `RunHistoryDiagnostics.ExportJson` 已提供导出；Unity 运行时 JSON 验证待编辑器环境补做 |

## Errors

| Attempt | Check | Result | Resolution |
|---|---|---|---|
| 1 | `dotnet build .tmp_event_compile/EventCompile.csproj` | 环境没有 .NET SDK | 已改用 Node 结构检查；待 Unity 编辑器可用时补真实编译 |

**Compliance Status: PARTIAL**
