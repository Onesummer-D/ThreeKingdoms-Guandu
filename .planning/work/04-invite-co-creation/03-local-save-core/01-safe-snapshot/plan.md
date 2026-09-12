# 安全快照与恢复

Status: in_progress
Active Subtask: none

## 实现清单

1. [x] `RunSaveData` 使用 Unity `JsonUtility` 可序列化字段，不保存 Unity 对象引用。
2. [x] `DialogueSystem.RestoreRunState` 校验节点存在后一次性恢复当前节点与访问顺序，不调用选项处理。
3. [x] `ResourceManager.RestoreSnapshot` 直接恢复四项夹紧值，不叠加旧值；剧情恢复事件统一刷新 UI。
4. [x] `CampaignMapUI` 导出/恢复锚点选择，读档后回顾可显示实际选择。
5. [x] `LocalSaveManager` 提供捕获、写盘、读取、备份、自动检查点与结局归档标题。
6. [x] `FinalUIManager` 在节点稳定后触发自动档，并提供统一读档后 UI 刷新入口。
7. [x] 定向编译、36 条章节检查与存档静态契约检查通过。

## Spec Compliance

| Req ID | Requirement Summary | Status | Verification |
|---|---|---|---|
| FR-049 | 本地存档数据与安全恢复 | in_progress | 代码和静态验收通过，待实机存读档 |
| NFR-031 | 离线运行且不改剧情数据 | verified | 定向编译通过 |
| NFR-035 | 恢复无重复副作用 | in_progress | 静态契约通过，待实机重复读档 |

Compliance Status: PENDING

## Errors

| Attempt | Error | Resolution |
|---|---|---|
| 1 | 扩充目标编译后 `JsonUtility` 报 CS0103 | 校验脚本缺少 Unity `JSONSerializeModule` 引用；补齐模块后重跑 |
