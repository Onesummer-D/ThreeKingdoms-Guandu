# 任务 04：诊断导出

**Status:** partial — export and validation APIs implemented, Unity runtime export pending  
**Spec:** FR-008, NFR-004, AC-001, AC-009

## 步骤

1. 增加脱敏 JSON 导出入口和文件命名规则。
2. 增加事件完整性检查：版本、runId、sequence、时间、闭合事件和结局终点。
3. 在开发模式提供诊断摘要；正式构建隐藏调试写入但保留用户可用导出。
4. 把检查步骤记录到阶段 QA 清单。

## 验证

- 一局导出 JSON 可被独立脚本读取并通过 schema 检查。
- 人工篡改 sequence 或事件类型后，检查能报出明确错误。

## Spec Compliance

| Req ID | Status | Evidence |
|---|---|---|
| FR-008 | ~ partial | `RunHistoryDiagnostics.ExportJson/TryValidate/BuildSummary` 已实现；运行时文件导出待编辑器 |
| AC-009 | ~ partial | 诊断接口与固定入口已准备；干净构建待 Unity 环境 |

## Verification

- `RunEventProtocolChecks.ps1` → `PASS: RunEvent protocol and integration checks (11 event types).`
- 当前环境没有 Unity/.NET SDK，无法完成构建和实际 JSON 运行导出。

**Compliance Status: PARTIAL**
