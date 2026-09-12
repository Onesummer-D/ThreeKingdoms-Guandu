# Task 06：4B 体验整合返工

Status: awaiting_visual_verification
Active Subtask: 11-seventh-round-replay-lifecycle

## 目标

依据 2026-09-08—09 的 Unity 实机截图，一次性收口首页、设置、存档、关于、游戏内退出、军议邀约、终局入口、战绩报告和海报导出，保留离线单人完整路线。

## 子任务

1. [x] `01-home-settings`：移除旧音量条，首页三按钮等尺寸横排；齿轮与静音图标；放大设置字号，缩小全屏方框并说明画质/全屏实际状态。（代码/契约通过，待 Unity 视觉验收）
2. [x] `02-about-save-exit`：修复非开始入口误弹出征说明；精简存档文案；重做关于卡片、人物三栏和玩法大字；新增全程右下角退出确认并移除旧保存按钮。（代码/契约通过，待 Unity 视觉验收）
3. [x] `03-invite-ending`：军议入口仅决策点显示、紧邻问题并闪烁；精简邀请说明；终局只保留两个并排入口。（代码/契约通过，待 Unity 视觉验收）
4. [x] `04-report-share`：重做报告和海报，使用曹操人物图、可见资源图、底部声明；导出后展示完整路径和打开/定位操作。（代码/契约通过，待 Unity 视觉验收）
5. [x] `05-verification`：定向编译、契约检查、资源日志/存档回归已通过；Unity 实机验收因当前环境无可操作 Unity 句柄保留待用户确认。
6. [x] `06-visual-correction`：依据第二轮实机截图修正省略号、卡片、设置控件、图标、剧情回顾、终局残留和战绩报告图表；定向编译与契约检查已重新通过，待 Unity 视觉复核。
7. [x] `07-third-round-visual-correction`：依据第三轮实机截图统一首页文字/颜色、修正退出与军议位置、精简画质档、增加原生资源条可视化、移除决策编号卡并居中史实简注；定向编译与契约检查已通过，待 Unity 视觉复核。
8. [x] `08-fourth-round-screenshot-feedback`：依据第四轮截图修正开始游戏底色、保存确认按钮、军议恢复/尺寸/文字、回顾背景轮换、报告摘要与资源百分比，并让所有结局节点保留退出入口且支持“再玩一局”；`VerifyPhase4.ps1` 已通过，仍需 Unity 视觉复核。
9. [x] `09-fifth-round-replay-lifecycle`：依据第五轮截图将开始游戏文字改为白色、军议按钮压缩到 400×80/40 号，并恢复结局直接重开时被终局隐藏的剧情与输入子面板；`VerifyPhase4.ps1` 已通过，仍需 Unity 路线复核。
10. [x] `10-sixth-round-state-spacing`：修复旧 `ButtonSpriteSwap` 覆盖开始游戏白字的问题，并将军议邀约与第三个选项的垂直节距收口到一致；已增加按钮本体白字兜底和运行时实际 RectTransform 对齐，`VerifyPhase4.ps1` 通过，仍待 Unity 视觉复核。
11. [x] `11-seventh-round-replay-lifecycle`：终局“再玩一局”改走专用直达重开入口；重开时恢复对白/资源/侧栏、两类点击层和监听，首个角色对白可继续推进；`VerifyPhase4.ps1` 通过，仍待 Unity 路线实机复核。

## Findings

- 首次规划补丁在审批等待超时后实际已部分落盘；复查确认 spec、roadmap 和父计划已更新，只有本任务文件未创建。后续以文件实际状态为准，不重复写入。
- 首页附加按钮必须改为无持久事件的新建按钮，避免复制“开始游戏”的场景绑定。
- 报告导出后至少提供完整路径、打开图片和定位文件；经典 Unity 桌面程序不能可靠地把附件直接注入所有 Windows 分享目标，因此不伪造“已分享到社交媒体”。
- 场景中确认存在独立的 `BGMVolumeSlider`，需在运行时隐藏其完整对象；设置页继续保留背景音乐与音效两个滑杆。
- Bootstrap Icons 官方仓库提供所需 gear、volume、mute、box-arrow-right 图标并采用 MIT 许可，适合转换为本地 Unity Sprite 后随包分发。
- `CloneMenuButton` 只调用 `RemoveAllListeners()`，不会清掉场景序列化的持久 StartGame 监听；新增首页按钮必须使用全新 GameObject，而不是克隆 StartButton。
- 人物资源已经完整存在：曹操、许攸、袁绍均有立绘和圆形头像，关于页与战绩报告无需生成新人物素材。
- 旧局内“保存进度”由 `SaveArchiveUI` 动态创建，可直接移除并由新的全局退出确认接管；`ReturnToMainMenu` 目前为私有，需要增加受控公开入口。
- 终局复盘与报告按钮同样通过克隆返回按钮创建，除尺寸重叠外也存在继承持久返回事件的风险，需改为新建按钮。
- 当前趋势图容器只有约 140 像素高且正文 24—31 号，在项目 Canvas 缩放下明显过小；报告需整体扩大卡片高度、字号、图表线宽与单点标记。
- 项目未安装 Unity Vector Graphics，不能直接依赖 SVG；图标将以本地、分辨率无关的 UGUI 几何图形实现，外形采用用户熟悉的齿轮、喇叭/斜杠和退出箭头语义，避免新增包和运行时联网。
- `DialogueText` 本身使用约 0.25 的局部缩放且横向尺寸很大，军议按钮若挂在根 Canvas 上很难稳定跟随；当前改挂 `GameInterfacePanel`，用全屏坐标把 750×80 按钮放在三项选择下方、对白纸框之外，避免遮挡正文。
- 原趋势图单点数据全部落在左边缘且只有 4 像素标记，极易被误判为空白；单点应横向铺成短线或居中显示，并提高线宽和节点大小。
- 自动存档允许绝大多数对白节点，仅排除两个即时条件分流节点；退出确认可在安全节点覆盖自动检查点并返回首页，处于不可保存瞬间时保留确认框和明确提示，不强行写坏档。
- 全局静音可通过 `AudioListener.volume` 独立实现，不破坏设置页保存的 BGM/音效比例；解除静音后原比例自动恢复。
- `HandleBattleReportClosed` 与复盘关闭逻辑目前以旧返回按钮是否显示判断终局，移除返回按钮后必须改为直接检测当前终局节点。
- 第四轮截图显示结局页的退出入口被隐藏，不能把“终局仅两个内容按钮”误解为隐藏全局退出；退出应在结局页保留，进入复盘/战绩弹层时再暂时隐藏。
- 军议邀约按钮文字可能被后创建的图形兄弟覆盖，需在创建后将 TMP 文本置于最上层并显式关闭省略模式。
- 报告资源条的标签和值与自定义趋势图同级时存在被遮挡风险，改为每行独立父对象，标签和值作为最后兄弟节点。
- 结局页通过“再玩一局”绕过主菜单返回时，不能只重置数据；终局隐藏的 DialoguePanel、Sidebar、ResourcePanel 必须在 BeginNewRun 前显式恢复，否则五个背景介绍后会留下不可点击的空画面。

## 验收要点

- 设置、关于、存档的打开和返回均不触发“出征之前”；只有开始游戏触发。
- 首页不再显示右侧音量条，三个主按钮同尺寸横排；齿轮和喇叭状态可辨认。
- 游戏进行和结局界面均可通过右下角退出入口返回首页；决策点外不出现军议邀约。
- 战绩报告正文和图表在 1920×1080 清晰可读；导出成功后玩家能直接打开图片或定位到文件。

## Spec Compliance

| Req | Status | Verification |
|---|---|---|
| FR-054 | partial | `VerifyPhase4.ps1` + HomeHubContractChecks；待 Unity 实机 |
| FR-055 | partial | `SaveContractChecks.ps1` + HomeHubContractChecks；待 Unity 实机 |
| FR-056 | partial | UI 编译 + 退出/邀约静态契约；待五锚点实机 |
| FR-057 | partial | BattleReportContractChecks + 终局逻辑；待七结局实机 |
| FR-058 | partial | BattleReportContractChecks + PNG 路径操作代码；待截图实机 |
| NFR-038 | met | 本地 UGUI 几何图标，无运行时网络依赖；Bootstrap Icons MIT 作为外形参考 |

## Errors

| Attempt | Error | Resolution |
|---|---|---|
| 1 | 首次整包补丁等待审批超时，返回状态未能反映已经落盘的部分 | 立即只读复查实际文件，保留已成功内容，仅补建缺失任务计划 |

## Planning Gate

- [x] 用户明确授权 17 项返工范围
- [x] spec 升级至 1.5
- [x] 本任务计划建立

Gate Status: CLEARED
