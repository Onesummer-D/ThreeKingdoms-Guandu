# 4B.2 · 战绩报告界面

Status: awaiting_user_visual_verification
Active Subtask: none

目标：终局入口打开一页只读报告，展示结局、实际决策证据、资源趋势、倾向评价、文化说明和真实参谋记录。

## 步骤

1. [x] 复用剧情回顾中的结局名称、图片、摘要和七条评价。
2. [x] 用真实决策文本生成可解释的“本局决策倾向”与证据。
3. [x] 绘制兵力、粮草、计策、风险四条 0—100 趋势线。
4. [x] 展示实际决策清单、关键资源变化和有效游玩时长。
5. [x] 增加文化说明，并在 4C 前明确“本局无已确认参谋记录”。
6. [x] 终局增加同风格“战绩报告”按钮，关闭报告后恢复终局按钮。
7. [x] 定向编译和报告字段契约检查通过。

## Spec Compliance

| Req ID | Requirement Summary | Status | Verification |
|---|---|---|---|
| FR-051 | 终局报告与真实趋势 | in_progress | 定向编译与字段契约通过，待实机完整路线 |
| NFR-037 | 不伪造统计、参与者或诊断 | ✓ met | 静态检查确认无排名/百分位/二维码宣称 |

Compliance Status: PENDING_USER_VISUAL_VERIFICATION

## Errors

| Attempt | Error | Resolution |
|---|---|---|
