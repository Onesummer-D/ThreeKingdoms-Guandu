# 任务一：邀约会话契约与状态隔离

Status: complete
Active Subtask: none

## 目标

在写 UI 代码前确定“军议邀约”最小闭环的数据结构、邀请码生成规则和生命周期，保证阶段四后续任务可以复用同一套运行时状态，并满足 FR-040、FR-041、FR-044、NFR-035、NFR-036。

## 实施步骤

1. [x] 盘点 `CampaignMapUI`、`FinalUIManager` 和 `DialogueSystem` 的只读接入点，确定快照所需字段。
2. [x] 定义 `InviteSessionState`：邀请方、幕次、当前节点、已选路线摘要、量化资源、邀请码、应邀建议、确认状态。
3. [x] 定义确定性邀请码规则和输入校验，确保同一快照可复现、错误码不触碰剧情状态。
4. [x] 定义创建、加入、确认、拒绝、取消、关闭、返回主菜单、重新开始的状态迁移表。
5. [x] 将契约记录到阶段四计划与 findings，作为任务二至四的实现边界。

## 已确认契约

### InviteSessionState

- `inviterLabel`：邀请方显示名，首版固定为“主将”。
- `chapterIndex` / `chapterTitle`：当前已访问最高幕次及标题。
- `currentNodeId`：当前节点 ID，仅用于确定性复现，不向玩家暴露未来节点。
- `selectedRouteSummary`：由 `CampaignMapUI` 提供的只读已选路线摘要。
- `troop` / `food` / `strategy` / `risk`：从 `ResourceManager` 读取并取整后的四项资源。
- `inviteCode`：基于上述字段生成的 8 位大写字母数字码。
- `guestLabel` / `guestSuggestion`：应邀者身份与一次策略建议。
- `status`：`None → AwaitingGuest → GuestSubmitted → Accepted/Declined → Closed`。

### 邀请码与隔离

- 将幕次、节点、路线摘要和资源整数组合为规范化 payload，使用无网络依赖的 FNV-1a 32 位哈希，再映射到不易混淆的 `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` 字符表。
- 同一快照始终得到同一邀请码；加入时只与当前运行时邀请会话比对，不把邀请码当作安全凭证。
- 邀约状态只存在 `InviteCoCreationUI` 的运行时对象中；关闭、取消、返回主菜单和 `ResetRunState` 都清空会话。

### 状态迁移

| 当前状态 | 操作 | 下一状态 | 主线影响 |
|---|---|---|---|
| None | 创建邀请 | AwaitingGuest | 无 |
| AwaitingGuest | 正确码加入并提交建议 | GuestSubmitted | 无 |
| AwaitingGuest | 取消/关闭/无效码 | None | 无 |
| GuestSubmitted | 主玩家确认 | Accepted | 仅生成共谋回声 |
| GuestSubmitted | 主玩家拒绝 | Declined | 仅记录拒绝结果 |
| Accepted/Declined | 关闭/重新邀请 | Closed/新的 AwaitingGuest | 不改原剧情 |
| 任意状态 | 返回主菜单/重新开始 | None | 清理运行时状态 |

## 不在本任务范围

- 不修改对白节点、资源数值、结局判定或 ScriptableObject。
- 不实现联网、账号、服务器、二维码解析或跨设备同步。
- 不在本任务直接改动生产脚本；完成契约后再进入任务二 UI 实现。

## 验收

- 快照字段足以还原当前战局但不包含未来节点内容。
- 邀请码生成不依赖随机数或网络，同一输入始终得到同一结果。
- 所有取消/拒绝/重置路径都能回到单人路线，且明确清理邀约运行时状态。
- 契约能同时支持五个决策锚点和终局复盘。

## Spec Compliance

| Req ID | Requirement Summary | Status | Verification |
|---|---|---|---|
| FR-040 | 生成战局邀请快照 | pending | 待步骤 1–2 |
| FR-041 | 短邀请码确定性生成 | pending | 待步骤 3 |
| FR-044 | 取消/关闭/重置与单人路线隔离 | pending | 待步骤 4 |
| NFR-035 | 只写入运行时会话 | pending | 静态检查 |
| NFR-036 | 五锚点/终局复用且可复现 | pending | 契约检查 |

**Compliance Status: VERIFIED**

## 当前停点

契约盘点和状态迁移表已完成。下一任务新增 `InviteCoCreationUI`，先做邀请快照、邀请码和邀请卡；第一版邀约式可演示目标是同设备接力闭环，不等待联网能力。
