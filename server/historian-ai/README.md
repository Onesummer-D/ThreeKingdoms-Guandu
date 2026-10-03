# 受约束史料智能体

`corpus.json` 是项目内可审阅的最小知识包，`grounding.js` 负责关键词检索、证据提示词、引用校验和无模型回退。公网服务如果设置 `OPENAI_API_KEY`，会把检索到的卡片交给云端模型；没有 Key、模型失败或模型漏引证据时，自动返回本地摘要。

## 运行方式

```powershell
$env:OPENAI_API_KEY = "..."
$env:OPENAI_MODEL = "gpt-5.4-mini"
node server.js
```

服务端接口：`POST /api/historian/ask`，请求体为 `{ "question": "...", "context": "..." }`。返回 `answer`、`mode`、`retrieved` 和可追溯的 `citations`。

## 微调边界

当前先用检索增强和引用校验交付。只有在收集到足量、已审阅的问答对，有可用 GPU、模型许可证和独立评测集时，才考虑 LoRA/指令微调。训练集不能把游戏 IF 分支标成史实，也不能把模型自由生成当作史料。没有这些条件时，保持基础模型 + 本地知识包更稳。模型名通过 `OPENAI_MODEL` 配置，默认值是 `gpt-5.4-mini`，部署前应按账号可用模型和官方文档确认。

`eval.jsonl` 是首批回归集，至少覆盖乌巢、许攸、荀攸、史实结局、游戏资源边界和证据不足拒答。
