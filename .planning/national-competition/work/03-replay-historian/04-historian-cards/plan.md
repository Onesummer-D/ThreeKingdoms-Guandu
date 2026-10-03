# 任务桩：史官与人物卡

**Status:** partial — 13-card gallery implemented; Unity runtime/content review pending  
**Spec:** FR-005, AC-005

补齐 10–15 张卡片及来源、边界和演绎说明；已实现 13 张卡片、解锁门槛和详情浏览，仍需在 Unity Game View 核对字体、滚动和布局，并由项目组复核史料表述。

## 已实现

- `CampaignMapUI` 新增史官卡片库入口和滚动浏览层。
- 13 张卡片覆盖战场、人物、战术、后方、结果和演绎边界。
- 每张卡记录来源、史书记载、理解、史实边界和游戏改编说明。
- 卡片按章节锚点解锁；从详情返回时保留卡片库上下文。
- `HistorianCardsContractChecks.ps1` 通过。

## 待验证

- Unity Game View：卡片滚动、点击、返回、长文本和中文字体。
- 内容复核：以项目组最终采用的史料版本逐条确认来源措辞。
