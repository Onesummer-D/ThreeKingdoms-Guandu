# 受约束史料智能体

`corpus.json` 是项目内可审阅的最小知识包，`grounding.js` 负责关键词检索、证据提示词、引用校验和无模型回退。公网服务如果设置 `OPENAI_API_KEY`，会把检索到的卡片交给云端模型；没有 Key、模型失败或模型漏引证据时，自动返回本地摘要。

## 运行方式

```powershell
$env:OPENAI_API_KEY = "..."
$env:OPENAI_MODEL = "gpt-5.4-mini"
node server.js
```

服务端接口：`POST /api/historian/ask`，请求体为 `{ "question": "...", "context": "...", "history": [{"role":"user","content":"..."},{"role":"assistant","content":"..."}] }`。返回 `answer`、`mode`、`retrieved`、实际引用的 `citations` 和 `answerVersion: grounding-v2`。旧版无 history 请求仍然兼容。

历史仅接受最近三组完整 user/assistant 问答，每条最多 1000 字；system 消息、未完成问答与其他字段不进入模型。指代式追问结合最近一组对话检索，新主题单独检索。剧情上下文仅传当前已经显示的文本，不传完整未解锁剧情或本局推测结局。

Unity 的 `HistorianChatUI` 负责透明图标、半透明悬浮窗、自由输入、来源展开和本局记录。PNG 与知识包位于 `Assets/Resources/Historian`，知识包须与服务端 `corpus.json` 同步。断网时客户端读取这份包；模型失败时服务器也可返回摘录。摘录不是新的模型回答。

验证命令：`node server/historian-ai/run-eval.js`（固定史料题），`node server/historian-ai/chat-test.js`（历史、真实 HTTP 合约、错误引用和模型故障），`node server/public-advisor/integration-test.js`（公网会话回归和模型超时）。

## 微调边界

当前先用检索增强和引用校验交付。只有在收集到足量、已审阅的问答对，有可用 GPU、模型许可证和独立评测集时，才考虑 LoRA/指令微调。训练集不能把游戏 IF 分支标成史实，也不能把模型自由生成当作史料。没有这些条件时，保持基础模型 + 本地知识包更稳。模型名通过 `OPENAI_MODEL` 配置，默认值是 `gpt-5.4-mini`，部署前应按账号可用模型和官方文档确认。

`eval.jsonl` 是首批回归集，至少覆盖乌巢、许攸、荀攸、史实结局、游戏资源边界和证据不足拒答。
