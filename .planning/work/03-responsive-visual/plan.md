# 阶段三增量计划

Status: complete
Active Task: none

## 任务

1. [x] 在第五幕代表性节点列表加入 500215，复用现有战况卡样式和节点背景图。
2. [x] 增加剧情回顾入口可见性 API，并在开始游戏/返回主菜单流程显式控制。
3. [x] 为 VisualDirector 增加清晰的资源变化事件闪变，同时提高低风险常驻氛围的可辨识度。
4. [x] 做代码级验证，更新阶段三记录。
5. [x] 新增终局专属复盘入口，复用返回主菜单按钮样式，并在终局隐藏普通剧情回顾入口。
6. [x] 复盘中补齐每幕决策选择、结局卡与七条结局的差异化总体评价。
7. [x] 为五个决策锚点加入史官注按钮与三层信息弹层。
8. [x] 对史官注、普通回顾、终局复盘和返回主菜单进行静态回归检查。
9. [x] 更新 3B/3C 阶段记录，并保留实机验收待办。
10. [x] 放大史官注弹层字号，拆分分区标题与正文并区分标题颜色。
11. [x] 修复回顾幕次同步延迟：以当前节点和本局已访问节点最高幕次共同判定，并增加打开面板后的状态兜底同步。
12. [x] 为“游戏改编说明”增加单行自适应字号，避免长句末行孤字。

## Spec Compliance

| Req ID | Requirement Summary | Status | Verification |
|---|---|---|---|
| FR-031 | 500215 进入剧情回顾并在终局保留关键片段 | ✓ met | 500215 已加入第五幕 beat 列表；终局保留最近两张已访问 beat |
| FR-032 | 主菜单隐藏入口、开始游戏恢复 | ✓ met | `StartGame` 显式恢复；`ReturnToMainMenu` 两次显式隐藏 |
| FR-033 | 资源变化产生明显反馈 | ✓ met | 用户完成实机验收，红/金闪变与增强脉冲强度可辨识且不遮挡主要 UI |
| FR-034 | 终局提供独立复盘入口并收口普通入口生命周期 | ✓ met | `ShowEndingUI` 隐藏普通入口；运行时克隆同款按钮；`ReturnToMainMenu`/`ResetRunState` 清理，`StartGame` 恢复普通入口 |
| FR-035 | 复盘展示实际选择、代表片段、结局卡且不泄露未来内容 | ✓ met | `OpenEndingReview` 启用 endingReviewMode；已完成章节调用 `CreateChoiceSummary`，结局后追加代表片段与结局卡；内容仍由 visited 集合驱动；幕次使用当前节点+已访问节点兜底并在打开时持续同步 |
| FR-036 | 七条结局拥有独立评价文案 | ✓ met | `GetEndingEvaluation` 为 7 条结局线提供独立文案，评价卡仅在 `HasReachedEnding` 后创建 |
| FR-037 | 五个已访问决策锚点提供史官注入口与三层信息 | ✓ met | `HistorianNotes` 覆盖 1001/2001/3001/4001/5001；每条含 source/record/interpretation/adaptation；分区标题与正文已拆分并设置独立字号/颜色，改编说明单行自适应字号 |
| FR-038 | 史官注弹层可打开/关闭且不改变剧情状态 | ✓ met | `OpenHistorianNote`/`CloseHistorianNote` 只切换 recapPanel 与运行时弹层，不写入剧情状态；Roslyn 编译 0 error |
| FR-039 | 3C 覆盖回顾、终局、主菜单和重新开始流程 | ✓ met | `Documentation/阶段三3C回归清单.md` 十项流程；代码检查与用户实机验收均通过 |
| NFR-031 | 不改剧情/资源数据，保持离线可复用 | ✓ met | 仅改运行时 UI/视觉脚本与回顾列表，不改节点或资源值 |
| NFR-032 | 复用既有终局按钮视觉资源，不新增场景依赖 | ✓ met | `CreateEndingReviewButton` 克隆 `endingReturnButton`，只改文字、位置和点击事件 |
| NFR-033 | 史官注不新增外部依赖且可恢复回顾状态 | ✓ met | 史官注文本内置于 `CampaignMapUI`，无 AssetDatabase/剧情数据写入；关闭弹层恢复回顾面板 |

**Compliance Status: VERIFIED**（3B/3C 代码级要求与用户实机验收均通过）

## Errors

| Attempt | Error | Resolution |
|---|---|---|
| - | - | - |
| 1 | 当前环境无 .NET SDK，`dotnet build Assembly-CSharp.csproj --no-restore` 无法运行 | 记录为环境限制；改用 Unity 生成工程/静态检查，待编辑器恢复后验证 |
| 2 | 首次 Roslyn 命令拼接 `-out:` 参数为空，导致 CS2005 | 改用 `Join-Path` 构造完整输出路径后重试，编译通过 |
| 3 | 首次合并同步逻辑的补丁上下文与当前 `CloseMap` 代码不完全匹配，`apply_patch` 未应用 | 拆分为 Update/CloseMap、Rebuild 和文本 helper 三个小补丁后成功应用 |
