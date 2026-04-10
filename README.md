\# 三国·官渡之战 | Three Kingdoms: Battle of Guandu



\&lt;p align="center"\&gt;

&#x20; \&lt;img src="https://img.shields.io/badge/Unity-2022.3.6f1-blue?style=for-the-badge\&logo=unity\&logoColor=white" /\&gt;

&#x20; \&lt;img src="https://img.shields.io/badge/C%23-61.8%25-green?style=for-the-badge\&logo=csharp\&logoColor=white" /\&gt;

&#x20; \&lt;img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge\&logo=windows\&logoColor=white" /\&gt;

&#x20; \&lt;img src="https://img.shields.io/badge/Genre-Visual%20Novel-orange?style=for-the-badge" /\&gt;

\&lt;/p\&gt;



\&lt;p align="center"\&gt;

&#x20; \&lt;b\&gt;中文\&lt;/b\&gt; | \&lt;a href="#english-version"\&gt;English\&lt;/a\&gt;

\&lt;/p\&gt;



\## 📖 项目简介 | Project Overview



\*\*中文：\*\*  

一款基于 Unity 开发的 2D 剧情解谜冒险游戏，玩家扮演曹操在官渡之战中运筹帷幄。通过战略决策影响兵力、粮草、计策成功率、风险四条核心数值，最终导向 \*\*7 种不同结局\*\*（1 条史实胜利 + 6 条 if 线虚构结局）。包含完整剧情系统、挖地道小游戏、动态音效与自动存档机制。



\*\*English:\*\*  

A Unity-based 2D narrative puzzle adventure game where players take the role of \*\*Cao Cao\*\* in the historic Battle of Guandu. Make strategic decisions affecting four core metrics (Troops, Supplies, Strategy Success Rate, and Risk) to unlock \*\*7 unique endings\*\* (1 historical + 6 alternative timelines). Features complete narrative system, tunnel-digging mini-game, dynamic audio, and auto-save.



\## 🎮 运行环境 | System Requirements



| 配置 | 要求 |

|------|------|

| \*\*操作系统\*\* | Windows 10/11 (64-bit) |

| \*\*运行内存\*\* | 4GB 及以上 |

| \*\*硬盘空间\*\* | 500MB 可用空间 |

| \*\*开发环境\*\* | Unity 2022.3.6f1 LTS (查看源码需要) |

| \*\*音频\*\* | 建议佩戴耳机体验完整音效 |



\## 🚀 快速开始 | Quick Start



\### 直接游玩 | Play Now

1\. 进入 `可运行版本/` 文件夹

2\. 双击 `官渡之战.exe`

3\. 享受游戏！建议佩戴耳机 🎧



\### 查看源码 | View Source

1\. 安装 Unity Hub 和 Unity 2022.3.6f1 LTS

2\. 在 Unity Hub 点击 "Open" → 选择本项目根目录（包含 `Assets` 的文件夹）

3\. 等待 Packages 自动下载完成（需联网）



\## 🎯 操作说明 | Controls



\- \*\*🖱️ 鼠标左键\*\*：推进剧情对话、选择战略选项

\- \*\*📊 实时面板\*\*：游戏界面左下角显示四条动态数值进度条

&#x20; - 兵力 (Troops)

&#x20; - 粮草 (Supplies)  

&#x20; - 计策成功率 (Strategy Rate)

&#x20; - 风险系数 (Risk Level)

\- \*\*💾 存档机制\*\*：关键剧情节点自动保存，支持多结局回溯



\## 📂 项目结构 | Project Structure

ThreeKingdoms-Guandu/

├── Assets/              # Unity 核心资源

│   ├── Scripts/         # C# 游戏逻辑脚本

│   ├── Scenes/          # 剧情场景文件

│   ├── Resources/       # 音频、字体、图片资源

│   └── ...

├── Packages/            # Unity 包管理清单

├── ProjectSettings/     # Unity 项目配置

├── 可运行版本/          # Windows Build 输出（含 .exe 和 \_Data）

├── 设计文档/            # 技术架构 \& 剧情节点规划表

└── README.md           # 本文件



\## ⚠️ 注意事项 | Important Notes



1\. \*\*不要删除\*\* `官渡之战\_Data` 文件夹，否则游戏无法运行（包含资源文件）

2\. \*\*杀毒软件误报\*\*：Unity 生成的 exe 可能被误报为病毒，请添加信任或排除

3\. \*\*首次打开源码\*\*：Unity 会自动下载 Packages，请保持网络连接，首次加载约 5-10 分钟

4\. \*\*退出游戏\*\*：按 `Alt+F4` 或点击窗口右上角关闭按钮



\## 🏆 特色功能 | Features



\- ✅ \*\*多分支剧情\*\*：7 种不同结局（史实胜利、许攸投奔、奇袭失败等 if 线）

\- ✅ \*\*挖地道小游戏\*\*：QTE 机制增加沉浸感

\- ✅ \*\*动态数值系统\*\*：四维度策略博弈

\- ✅ \*\*完整音效\*\*：BGM + 音效 + 字体资源全部配置完毕

\- ✅ \*\*自动存档\*\*：关键节点自动保存进度



\## 📜 开源协议 | License



本项目为课程作业/参赛作品，仅供学习交流使用。



\---



<h2 id="english-version">🌐 English Version</h2>



\*\*System Requirements:\*\*

\- OS: Windows 10/11 (64-bit)

\- RAM: 4GB+

\- Storage: 500MB available

\- Engine: Unity 2022.3.6f1 LTS



\*\*How to Play:\*\*

1\. Navigate to `可运行版本/` folder

2\. Run `官渡之战.exe`

3\. Play with headphones for best experience



\*\*Controls:\*\*

\- Left Click: Advance dialogue \& make choices

\- Real-time HUD: Bottom-left corner shows 4 metrics (Troops, Supplies, Strategy, Risk)

\- Auto-save: Progress saved automatically at key story nodes



\*\*Features:\*\*

\- 7 Multiple Endings (1 historical + 6 alternative)

\- Tunnel-digging mini-game with QTE

\- Complete narrative system with audio \& fonts

\- Auto-save system



\---



<p align="center">

&#x20; <b>开发者 | Developer:</b> Onesummer-D<br>

&#x20; <b>学校 | School:</b> Minzu University of China (中央民族大学)<br>

&#x20; <b>状态 | Status:</b> ✅ Completed (开发完成)

</p>

