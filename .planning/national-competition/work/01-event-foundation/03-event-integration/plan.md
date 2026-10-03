# 任务 03：事件接入

**Status:** partial — hooks implemented, Unity runtime path pending  
**Spec:** FR-001, FR-002, FR-008, NFR-001, AC-001

## 步骤

1. 接入节点显示、选项选择和资源变化。
2. 接入小游戏开始、关键操作、完成/失败和结果资源变化。
3. 接入参谋建议、接受/拒绝、场景状态和结局。
4. 对重复回调和异常退出做幂等处理。

## 验证

- 固定演示路径的每个关键动作在事件流中只出现一次。
- 四类小游戏至少各有一个开始/完成事件。
- 结局事件包含结局 ID、关键原因和历史边界标记。

## Spec Compliance

| Req ID | Status | Evidence |
|---|---|---|
| FR-001 | ~ partial | 节点、选项、资源和小游戏事件已接入；运行时顺序检查待编辑器 |
| FR-002 | ~ partial | 小游戏轨迹和结局分析已写入 RunRecord；真实演示路径待编辑器 |

## Verification

- `RunEventProtocolChecks.ps1` → `PASS: RunEvent protocol and integration checks (11 event types).`
- 代码结构检查通过；真实 Unity 运行回归受环境缺少 Unity 编辑器限制。

**Compliance Status: PARTIAL**
