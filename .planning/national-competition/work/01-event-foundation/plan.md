# 阶段 01：统一事件协议与 RunRecord

**Status:** active — implementation complete, runtime verification pending  
**Active Task:** `03-event-integration`  
**Spec:** FR-001, FR-002, FR-007, FR-008, NFR-001, NFR-002, NFR-004, AC-001, AC-002

## 目标

建立版本化事件流和完整 RunRecord，接入现有节点、决策、资源、小游戏、参谋和结局入口；完成旧记录读取与 JSON 诊断导出。该阶段完成前不开始视觉、绘卷、公网和 AI 的生产实现。

## 任务

1. `01-event-contract`：定义事件类型、公共元数据和 payload 约束。
2. `02-runrecord-migration`：扩展 RunHistoryData，加入 schemaVersion、runId、seed、参与者、小游戏、结局和史实字段，补旧数据迁移。
3. `03-event-integration`：接入 DialogueSystem、ResourceManager、小游戏、InviteSessionState 和 EndingManager，保证一次动作只记录一次。
4. `04-diagnostics-export`：提供事件 JSON 导出、顺序校验和最小诊断面板/脚本。

## 阶段验收

- 新开一局导出包含 `RunStarted`、节点、决策、资源、小游戏和结局事件。
- 旧版存档可以加载，现有战绩报告不报错。
- 相同 seed 与相同决策的事件序列可比较，sequence 单调递增。
- 诊断检查能发现缺失 runId、乱序事件和未闭合小游戏事件。

## Spec Compliance

| Req ID | Requirement Summary | Status | Verification |
|---|---|---|---|
| FR-001 | 版本化统一事件协议 | ~ partial | 11 类事件和 Tracker hooks 已实现；真实 Unity JSON 待补 |
| FR-002 | 完整 RunRecord 与旧记录迁移 | ~ partial | 新字段、迁移和小游戏/结局记录已实现；旧/新存档运行回归待补 |
| NFR-001 | seed 与事件序列可复现 | ~ partial | seed 已记录；当前剧情无随机源，双跑比较待运行验证 |
| NFR-004 | 可测试、可诊断 | ~ partial | 两个 QA 脚本通过；Unity 编辑器验证待补 |

## 阶段验证

- `RunEventProtocolChecks.ps1`：通过。
- `RunHistoryMigrationChecks.ps1`：通过。
- Node 结构检查：通过，相关 C# 文件括号平衡。
- Unity/.NET 编译：环境缺失，未完成。

**Compliance Status: PARTIAL**
