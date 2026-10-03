# 任务桩：绘卷时间线模型

**Status:** partial — event-backed model implemented, runtime replay pending  
**Spec:** FR-004, AC-004

`BattleReplayUI` 直接读取 `RunHistoryTracker.ExportHistory()` 的事件流，支持决策/结局索引和结局原因展示；运行时验证待 Unity。
