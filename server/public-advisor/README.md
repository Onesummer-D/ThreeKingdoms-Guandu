# 官渡之战公网军议服务

这是一个无第三方依赖的 Node.js 短期会话中继服务。主机创建会话，手机打开 `/join/...` 读取快照并提交一次建议，主机轮询建议后决定采纳或拒绝。服务不保存账号，不把主线决策交给手机。

## 本机启动

```powershell
cd server/public-advisor
$env:PUBLIC_ORIGIN = "http://localhost:8080"
npm start
```

健康检查：`GET /healthz`。

## 云服务器部署

1. 安装 Node.js 18+，把本目录复制到服务器。
2. 用进程管理器启动：`PUBLIC_ORIGIN=https://你的域名 npm start`。
3. 通过 Nginx/Caddy 终止 HTTPS，再反代到 `127.0.0.1:8080`。
4. 只开放 80/443；不要把 Node 端口直接暴露给公网。
5. 会话默认 15 分钟过期，`SESSION_TTL_SECONDS` 可设为 120–3600。

当前联调地址是 `http://81.70.40.146:8080`。在本机 PowerShell 中运行：

```powershell
.deploy-from-windows.ps1 -IdentityFile C:\path\to\id_ed25519
```

脚本只接受 SSH 密钥路径，不把密码写入工程；没有密钥时也可以先把本目录上传到服务器，再在上传目录执行 `bash deploy-ubuntu.sh`。脚本会识别上传目录就是安装目录的情况，不会把目录复制到自己里面。服务启动后检查 `http://81.70.40.146:8080/healthz`。

HTTP 仅用于当前联调。正式展示前建议在服务器前面加 Nginx/Caddy 和 HTTPS，并把 Unity `publicAdvisorBaseUrl` 改成 HTTPS 地址。

## API 约定

- `POST /api/sessions`：创建短期会话，返回 `hostToken`、`joinUrl`、快照和选项。
- `GET /api/sessions/:id/qr.png`：主机凭 `X-Host-Token` 获取当前会话二维码 PNG。
- `GET /api/sessions/:id`：主机凭 `X-Host-Token` 查询状态。
- `GET /api/sessions/:id/suggestions?after=N`：主机轮询新建议。
- `POST /api/sessions/:id/suggestions`：手机凭 `joinToken` 提交一次建议，支持 `clientRequestId` 幂等重试。
- `POST /api/sessions/:id/decision`：主机凭 `X-Host-Token` 写入采纳/拒绝。
- `DELETE /api/sessions/:id`：主机关闭会话。
- `POST /api/historian/ask`：受约束史料问答，返回 `citations`、`retrieved`、`answerVersion` 和审计编号。

生产部署必须使用 HTTPS，并通过反向代理设置访问日志、限流和安全响应头。当前服务使用内存存储，重启会使会话失效；这符合短期比赛邀约的降级目标，不适合长期房间。
