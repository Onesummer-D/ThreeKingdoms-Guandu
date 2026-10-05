# 阶段 06 功能冻结清单

## 已验证

- [x] 16 项自动契约检查通过
- [x] 脱敏 RunRecord JSON 可解析且敏感信息扫描通过
- [x] 本地公网 relay 集成回归 10/10
- [x] 史料固定评测 10/10
- [x] 腾讯云 `/healthz` 返回 200
- [x] DeepSeek 线上回答返回 `grounded_model` 和来源编号
- [x] systemd 服务开机启动并自动重启
- [x] Git 本地提交包含部署、回退、验收和样例证据

## 冻结前必须补证

- [ ] Unity 2022.3.62f3c1 干净导入无编译错误
- [ ] 按 `CleanBuildCommands.md` 完成无界面 Windows 构建
- [ ] Windows 构建离线启动并走通固定演示路径
- [ ] Game View 展示三种 VisualDirector 状态
- [ ] 战役绘卷播放真实事件并能定位结局原因
- [ ] 手机 A 扫码加入并提交建议
- [ ] Unity 主机收到建议并采纳/拒绝
- [ ] 断网后本地参谋流程可继续
- [ ] 导出一份真实脱敏 RunRecord 并通过 `RedactedExportChecks.ps1`

## 版本记录

- 本地最新 Git 提交：`4f3f2b8 Fix UTF-8 redacted export validation`
- 远程服务：`guandu-public-advisor.service`
- 公网地址：`http://81.70.40.146:8080`
- 线上 AI 模式：`grounded_model`；不可用时回退 `grounded_fallback`

## 禁止进入材料

- SSH 密码、API Key、hostToken、joinToken
- 完整二维码 URL
- 用户主机路径、设备信息和个人账号
