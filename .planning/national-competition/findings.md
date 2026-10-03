# 当前工程发现

## 2026-09-29/30 基线核对

- Unity 版本为 2022.3.62f3c1，已启用 UGUI、Timeline、音频和网络请求模块；项目没有现成 Cinemachine 依赖。
- `VisualDirector` 当前处理资源阈值、色调层、暗角、资源文字脉冲和事件闪变，尚未统一驱动声音、镜头和场景元素。
- `RunHistoryData` 当前保存活动时长、完成状态、节点顺序、决策、资源时间线和参谋回声；没有版本号、seed、参与者、小游戏轨迹、事件流和史实偏离字段。
- `RunHistoryTracker` 已订阅节点、选项和资源事件，可作为事件协议的第一接入点；需要避免重复订阅和重复记录。
- `FinalUIManager.StartReplay()` 当前调用 `BeginNewRun()`，属于重新开局，不是已有记录的事件回放。
- `BattleReportUI` 和 `BattleReportAnalyzer` 已覆盖结局评价、资源趋势、决策列表、参谋回声和 PNG 战绩海报；这些功能应继续保留并改为读取扩展后的记录。
- `InviteSessionState` / `InviteCoCreationUI` 是本机离线接力流程；README 明确把远程手机和跨设备服务排除在当前构建外。
- `DialogueSystem`、`ResourceManager` 和现有小游戏是最适合接入 `DecisionMade`、`ResourceChanged`、`MiniGameEvent` 的入口。
- 现有阶段 4D 静态覆盖检查已通过，但 Unity Game View 全流程回归、干净提交包编译证据和新功能运行证据仍需补做。

## 服务器与 AI 的待确认前提

- 云服务器可用于公网会话，但开始后端任务前必须取得域名/公网 IP、SSH 或部署方式、TLS 证书、开放端口和可用数据库/缓存选项。
- 公网首版优先使用短期 HTTPS 会话 + 轮询；WebSocket 只在轮询闭环稳定后增加。
- AI 首版采用史料知识包 + 检索/提示约束 + 来源标注。LoRA/指令微调只有在训练样本、GPU、模型许可和离线评测集准备好后才进入增强任务。
- 云端 AI 不得成为核心剧情的单点依赖，必须保留本地固定回答或检索回退。

## 计划决策

- “本局回顾”和“战役绘卷”分工：前者负责分析，后者负责播放事件；不重复制作同一页报告。
- 小游戏以可复核的操作摘要记录，不承诺未采集的逐帧录像。
- 公网多人邀约仍由主玩家最终确认，远程参谋不能直接推进主线。
