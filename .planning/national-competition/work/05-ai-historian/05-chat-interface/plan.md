# 问史官聊天交互

**Status:** complete
**Spec:** FR-007 / AC-007 / AC-008；spec 0.2
**Authorization:** 用户确认方案，提供透明图标并要求推进；不另设审批。

## 步骤
- [x] 原样导入透明图标，独立入口和半透明聊天窗口。
- [x] 自由输入、推荐问题、滚动历史、来源展开、等待与失败回退。
- [x] 游戏暂停/恢复、输入隔离、本局记录与新局清空。
- [x] 后端历史规范化、上下文检索和连续追问；同步 Unity 内服务。
- [x] 有意义的接口回归、故障测试与 Unity 编译/运行验证。
- [x] 更新并核验云端服务；保留备份与密钥配置。

## 验收
| 要求 | 状态 | 证据 |
|---|---|---|
| 透明图标和悬浮窗 | met | 原图与导入 PNG SHA256 相同；三种尺寸真实场景 UI 渲染 |
| 自由问答、记录、推荐和来源 | met | Unity 23 项运行检查通过；Game View 实际鼠标、中文输入、Enter 与两轮云端问答成功；推荐按钮同路径绑定 |
| 连续追问与剧情摘要 | met | 后端 13/13；Unity 实际请求 JSON；线上 DeepSeek 两轮 grounded_model / grounding-v2 |
| 暂停恢复和新局隔离 | met | Unity 运行检查：原 timeScale 恢复、阻止底层输入、请求重置隔离、新局清空 |
| 云端更新和断网回退 | met | 两个文件线上 SHA256 一致、systemd active；Unity 实际断网回退；relay 11/11 |

**Compliance Status: VERIFIED**

最终验证见 `outputs/2026-10-14-historian-chat/verification.md` 和 `unity-runtime-checks.txt`。额外修复底层剧情文字透入输入区的问题，并验证关闭后原文字恢复；新增编辑器测试存档隔离。不同第三方输入法的候选窗口行为仍可依操作清单点验，不据此宣称所有输入法都已覆盖。

## 部署证据
原文件备份 `/root/guandu-public-advisor/historian-backup-20261014-173202`。未更改或记录模型密钥与登录凭据。
史官模块 SHA256 `02ca1dc5f757266995b584ab1608805cd42e4ec121532d8032d49a474b2d7304`。
服务入口 SHA256 `087a2c4b88e8c801051449c430d480fc53172d15578d9404d02951e301ea722e`。
