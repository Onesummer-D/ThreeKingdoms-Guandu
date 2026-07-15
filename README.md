# 三国·官渡之战 | Three Kingdoms: Battle of Guandu

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3.6f1-blue?style=for-the-badge&logo=unity&logoColor=white" />
  <img src="https://img.shields.io/badge/C%23-61.8%25-green?style=for-the-badge&logo=csharp&logoColor=white" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white" />
  <img src="https://img.shields.io/badge/Genre-Visual%20Novel-orange?style=for-the-badge" />
</p>

<p align="center">
  <b>中文</b> | <a href="#english-version">English</a>
</p>

## 📖 项目简介 | Project Overview

**中文：**  
一款基于 Unity 开发的 2D 剧情解谜冒险游戏，玩家扮演曹操在官渡之战中运筹帷幄。通过战略决策影响兵力、粮草、计策成功率、风险四条核心数值，最终导向 **7 种不同结局**（1 条史实胜利 + 6 条 if 线虚构结局）。包含完整剧情系统、挖地道小游戏、动态音效与自动存档机制。

<div id="english-version">

**English:**  
A Unity-based 2D narrative puzzle adventure game where you play as Cao Cao during the Battle of Guandu. Strategic decisions affect four core metrics: troops, supplies, strategy success rate, and risk, leading to **7 different endings** (1 historical victory + 6 alternate history endings). Features complete narrative system, tunneling mini-games, dynamic audio, and auto-save.

</div>

## 🎮 游戏特性 | Features

- **多结局叙事**: 7 种结局路线，基于历史又超脱历史  
- **战略决策系统**: 兵力、粮草、计策、风险四维数值管理  
- **解谜要素**: 挖地道等互动小游戏  
- **完整音画体验**: 动态音效与视觉呈现  
- **自动存档**: 随时记录你的战略抉择  

## 🛠️ 技术栈 | Tech Stack

- **Engine**: Unity 2022.3.6f1 LTS  
- **Language**: C# (.NET)  
- **Platform**: Windows  
- **Genre**: Visual Novel / Strategy  

## 🚀 运行方式 | How to Run

通过 Unity 打开项目：

```bash
git clone https://github.com/Onesummer-D/ThreeKingdoms-Guandu.git 
# 在 Unity Hub 中添加项目文件夹并打开
```

## 📄 许可证 | License

MIT © 2025 Onesummer-D

# 三国·官渡之战 | Three Kingdoms: Battle of Guandu

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3.6f1-blue?style=for-the-badge&logo=unity&logoColor=white" />
  <img src="https://img.shields.io/badge/C%23-61.8%25-green?style=for-the-badge&logo=csharp&logoColor=white" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white" />
  <img src="https://img.shields.io/badge/Architecture-Data--Driven-orange?style=for-the-badge" />
</p>

---

# 项目简介

**《三国·官渡之战》** 是一款基于 Unity 开发的历史剧情策略游戏。

项目核心并非单纯复现历史剧情，而是探索：

> 如何通过数据驱动架构，实现复杂非线性剧情、动态资源约束与多结局决策系统。

玩家将在官渡之战背景下扮演曹操，通过战略选择影响：

- 兵力
- 粮草
- 计策成功率
- 风险值

系统根据玩家决策动态计算剧情走向，最终产生 **7种不同结局**。


项目重点解决以下工程问题：

- 多分支剧情如何避免大量 if-else 嵌套；
- 数百个剧情节点如何保持可维护性；
- 多维资源如何影响事件结果；
- 游戏逻辑如何实现配置化扩展。

---

# 游戏特性

## 非线性剧情系统

- 200+剧情节点
- 7种结局路线
  - 1条历史胜利路线
  - 6条架空历史路线

玩家不同选择会改变：

- 资源状态
- 后续剧情
- 最终结局

---

## 动态决策系统

设计四维资源模型：

|资源|作用|
|-|-|
|兵力|影响战斗结果|
|粮草|影响持续作战能力|
|计策|影响策略成功概率|
|风险|影响失败概率|


通过资源组合动态决定事件结果，实现：

玩家决策

↓

资源变化

↓

条件判断

↓

剧情分支

↓

结局反馈

---

# 技术架构


## 1. 数据驱动剧情系统

针对传统剧情游戏中大量硬编码导致维护困难的问题，采用：

**ScriptableObject + 配置化节点管理架构**

实现：

- 剧情文本数据与代码解耦；
- 节点、选项、条件独立配置；
- 支持策划人员无需修改代码调整剧情。


核心数据结构：

DialogueDataSO

├── DialogueNode
│
├── DialogueOption
│
├── ResourceEffect
│
└── Condition



每个剧情节点包含：

- 节点ID
- 文本内容
- 角色信息
- 背景资源
- 可选分支
- 资源变化
- 后续节点


---

## 2. 多分支剧情管理系统


设计节点编号规范：
100001
││││││
││││└─事件编号
│││└──章节编号
││└──类型编号
│└──阶段编号



通过统一ID管理：

- 支持200+节点快速索引；
- 避免剧情断链；
- 降低后期扩展成本。


---

## 3. 动态决策算法

设计基于资源阈值的条件判断机制。

相比传统：

```csharp
if(resource > x)
{
    ...
}
else
{
    ...
}

采用配置化条件规则：

资源状态+事件条件+历史选择

↓

动态剧情结果

例如高风险状态下：

失败阈值提升

兵力 < 35

↓

战术失败

使:"玩家行为 → 系统状态 → 剧情反馈"形成闭环

---

# 核心模块

## Dialogue System

负责完整剧情运行流程：

- 节点加载与缓存；
- 剧情节点跳转；
- 玩家选项处理；
- 条件判断与分支触发。


---

## Resource System

管理核心策略资源：

- 兵力（Troops）
- 粮草（Supplies）
- 计策（Strategy）
- 风险（Risk）


支持：

- 资源数值动态变化；
- 状态监听与事件广播；
- UI实时同步。


---

## Mini Game System

包含多个独立交互模块：

- 拼图解谜（Puzzle）
- 网格移动（Grid Movement）
- 挖地道（Tunnel）
- 滑块策略（Slider）


小游戏结果会影响：

- 后续剧情节点；
- 资源状态变化；
- 最终结局判定。


---

## Audio & UI System

负责游戏表现层管理：

- 动态 BGM 切换；
- 音效播放与管理；
- UI状态控制；
- TextMeshPro 字体适配与文本渲染优化。

---

# 技术栈

| 类别 | 技术 |
|---------|------|
| 游戏引擎 | Unity 2022.3.6f1 LTS |
| 语言 | C# |
| 架构 | Data-driven Architecture |
| 数据系统 | ScriptableObject |
| UI | UGUI + TextMeshPro |
| 版本管理 | Git/GitHub |

---

# 快速开始

git clone https://github.com/Onesummer-D/ThreeKingdoms-Guandu.git

使用 Unity Hub 打开项目：

Unity Version:
2022.3.6f1 LTS

---

## 开源协议

MIT License
