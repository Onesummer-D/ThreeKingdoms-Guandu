# 4C-06 用户截图反馈回归

Status: awaiting_visual_verification
Active Subtask: visual-verification

## 目标

完成 FR-081–FR-089：修复同一决策节点连续多次军议邀约的生命周期问题，扩大入口判定到所有实际可选节点，接入彩蛋勋章回顾，并把战绩报告改成真实可见的多点资源趋势、可展开详情、完整长文案、左图右文卡片布局和第三轮趋势图视觉尺度收口。

## 实施步骤

1. [x] 盘点邀约状态机、节点选项判定、回顾卡图片选择、资源日志和报告卡片创建路径，确认现有数据字段与不变量。验证：`rg`/YAML 盘点确认 9 个带选项节点、Accepted 二次邀请复用、500215 已有 `achievementSprite`、报告存在折线绘制但被终点条覆盖。
2. [x] 修复同节点多次邀约：采纳/拒绝后保留回声但结束当前 attempt，重新邀请时创建清洁输入状态并保证提交命中当前 attempt。验证：`AdvisorStateChecks` 覆盖采纳后第二次提交、拒绝后再次开始；`RunHistoryTracker` 保留重复节点建议。
3. [x] 将军议入口改为当前节点存在至少一个非空实际选项判定，排除背景介绍、转场、彩蛋纯叙事和终局非决策节点；补充静态节点覆盖检查。验证：对白 YAML 盘点 9 个选项节点，`AdvisorLoopContractChecks` 覆盖漏掉的 1004/400204/400306/500214。
4. [x] 找到并接入“政治家的胸怀”本地勋章资源，令彩蛋回顾卡左侧使用勋章、普通卡继续使用剧情图，保持右侧文字完整。验证：`500215.achievementSprite` 已存在，`CampaignMapUI.CreateBeatCard` 优先使用并命名 `AchievementBadge`。
5. [x] 让战绩报告从真实资源时间线绘制多点趋势；保留当前数值可读性，但不再以终点柱状条冒充趋势。验证：`BattleReportUI` 绑定 `BuildTrendTimeline` 到 `TrendLines`，报告契约禁止 `CreateResourceBarChart`。
6. [x] 统一报告卡片左图预留位与右侧多行文本布局，移除所有会产生省略号的结局摘要/正文设置，按文本高度调整卡片和滚动区。验证：各卡创建 `CardBadgeSlot`，`FinalUIManager` 透传五个可选 Sprite 到运行时报告组件，文化卡高度 250 且 Overflow/自动缩放，回声正文不再 Compact 截断。
7. [x] 运行 `Documentation/QA/VerifyPhase4.ps1` 及专项契约/逻辑检查。验证：全套脚本通过；当前环境未执行 Unity Play Mode 视觉截图，因此保留视觉验收状态。
8. [x] 将趋势展示拆为 Compact/Detail 两种布局模式：两者共用 `BuildTrendTimeline` 真实数据，只由展示层决定节点标签密度、图表边距和信息区域，不复制数据处理逻辑。验证：`ResourceTrendViewMode` 与两处 `SetData` 调用通过定向编译/契约检查。
9. [x] 完善 Detail 模式：标题/图例独立分区，绘制 0/25/50/75/100 纵轴刻度和网格，按真实 `ResourceSnapshotData.label` 显示横轴节点，补充风险序列、重合节点 marker 与动态终点差值。验证：`BattleReportContractChecks.ps1` 检查刻度、标签采样、marker、虚线和差值逻辑全部通过。
10. [x] 完善 Compact 模式：标题、简化图例、终点差值、有限横轴标签、折线和“查看详情”按容器尺寸重新布局，确保 1920×1080 与 1600×900 不发生重叠或裁切。验证：标题启用自动缩放，图表使用 `RectTransform.rect` 动态边距，`VerifyPhase4.ps1` 全套静态检查通过。
11. [ ] 运行 `VerifyPhase4.ps1`、趋势专项静态检查和必要的状态检查；保留 Unity Game 视图作为最终视觉验收，确认起点/终点、节点标签、刻度、风险序列与详情关闭生命周期。
12. [x] 精修 Detail 信息层级与终点摘要：扩大图表区域，风险序列只保留在图表中，数据来源曾降为底部弱提示，终点状态采用两行两列；后续视觉收口已删除独立来源和解释文字。验证：`BattleReportContractChecks.ps1` 与 `VerifyPhase4.ps1` 通过，报告源码仅保留图表风险序列并生成合并资源摘要。
13. [x] 统一 X 轴短标签并增强重合线区分：已知决策关键词归纳为短事件名，未知长标题回退“决策N”且不生成省略号；风险线保留真实坐标，使用虚线与空心方块 marker。验证：报告契约检查短标签适配器、空心方块 marker、风险虚线和“决策N”回退均通过。
14. [x] 精修 Compact 横向空间与信息顺序：左侧图片位约 14%–16%，右侧按标题→合并资源摘要→趋势图→查看详情布局；不改变真实时间线。验证：`CardBadgeRight=.16f`、`CardTextLeft=.18f`、Compact 绘图区 `.18–.975` 已落盘，`VerifyPhase4.ps1` 通过。
15. [ ] 重新运行全部定向检查并完成 Unity Game 视图视觉验收，确认报告可打开、Detail/Compact 不重叠、长标签不截断、详情可关闭且报告滚动位置不被破坏。
16. [x] 合并 Detail/Compact 的图例与终点值：每项使用资源 marker + 当前值 + 相对起点变化量；删除 Compact 独立图例、终点摘要标题和风险提示，删除 Detail 独立终点状态、来源说明和常驻风险解释。验证：`CreateResourceSummary`/`UpdateResourceSummary` 共用四项摘要，源码不再包含独立图例、终点读数或风险提示。
17. [x] 按视觉尺度重排并放大字号：保留项目原字体，增大标题、资源摘要、坐标刻度、X 轴标签和详情按钮；释放的区域优先给折线图，不用缩小文字解决布局。验证：Detail 标题 46、摘要 29、坐标 27/26；Compact 标题 40、摘要 27、坐标 25/24，趋势轴标签关闭自动缩小。
18. [x] 检查趋势图文字和缩放链路：所有趋势文字为 `TextMeshProUGUI`，趋势组件及父级不使用 `localScale < 1` 压缩，Canvas Scaler 只负责全局适配。验证：源码/场景检索与定向编译。
19. [ ] 重新运行全部检查并完成 Unity Game 视图可读性验收：普通窗口和 1.5x Game 视图下，Compact/Detail 均能直接读出四项资源、坐标和短节点名。
20. [x] 完成最后一轮可读性精修：删除 Detail 风险解释；Detail/Compact 横轴分别调整为 26/24、纵轴分别调整为 27/25，坐标字提高亮度与 Medium 字重；Detail 绘图区上扩，Compact 卡片增高至 560、绘图区调整为 `.12–.70`，详情按钮上移并调整为 28 号。验证：`VerifyPhase4.ps1` 与 `git diff --check` 通过，未修改数据逻辑、配色、marker、资源摘要结构或新增功能。
21. [x] 只放大坐标文字，不再调整布局：Detail 横/纵轴调整为 32/31，Compact 横/纵轴调整为 30/29，坐标字重提升为 SemiBold、颜色提升为灰白高亮；不改图表结构、位置、卡片高度、线条、marker、资源摘要或数据逻辑。验证：`VerifyPhase4.ps1` 专项契约与定向编译通过；Unity Game View 仍需人工确认录屏可读性。

## 验收映射

| Req ID | Requirement Summary | Status | Verification |
|---|---|---|---|
| FR-081 | 同节点多次邀约且状态/输入隔离 | ✓ met | `AdvisorStateChecks` 覆盖采纳后二次提交、拒绝后再次开始；运行时 UI 仍待再次截图 |
| FR-082 | 所有实际可选节点显示入口 | ✓ met | YAML 9 个选项节点盘点、入口通用判定、1/2/3 项定位契约均通过 |
| FR-083 | 彩蛋回顾使用政治家的胸怀勋章 | ~ partial | `500215.achievementSprite` 与 `AchievementBadge` 接入已通过静态契约；待 Game 视图确认视觉效果 |
| FR-084 | 真实多点资源趋势 | ~ partial | `resourceTimeline` → `TrendLines` 折线和合并资源摘要已通过报告契约；待 Game 视图确认不被遮挡 |
| FR-085 | 长文案完整换行显示 | ~ partial | 史官卡增高、自动换行、Overflow 和报告契约已通过；待 Game 视图确认实际字体/滚动 |
| FR-086 | 所有报告卡左图右文并支持后续填图 | ~ partial | `FinalUIManager` 可配置五个 Sprite、每卡 `CardBadgeSlot` 已接入；待 Game 视图确认占位尺寸 |
| FR-087 | 趋势卡有 Compact/Detail 双模式和决策复盘信息 | ~ partial | Compact/Detail、节点标签、刻度、风险序列、marker、终点差值和响应式布局已通过定向编译/契约检查；待 Unity Game 视图确认实际字体、折线和标签不裁切 |
| FR-088 | 第二轮趋势图层级、空间与短标签精修 | ~ partial | 已纳入步骤 12–15，待生产改动及 Unity Game 视图确认 |
| FR-089 | 第三轮视觉尺度与重复信息收口 | ~ partial | 步骤 16–18、20–21 与本轮字号/空间调整已完成并通过静态复验；Unity Game 视图仍待确认 |

**Compliance Status: PARTIAL — awaiting Unity Game view visual verification**

## Errors

| Attempt | Error | Resolution |
|---|---|---|
| 1 | `VerifyPhase4.ps1` 的 `HomeHubContractChecks.ps1` 仍要求旧的 `option3Center.y - optionSpacing` 硬编码表达式；新入口已改为收集激活选项以覆盖 1/2/3 项节点 | 更新 QA 契约为检查通用 `lowestCenter`、`GetRectWorldHeight` 和 `optionSpacing` 逻辑后重跑 |
| 2 | 完整 `dotnet build Assembly-CSharp.csproj --no-restore` 无法执行，环境没有 .NET SDK | 使用项目已有 `VerifyPhase4.ps1` 的 Bundled Roslyn 编译链完成目标脚本编译；不下载或引入新依赖 |
| 3 | 一次只读读取命令使用了拼写错误的工作目录，进程启动前失败 | 立即改用正确目录复查；未产生工作区修改 |
| 4 | 首次更新 `BattleReportContractChecks.ps1` 的补丁使用了与实际正则转义不同的上下文，未产生文件改动 | 重新读取 QA 脚本后按实际行内容应用补丁 |
| 5 | `VerifyPhase4.ps1` 的定向编译清单未包含新增 `ResourceTrendPlotUI.cs`，导致 `BattleReportUI.cs` 找不到类型 | 需要将稳定趋势渲染器合并到已有被编译的 UI 源文件，或同步更新定向编译清单后重跑 |
| 6 | 记录发现时误把只读 skill 路径作为空更新补丁目标，补丁在应用前被拒绝，未产生文件改动 | 删除无效目标后重新提交仅针对 findings 的有效补丁 |
| 7 | 更新 findings 时再次误带入空的 skill 文件更新块，补丁在应用前被拒绝，未产生文件改动 | 改为只提交 findings 文件的有效补丁 |
| 8 | 首次把 FR-087 扩展补丁定位到错误的 `Version` 上下文，未产生 spec/plan 文件改动 | 分拆 spec、父计划和子计划补丁，并按当前文件实际上下文重新应用 |
| 9 | `apply_patch` 不允许同一补丁对 `ResourceTrendPlotUI.cs` 同时执行 Delete/Add 操作，未产生文件改动 | 改用先删除再单独添加的两个可逆补丁步骤 |
| 10 | `apply_patch` 不允许同一补丁对 `BattleReportUI.cs` 包含两个 Update 操作，未产生文件改动 | 合并为同一个 `BattleReportUI.cs` 更新块后重新应用 |
| 11 | 用户反馈最新版本点击“战绩报告”后只剩被拉亮的剧情背景，报告面板没有出现；运行时异常位置尚未捕获 | 已从 Unity Editor.log 定位为 `BattleReportUI.CreateLegend()` 设置 TMP `outlineWidth` 时的 `NullReferenceException`；移除报告图例和趋势节点 marker 的运行时描边赋值，避免字体材质尚未初始化时中断 `Rebuild()` |
| 12 | 记录运行时修复时一次补丁误包含了空的占位文件更新块，补丁校验拒绝，未产生工作区改动 | 删除占位更新块后重新提交仅针对计划与 findings 的有效补丁 |
| 13 | 第二轮趋势精修后的首次 `VerifyPhase4.ps1` 重跑被新增的 PowerShell 正则误报：`+` 未按字面量匹配“决策N”回退代码 | 将契约改为 `[+]` 字符类匹配源码字面加号；生产代码未变 |
| 14 | 最后一次验证命令中的 `rg` 复合正则转义不完整，导致搜索命令自身报正则解析错误；此前 `VerifyPhase4.ps1` 已全部通过 | 将后续源码确认改为多个固定字符串搜索；不影响生产代码或 QA 结果 |
| 15 | 最后一轮视觉补丁误带空的占位文件更新块，`apply_patch` 拒绝整份补丁，生产代码未改变 | 删除占位块后拆成仅针对真实生产文件的补丁重新应用 |
| 16 | 最后只读核对命令把 PowerShell 字符串中的引号转义写在双引号命令内，触发解析错误；生产代码未改变 | 改用不含嵌套引号的固定字符串检索，并单独重跑 `git diff --check` |
| 17 | 检索其他任务时线程列表页大小传入 100，工具限制最大 50，调用被拒绝 | 改用 `limit=50` 重新检索；未产生外部改动 |
| 18 | 在恢复阶段计划时把 `.planning` 读取命令放在仓库根目录，实际计划位于 `Unity/官渡之战/.planning`，路径读取失败 | 改用 Unity 项目目录作为计划根；未产生工作区修改 |
| 19 | 本轮再次从仓库根目录读取 `.planning`，得到同一“路径不存在”错误 | 停止重复读取根目录，后续所有计划操作固定使用 `Unity/官渡之战`；未产生工作区修改 |
