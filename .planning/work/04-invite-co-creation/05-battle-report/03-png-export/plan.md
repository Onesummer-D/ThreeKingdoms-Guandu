# 4B.3 · 本地 PNG 海报

Status: awaiting_user_visual_verification

目标：从战绩报告导出可打开的本地 PNG，清晰显示结局、倾向、关键选择与资源终值，不自动发布、不生成无效二维码。

## 步骤

1. [x] 报告页提供“分享海报”按钮。
2. [x] 使用当前报告首屏截图编码为 PNG，并写入用户图片目录。
3. [x] 图片目录不可写时提示失败，不伪造“已分享”；首版不自动发布、不生成二维码。
4. [x] 编译和 PNG 导出字段契约检查通过。
5. [ ] Unity 实机点击后确认 PNG 可打开且文字未被裁切。

## Spec Compliance

| Req ID | Requirement Summary | Status | Verification |
|---|---|---|---|
| FR-051 | 本地 PNG 海报 | in_progress | 静态检查通过，待实机打开文件 |
| NFR-037 | 不伪造发布/二维码 | ✓ met | 代码只写本地图片目录 |

Compliance Status: PENDING_USER_VISUAL_VERIFICATION

## Errors

| Attempt | Error | Resolution |
|---|---|---|
