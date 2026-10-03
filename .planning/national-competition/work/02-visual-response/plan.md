# 阶段 02：三级响应式影像

**Status:** active — implementation complete, runtime verification pending  
**Spec:** FR-003, NFR-002, AC-003

## 任务桩

- `01-state-model`：定义常态/警戒/危局阈值、滞回和事件覆盖。
- `02-audio-camera-bindings`：连接 AudioManager、轻量镜头/界面扰动和场景元素。
- `03-runtime-regression`：固定资源路径与断引用降级测试。

## 阶段出口

三种状态在 Windows 构建中可被实际触发，并且至少各有声音、视觉、扰动三类反馈中的有效项；状态变化写入事件流。
