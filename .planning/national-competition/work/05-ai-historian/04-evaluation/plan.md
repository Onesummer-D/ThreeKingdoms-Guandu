# 任务桩：AI 评测

**Status:** partial — fixed evaluation and network fallback checks implemented; target set expansion pending  
**Spec:** FR-007, NFR-004, AC-007, AC-008

建立至少 10 个固定问题、来源命中、拒答、超时和无网回退测试。当前 10 个固定问题全部通过，模型地址不可达时的回退案例也已通过，后续可扩充真实 HTTP 超时和更大问题集。
