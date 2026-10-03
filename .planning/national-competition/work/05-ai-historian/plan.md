# 阶段 05：受约束 AI 史料助手

**Status:** active — corpus, grounded fallback, endpoint and in-game entry implemented; cloud model/runtime verification pending  
**Spec:** FR-007, NFR-002, NFR-003, NFR-004, AC-007, AC-008

## 任务桩

- `01-source-corpus`：整理史料知识包、来源编号、节点标签和史实边界。
- `02-grounded-inference`：实现检索/提示约束、结构化回答、拒答和本地回退。
- `03-model-deployment`：接云端模型；只有条件满足时增加 LoRA/指令微调。
- `04-evaluation`：固定问题集、来源命中率、拒答率和无网回退测试。

## 当前进展

- `server/historian-ai/corpus.json` 已整理 10 张带来源、定位、标签和史实边界的知识卡。
- `grounding.js` 已实现检索、引用校验、精确数据拒答和本地固定摘要回退。
- 公网服务新增 `POST /api/historian/ask`；Unity 新增 `HistorianAgentClient`，史官卡片页提供三个示例问题入口。
- `eval.jsonl` 10 个问题全部通过；模型地址不可达时的网络回退也已验证，仍需在实际服务器配置模型后做云端响应验证。
- 默认模型配置已改为 `gpt-5.4-mini`，部署前按 OpenAI Docs 和账号可用模型确认；微调仍是条件式增强。

## 阶段出口

至少 10 个固定问题有来源可追溯答案；知识包外问题不编造；云端 AI 不可用时仍有可用回退。
