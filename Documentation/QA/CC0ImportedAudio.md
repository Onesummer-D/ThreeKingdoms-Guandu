# 官渡之战 CC0 音效导入台账

下列音频源自 OpenGameArt 页面明示的 CC0 资源。
已导入为 Unity 可识别 OGG 及固定 GUID 的 .meta 文件；其中 **6 个动作明确的剧情节点已绑定为待验收的试听版本**，其余素材先保存在资源库中。合并前请在 Unity Game 视图确认音色和播放时机。

| 音频文件 | 用途 | 作者和授权 | 原始素材 | 时长 | SHA256 |
|---|---|---|---|---:|---|
| FireIgnition.ogg | 乌巢初燃与木料噼啪 | AntumDeluge / CC0 | [来源](https://opengameart.org/content/fire-crackling) / fire_crackle.ogg | 3.55s | f922929805bc6afdf0a35727fa3998033db544c1b6f62ed3aeab94341ed6ddeb |
| FireLoop.ogg | 乌巢持续燃烧环境声 | PagDev / CC0 | [来源](https://opengameart.org/content/fireplace-sound-loop) / fire_loop.wav | 29.26s | 356d30240c0b2f7068ba51ebb24d0a2ee949c9d36d82c4299410413b8c6c3ade |
| BattleShout.ogg | 战场群体呼喊 | StarNinjas / CC0 | [来源](https://opengameart.org/content/crowd-shoutingspeaking-ambience) / crowd.ogg | 6.00s | b762f6e59335b19139b5573965765034d1554d6ab413e6fadf29d475081b8b68 |
| SwordClashA.ogg | 兵刃交锋变体一 | StarNinjas / CC0 | [来源](https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes) / sword_clash.4.ogg | 0.92s | 4c2c962f8703882b8a6f863c1f5a35c6ae518d0281755fac9a13e63a73b86fa5 |
| SwordClashB.ogg | 兵刃交锋变体二 | StarNinjas / CC0 | [来源](https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes) / sword_clash.7.ogg | 1.17s | 6763e519e7b31250a78f514d2b8b4f4e73a3bb105c980844ea687d698364b80c |
| RockBreak.ogg | 石块裂解和霹雳车重击 | rubberduck / CC0 | [来源](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx) / bfh1_rock_breaking_02.ogg | 0.86s | 3e06b794511f693b6aaca5eee1d47491bab74d814746a68de6c8bf4eec744ccc |
| RockFall.ogg | 地道塌方落石 | rubberduck / CC0 | [来源](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx) / bfh1_rock_falling_03.ogg | 0.85s | 16080f9bc90924b0e4ede52922529e30e34e72af32a505b39bdfc5ebcbb9248c |
| WoodBreak.ogg | 木制攻城楼断裂 | rubberduck / CC0 | [来源](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx) / bfh1_wood_breaking_03.ogg | 0.49s | 0d230f703fe558b61e93cbd4d7cc6ebb5b73e752bcdd13f5c4347c013b405ca7 |
| WoodFall.ogg | 木楼坍塌收尾 | rubberduck / CC0 | [来源](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx) / bfh1_wood_falling_01.ogg | 0.70s | 36e5d8905268c641c3d391c10f0297b9c46ba30d3b551e24d59eafcf33643a10 |
| SwordDrop.ogg | 倚天剑落地 | rubberduck / CC0 | [来源](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) / metal_falling_01.ogg | 1.79s | 84f68de4298918630f328087486619e4b80892f7655724f9d391af730b37cea0 |
| WoodThud.ogg | 拍案或重物闷响 | rubberduck / CC0 | [来源](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) / wood_slam_02.ogg | 0.48s | 73606869502682f640ebc0ab330c66801650a58a363f90c93c7882cd1abb587f |

CC0 不强制署名，保留出处是为了比赛和开源发布材料可追溯。
这些文件是根据用途与原始文件名筛选的候选素材，尚未经过 Unity 内实机听感验收。
## 已预绑定的 6 个剧情节点（试听版）

| 节点 | 音效 | 动作 | 
|---|---|---|
| 100206 | RockBreak | 霹雳车巨石轰击 |
| 100305 | RockFall | 地道被土石掩埋 |
| 300401 | WoodThud | 曹操拍案 |
| 500207 | SwordClashA | 乌巢近战冲突 |
| 500208 | FireIgnition | 粮囤点火 |
| 500417 | SwordDrop | 倚天剑坠地 |

FireLoop（29 秒）是持续环境音，不直接作为 sfxOnEnter 绑定，否则剧情快速推进后它仍会播放。BattleShout 以及其他木料/刀剑变体先保存为可选择音源，不未经试听便强行触发。

**仍缺专门的霹雳车机械发射/绞盘声**，不能拿石头撞击声假装机械启动。源音频虽然授权为 CC0，但仍应保存来源页面的授权截图，以备作品提交时使用。

### 本地使用
合并本 PR，关闭 Unity，在项目根目录执行 git pull --ff-only origin main；重新打开 Unity 等待导入即可在 Assets/Audio/ImportedCC0 找到 11 个 OGG。再次开局游玩上述 6 个节点确认实际声音，其他素材可以在 Inspector 里试听后再决定是否绑定。
