# 任务 02：RunRecord 与旧数据迁移

**Status:** partial — implementation and fixture checks complete, Unity load regression pending  
**Spec:** FR-002, NFR-002, NFR-003, AC-002

## 步骤

1. 扩展 `RunHistoryData` 与 Clone/Restore 路径，加入版本、seed、参与者、事件流、小游戏轨迹、结局原因和史实边界字段。
2. 为旧 JSON 建立显式迁移函数，缺字段写入 unknown/default 并保留已有列表。
3. 将报告分析器和海报所需字段映射到新模型，保证现有 UI 不依赖新字段才能运行。
4. 使用代表性的旧记录、新记录和空记录做加载/导出回归。

## 验证

- 旧记录加载后可以打开战绩报告。
- 新记录导出再导入后字段和事件数量一致。
- 没有个人信息或服务 token 被写入导出 JSON。

## Spec Compliance

| Req ID | Status | Evidence |
|---|---|---|
| FR-002 | ~ partial | `NormalizeHistory`、`EnsureCollections`、`RebuildLegacyEvents` 已接入；旧 fixture 静态检查通过 |
| AC-002 | ~ partial | `RunHistoryMigrationChecks.ps1` 通过；Unity 真实旧存档加载与 BattleReport UI 回归待编辑器环境 |

## Verification

- `RunHistoryMigrationChecks.ps1` → `PASS: legacy RunHistory fixture and migration hooks are present.`
- `.NET SDK` 不存在，Unity 编辑器未安装，无法在本机完成 `JsonUtility.FromJson` 和 UI 回归。

**Compliance Status: PARTIAL**
