# 阶段 03：战役绘卷与史官卡

**Status:** active — replay and historian gallery implemented; Unity runtime verification pending  
**Spec:** FR-004, FR-005, NFR-001, AC-004, AC-005

## 任务桩

- `01-replay-model`：从事件流生成时间线模型和关键节点索引。
- `02-replay-ui`：实现播放、暂停、逐步、跳转和结局原因定位。
- `03-minigame-symbolic-playback`：为小游戏轨迹提供可读的符号化回放。
- `04-historian-cards`：补齐 10–15 张史官/人物卡及来源元数据。

## 当前进展

- 战役绘卷已从真实事件流读取并支持播放、逐步、定位和结局原因展示。
- 史官卡片已扩展为 13 张可解锁卡片库，并从剧情回顾入口进入。
- 运行时验证仍受当前环境没有 Unity 编辑器限制，任务状态保持 partial。

## 阶段出口

从一局真实记录进入战役绘卷，能在同一局数据上完成播放和定位；所有史官卡有来源与史实边界。
