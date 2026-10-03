# 阶段 04：公网多人军议邀约

**Status:** active — cloud relay and Unity transport implemented; deployment/runtime verification pending  
**Spec:** FR-006, NFR-003, NFR-004, NFR-005, AC-006, AC-008

## 任务桩

- `01-cloud-session-api`：部署短期会话、快照、建议和状态查询 API。
- `02-mobile-web-join`：手机网页、二维码、过期和重复提交处理。
- `03-unity-transport`：Unity HTTPS 轮询、建议回传、接受/拒绝和 RunRecord 接入。
- `04-failure-fallback`：断网、超时、云端错误和本地邀约回退。

## 当前进展

- `server/public-advisor` 已提供短期会话、手机快照页、建议幂等提交、主机轮询、采纳/拒绝、二维码端点和过期清理。
- Unity 新增 `PublicAdvisorClient`，并在 `InviteCoCreationUI` 中接入可选 HTTPS 轮询；公网地址留空时继续使用本机流程。
- `PublicAdvisorContractChecks.ps1`、Node 语法检查和本机 HTTP 端到端闭环已通过；Ubuntu systemd 与 Windows 上传脚本已准备，并修正同目录部署复制问题。
- 仍需用云服务器控制台或 SSH 密钥执行部署；当前无域名，只能先用 HTTP 联调，之后再配置 HTTPS 反代并做两台真实设备验收。

## 阶段出口

两台不同设备可完成二维码加入、建议提交、云端中继、Unity 收取和主玩家确认；公网故障不影响主线。
