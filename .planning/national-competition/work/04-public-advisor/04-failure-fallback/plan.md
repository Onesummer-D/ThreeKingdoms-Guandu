# 任务桩：公网失败回退

**Status:** partial — local fallback and transport error path implemented; failure matrix pending  
**Spec:** FR-006, NFR-002, AC-008

验证断网、超时、服务重启和 token 过期时回退至本地邀约并保持主线可玩；已实现公网地址为空、请求错误时保留本机流程，Game View 失败矩阵待补。
