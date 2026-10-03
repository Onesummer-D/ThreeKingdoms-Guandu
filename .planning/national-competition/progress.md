# 进度记录

## 2026-09-30 — 规格与路线图草案

- 已核对当前 Unity 工程、VisualDirector、RunHistory、StartReplay 和本地邀约边界。
- 已创建 `.planning/spec.md`，包含 FR-001–FR-008、NFR-001–NFR-005、AC-001–AC-009。
- 已创建 `.planning/roadmap.md`，拆分为事件底座、视觉响应、绘卷与史官、公网邀约、AI 史料、集成验收六个阶段。
- 已记录公网服务器和 AI 微调的前置条件与降级策略。
- 当前仍处于 Planning Gate，尚未修改生产代码。

## 下一步

1. 用户审阅并确认 `spec.md` 与 `roadmap.md` 的范围和计划结构。
2. 清除 Planning Gate，开始阶段 01 的事件协议和 RunRecord 任务。
3. 每个任务完成后执行计划中的验证命令并记录证据。

## 2026-09-30 — 阶段计划补齐

- 已创建阶段 01 的详细计划和四个任务计划。
- 已为阶段 02–06 创建阶段计划及未来任务桩，覆盖公网会话、手机网页、Unity 传输、AI 知识包/评测和最终构建。
- 公网方案升级为云端短期会话 + HTTPS 轮询优先 + 二维码手机网页；仍保留本地回退。
- AI 方案升级为云端可部署模型 + 本地知识包约束；LoRA/指令微调作为条件式增强，不阻塞基础交付。
- Planning Gate 仍为 NOT CLEARED，尚未修改 Unity 生产代码。

## 2026-10-01 — Planning Gate 清除

- 用户明确批准规格与路线图，`spec.md` 状态更新为 `approved`。
- `roadmap.md` Planning Gate 更新为 `CLEARED`。
- 当前活动阶段为 `01-event-foundation`，开始执行事件协议与 RunRecord。

## 2026-10-01 — 阶段 01 任务 01

- 新增 `RunEventData.cs`：版本化事件类型、参与者、小游戏轨迹和结局分析数据。
- 扩展 `RunHistoryData.cs`：schemaVersion、runId、seed、participants、events、miniGames、ending 和完整性字段。
- 重写 `RunHistoryTracker.cs` 的记录与迁移路径，接入节点、决策、资源、参谋、小游戏和结局事件。
- 新增 `RunHistoryDiagnostics.cs`，提供 JSON 导出、序列/runId/schema 校验和摘要。
- `FinalUIManager.cs` 已接入小游戏开始、操作和结束记录；`LocalSaveManager.cs` 暴露当前 runId。
- Node 结构检查和事件钩子检查通过；真实 Unity 编译暂受当前环境没有 Unity/.NET SDK 限制。
- 任务 01 已完成主要实现，下一步进入任务 02 的旧记录迁移回归。

## 2026-10-01 — 阶段 02 预接入

- `VisualDirector` 已扩展为 Calm/Alert/Crisis 三级状态。
- 状态转换现在可写入 `SceneStateChanged`，并连接已有音效、主相机轻量抖动、立绘亮度和发石车透明度。
- 新增 `VisualDirectorStateChecks.ps1`，静态检查通过。
- Unity Game View 的真实音频、镜头和缺引用降级验证待补。

## 2026-10-01 — 战役绘卷预接入

- 新增 `BattleReplayUI.cs`，从真实 `RunHistoryData.events` 生成事件时间线。
- 支持播放、暂停、上一条、下一条、首个决策、跳到结局和结局原因/史实边界展示。
- `BattleReportUI` 新增“战役绘卷”入口，`FinalUIManager` 完成组件初始化和关闭回调。
- `BattleReplayContractChecks.ps1` 通过；真实 Game View 交互验证待 Unity。

## 2026-10-01 — 史官卡片库

- `CampaignMapUI` 新增 13 张史官/人物/战术卡片，均包含来源、史实边界和游戏改编说明。
- 新增“史官卡片”入口、解锁门槛、滚动浏览、详情页和上下文返回。
- 新增 `HistorianCardsContractChecks.ps1`，静态卡片数量与入口检查通过。
- 需要 Unity Game View 核对长文本布局、中文字体和点击返回链路；内容来源还需项目组最终复核。

## 2026-10-01 — 公网军议最小闭环

- 新增 `server/public-advisor`：短期会话、快照、手机建议提交、主机轮询、采纳/拒绝、过期清理和响应式手机页。
- 新增 Unity `PublicAdvisorClient`，在 `InviteCoCreationUI` 中以可选 HTTPS 传输接入；公网地址留空自动保持本机回退。
- 本地 Node HTTP 创建会话、手机提交、主机轮询闭环通过；Node 服务和手机页脚本检查通过。
- 新增 `PublicAdvisorContractChecks.ps1` 并通过。云服务器部署、域名 HTTPS、QR 展示和两台真实设备验收仍待完成。

## 2026-10-02 — 史料助手与公网部署准备

- 史料评测扩充并修复后达到 10/10；新增游戏内“问史官”入口，调用公网 `POST /api/historian/ask`，失败时回到固定史料卡片。
- AI 默认模型改为 `gpt-5.4-mini`，提示使用 `max_completion_tokens`；模型名仍由环境变量配置，微调不作为本期硬依赖。
- 公网联调默认地址设为 `http://81.70.40.146:8080`，Unity `insecureHttpOption` 调为允许 HTTP；新增 Ubuntu systemd 部署脚本和 Windows 上传脚本。
- SSH 直连服务器被拒绝，原因是当前环境没有可用密钥；部署脚本和地址已准备，待云主机控制台或 SSH 密钥可用后执行。
- 当前阶段仍保持 partial：二维码、HTTPS、双设备实测、Unity Game View 和正式服务器部署未完成。

## 2026-10-03 — AI/公网烟测与路线图校正

- 本机 HTTP 端到端烟测通过：创建会话、手机建议、主机轮询、决策回写和史料问答均正常。
- 模型地址不可达时的网络失败回退通过，仍返回本地有引用的 `grounded_fallback`。
- 修正 Unity 二维码下载处理器，改用 `DownloadHandlerTexture` 读取 PNG，并将该契约加入检查。
- 公网与 AI 合约检查通过，史料固定评测保持 10/10，Node 语法检查通过。
- 路线图活动阶段校正为 `05-ai-historian`；阶段 01–04 标记为 partial，剩余工作集中在真实 Unity 运行时、SSH/云端部署、二维码、HTTPS 和双设备验收。
