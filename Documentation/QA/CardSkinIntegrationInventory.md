# 《三国·官渡之战》四套卡片美术接入清单

本次基于 main 的 Unity C# UI 创建点盘点。**一共 17 类 UI 容器**（不等于 17 张不同美术图），统一复用四种素材。以下只记录需要底板的大卡片与弹窗；滚动条、聊天气泡、选项按钮等小型控件保持现有皮肤。

| 编号 | 代码入口 | 卡片/区域 | 新样式 | 尺寸/适配 |
|---|---|---|---|---|
| 01 | GameplayExitUI | 退出确认 | SmallBanner（图1） | 小型 40%×32% 弹窗 |
| 02 | PlayerIdentityUI | 主将留名 | LightPanel（图2） | 64%×54% 弹窗 |
| 03 | HomeHubUI | 设置 | **保留原黑金框（暂不换图）** | 76%×84% 弹窗；避免复合边框遮挡滑杆 |
| 04 | HomeHubUI | 关于游戏（含人物、玩法介绍） | DarkPanel（图3） | 同主面板 |
| 05 | InviteCoCreationUI | 军议邀约主弹窗 | DarkPanel（图3） | 84%×88% |
| 06 | InviteCoCreationUI | 主将审核 / 共谋回声 | **保留实体深色子卡** | 内嵌弹窗只用一层大底图 |
| 07 | InviteCoCreationUI | 出征之前 | DarkPanel（图3） | 84%×88% |
| 08 | CampaignMapUI | 剧情回顾 | DarkPanel（图3） | 88%×89% |
| 09 | CampaignMapUI | 史官注 | DarkPanel（图3） | 76%×80% |
| 10 | CampaignMapUI | 史官卡片库 | DarkPanel（图3） | 80%×84% |
| 11 | HistorianChatUI | AI史官问答窗口 | DarkPanel（图3） | 71%×75% |
| 12 | SaveArchiveUI | 军帐存档 | DarkPanel（图3） | 76%×84% |
| 13 | BattleReplayUI | 战役绘卷 | DarkPanel（图3） | 89%×91% |
| 14 | BattleReportUI | 战绩报告主窗口 | DarkPanel（图3） | 89%×91% |
| 15 | BattleReportUI | 决策依据详情 / 资源趋势详情 | DarkPanel（图3） | 较大型二级弹窗 |
| 16 | BattleReportUI | 报告内各摘要卡片（结局/统计/倾向/趋势/文化/共谋） | **保留原统一卡片底色** | 子卡不重复铺满大幅装饰 |
| 17 | BattleReportUI | 导出的整张战绩海报 | LongPoster（图4） | 长图，比例依布局，高度可变 |

## 图片文件与用途

- `Assets/Resources/CardSkins/SmallBanner.png`，原图 1000×450：左右圆纹小横幅
- `Assets/Resources/CardSkins/LightPanel.png`，原图 1709×983：浅色水纹横向大卡
- `Assets/Resources/CardSkins/DarkPanel.png`，原图 1709×983：深墨水纹横向大卡
- `Assets/Resources/CardSkins/LongPoster.png`，原图 1440×1920：竖版战绩海报

三套横版由 `CardBackgroundSkin` 动态创建带四边固定装饰区的九宫格 Sprite；长海报用 Simple。无法读取图片时回退深色面板，保证玩家能正常游玩。

## 尚未完成、不可直接合并

**四张 PNG 和对应 .meta 已由用户在本地提交并推送到本 PR。** 仍需拉取本轮布局修复并通过 Unity Play Mode 逐页验收。

视觉复核：浅色主将留名标题已做深色文字适配；其余场景保留深色排版。因为部分布局由 Canvas 动态生成，请在 Unity Play Mode 检查文案、按钮命中、装饰边框是否被裁切，尤其战绩长图的高度适配。

建议后续逐场景微调，不要一次合并未进行视觉验收的所有 UI。

## 2026-10-18 第一轮实机反馈与修复

七张实际截图暴露了同一根因：原图有透明外边距及大幅水纹装饰，把美术直接设置成 Panel 自身的 Sprite 时，卡片的视觉边界比 RectTransform 的交互/排版区域更小，出现标题、滚动条、按钮在框外的假象。这不是 Canvas sorting order 的问题。

修复方式：

- **深色大卡** 使用统一的 `OrnateFrameArt` 子对象：横向各延展原内容框的 11%，纵向 6.5%，Sprite 九宫格画在子对象上，`SetAsFirstSibling` 保证正文始终在上层；原 Panel 保留深色衬底与点击命中区域。
- **军议邀约 / 出征之前** 的 ScrollRect 和滚动条向内收，使文字与底部按钮落在框内。
- **剧情回顾 / 史官卡片** 的滚动内容向内收，让条目与边饰形成稳定留白。
- **AI史官** 的聊天记录、快捷提问、输入框、发送按钮向内收，不重复画第二道现代直线边框。
- **主将留名** 整体上移约 7% 画布高度，避免底部云纹压住首页三个入口按钮。
- **设置界面** 保留之前已验收的黑金材质、刻度和滑块，不叠深蓝大卡。
- **战绩报告** 大边框使用新皮肤，内部重复的小卡片仍使用旧有中性底色；导出长海报维持独立长图背景。

**实机验证清单：** 依次打开主将留名、出征之前、设置、剧情回顾、史官卡片、AI史官、军议邀约、主将审核、战绩报告和长海报；确认标题/按钮/滚动条位于**可见的边框里面**，滚动能够到底，输入框能聚焦，关闭返回没有被遮住。修改需在 Unity 2022.3 Editor Play Mode 再确认，自动化脚本不等于像素级渲染验收。
