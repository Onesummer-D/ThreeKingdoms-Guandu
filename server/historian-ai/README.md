# 受约束史料智能体

`corpus.json` 是项目内可审阅的最小知识包，`grounding.js` 负责短语/中文二字词片段加权检索、相关性门槛、逐句引用及数字/引文检查和无模型回退；`run-evidence.js` 专门对本局真实游戏记录做可溯源复盘，使用 [G01]（非历史史料）。这些词法检索与校验并不是向量语义检索或事实蕴含证明。公网服务如果设置 `OPENAI_API_KEY`，会把检索到的卡片交给云端模型；没有 Key、模型失败或模型漏引证据时，自动返回本地摘要。

## 运行方式

```powershell
$env:OPENAI_API_KEY = "..."
$env:OPENAI_MODEL = "gpt-5.4-mini"
node server.js
```

服务端接口：`POST /api/historian/ask`，请求体为 `{ "question": "...", "context": "...", "history": [{"role":"user","content":"..."},{"role":"assistant","content":"..."}], "run": { "...": "可选的本局结构化战况" } }`。返回 `answer`、`mode`、`retrieved`、实际引用的 `citations` 和 `answerVersion: grounding-v3`。旧版无 history 请求仍然兼容。

历史仅接受最近三组完整 user/assistant 问答，每条最多 1000 字；system 消息、未完成问答与其他字段不进入模型。指代式追问结合最近一组对话检索，新主题单独检索。剧情上下文仅传当前已经显示的文本，不传完整未解锁剧情或本局推测结局。

Unity 的 `HistorianChatUI` 负责透明图标、半透明悬浮窗、自由输入、来源展开和本局记录。发给后端的 `run` 是已发生数据的精简结构，仅包含资源起终快照、最多40条实际选择及其前后快照、最多12项小游戏结果以及**已抵达**的结局评价；不发送玩家昵称、参谋身份或未解锁节点。未达到结局时不会发出任何终局字段。PNG 与知识包位于 `Assets/Resources/Historian`，知识包须与服务端 `corpus.json` 同步。断网时客户端读取这份包；模型失败时服务器也可返回摘录。摘录不是新的模型回答。

验证命令：`node server/historian-ai/run-eval.js`（固定史料题），`node server/historian-ai/chat-test.js`（历史、真实 HTTP 合约、错误引用和模型故障），`node server/public-advisor/integration-test.js`（公网会话回归和模型超时）。

## 微调边界

当前先用检索增强和引用校验交付。只有在收集到足量、已审阅的问答对，有可用 GPU、模型许可证和独立评测集时，才考虑 LoRA/指令微调。训练集不能把游戏 IF 分支标成史实，也不能把模型自由生成当作史料。没有这些条件时，保持基础模型 + 本地知识包更稳。模型名通过 `OPENAI_MODEL` 配置，默认值是 `gpt-5.4-mini`，部署前应按账号可用模型和官方文档确认。

`eval.jsonl` 是首批回归集，至少覆盖乌巢、许攸、荀攸、史实结局、游戏资源边界和证据不足拒答。

## v3 证据边界与部署

- **历史问答 [Hxx]**：词法相关性筛选最多4张史料卡，模型只能引用候选卡；按事实句检查引用编号、局部词组重合、未见数字与未见原文引语。不合规返回卡片原有摘要。规则无法保证复杂语义完全正确，需要文案组定期人工评审。
- **本局回顾 [G01]**：请求“我为什么会输”“本局怎么选择的”等问题时，直接使用实际已发生的游戏记录生成确定性说明；只有通关后才说明结局。结局“原因”是游戏自身的设计评价，不能证明现实历史因果，也不推演玩家未游玩的分支。无记录时明确拒绝。
- **无外部依赖**：不需要新数据库或 Embedding API；可以在文案组扩展知识库后再讨论语义检索。若增补知识卡，必须同步 `Assets/Resources/Historian/corpus.json` 和 `server/historian-ai/corpus.json`。
- 本次仅修改仓库源代码，**不会自动更新运行中的公网服务器**。部署后核验响应的 `answerVersion=grounding-v3`；服务器的实际模型由 `OPENAI_MODEL` 和 `OPENAI_API_URL` 环境变量决定。
