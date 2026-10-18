# 《三国·官渡之战》四套卡片美术接入清单

本次基于 main 的 Unity C# UI 创建点盘点。**一共 17 类 UI 容器**（不等于 17 张不同美术图），统一复用四种素材。以下只记录需要底板的大卡片与弹窗；滚动条、聊天气泡、选项按钮等小型控件保持现有皮肤。

| 编号 | 代码入口 | 卡片/区域 | 新样式 | 尺寸/适配 |
|---|---|---|---|---|
| 01 | GameplayExitUI | 退出确认 | SmallBanner（图1） | 小型 40%×32% 弹窗 |
| 02 | PlayerIdentityUI | 主将留名 | LightPanel（图2） | 64%×54% 弹窗 |
| 03 | HomeHubUI | 设置 | DarkPanel（图3） | 76%×84% 弹窗 |
| 04 | HomeHubUI | 关于游戏（含人物、玩法介绍） | DarkPanel（图3） | 同主面板 |
| 05 | InviteCoCreationUI | 军议邀约主弹窗 | DarkPanel（图3） | 84%×88% |
| 06 | InviteCoCreationUI | 主将审核 / 共谋回声 | DarkPanel（图3） | 主弹窗内嵌 |
| 07 | InviteCoCreationUI | 出征之前 | DarkPanel（图3） | 84%×88% |
| 08 | CampaignMapUI | 剧情回顾 | DarkPanel（图3） | 88%×89% |
| 09 | CampaignMapUI | 史官注 | DarkPanel（图3） | 76%×80% |
| 10 | CampaignMapUI | 史官卡片库 | DarkPanel（图3） | 80%×84% |
| 11 | HistorianChatUI | AI史官问答窗口 | DarkPanel（图3） | 71%×75% |
| 12 | SaveArchiveUI | 军帐存档 | DarkPanel（图3） | 76%×84% |
| 13 | BattleReplayUI | 战役绘卷 | DarkPanel（图3） | 89%×91% |
| 14 | BattleReportUI | 战绩报告主窗口 | DarkPanel（图3） | 89%×91% |
| 15 | BattleReportUI | 决策依据详情 / 资源趋势详情 | DarkPanel（图3） | 较大型二级弹窗 |
| 16 | BattleReportUI | 报告内各摘要卡片（结局/统计/倾向/趋势/文化/共谋） | DarkPanel（图3） | 高度各异，动态九宫格 |
| 17 | BattleReportUI | 导出的整张战绩海报 | LongPoster（图4） | 长图，比例依布局，高度可变 |

## 图片文件与用途

- `Assets/Resources/CardSkins/SmallBanner.png`，原图 1000×450：左右圆纹小横幅
- `Assets/Resources/CardSkins/LightPanel.png`，原图 1709×983：浅色水纹横向大卡
- `Assets/Resources/CardSkins/DarkPanel.png`，原图 1709×983：深墨水纹横向大卡
- `Assets/Resources/CardSkins/LongPoster.png`，原图 1440×1920：竖版战绩海报

三套横版由 `CardBackgroundSkin` 动态创建带四边固定装饰区的九宫格 Sprite；长海报用 Simple。无法读取图片时回退深色面板，保证玩家能正常游玩。

## 尚未完成、不可直接合并

**GitHub C# 已更新，但上传到本次聊天的原始美术 PNG 不能经当前 GitHub 插件直接写入二进制文件。** 需要先将交付的 `Guandu_CardSkins_4styles.zip` 解压，把其中 `Assets/Resources/CardSkins/` 全部提交到此 PR 分支，CI 与实际视觉才有正确素材。

视觉复核：浅色主将留名标题已做深色文字适配；其余场景保留深色排版。因为部分布局由 Canvas 动态生成，请在 Unity Play Mode 检查文案、按钮命中、装饰边框是否被裁切，尤其战绩长图的高度适配。

建议后续逐场景微调，不要一次合并未进行视觉验收的所有 UI。
