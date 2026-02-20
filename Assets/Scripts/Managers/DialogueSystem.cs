using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 对话选项的数据结构（用于运行时）
[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public int nextNodeId;
    public float troopEffect;
    public float foodEffect;
    public float strategyEffect;
    public float riskEffect;
    public string miniGameType;
    public string condition;
}

// 对话节点的数据结构（用于运行时）
[System.Serializable]
public class DialogueNode
{
    public int nodeId;
    public string dialogueText;
    public string backgroundImage;
    public string leftCharacter;
    public string rightCharacter;
    public List<DialogueOption> options = new List<DialogueOption>();

    // ✅ 新增：纯文本节点的跳转目标
    public int nextNodeId = 0;  // 0表示无跳转，使用options；>0表示纯文本节点跳转到此ID

    public bool IsPureTextNode
    {
        get { return options == null || options.Count == 0; }
    }
}

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    // UI组件引用
    public TMP_Text dialogueTextDisplay;
    public Button[] optionButtons;
    public TMP_Text resourceTextDisplay;
    public Image backgroundImageDisplay;
    public Image leftCharacterDisplay;
    public Image rightCharacterDisplay;


    // ========== 新增：事件定义 ==========
    public event Action<int, DialogueOption> OnOptionSelected;
    public event Action<int> OnDialogueNodeShown;  // 新增：节点显示事件

    // ========== 公开属性 ==========
    public DialogueNode CurrentNode { get { return currentNode; } }
    public bool IsShowing { get; private set; }
    // =============================

    // 当前对话节点
    private DialogueNode currentNode;

    // 数据加载状态
    private bool isDataLoaded = false;
    private Dictionary<int, DialogueNode> loadedNodes = new Dictionary<int, DialogueNode>();

    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("DialogueSystem初始化完成！");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 等待DataLoader初始化
        StartCoroutine(InitializeAfterDataLoader());
    }

    IEnumerator InitializeAfterDataLoader()
    {
        // 等待DataLoader初始化
        yield return new WaitUntil(() => DataLoader.Instance != null);

        // 额外等待一帧确保数据加载完成
        yield return null;

        // 尝试加载数据
        LoadAllDialogueData();

        if (isDataLoaded && loadedNodes.Count > 0)
        {
            Debug.Log($"成功加载 {loadedNodes.Count} 个对话节点，开始游戏");
        }
        else
        {
            Debug.LogError("对话数据加载失败！使用备用测试数据");
            CreateBackupTestData();
        }
    }

    // 从DataLoader加载所有对话数据
    void LoadAllDialogueData()
    {
        if (DataLoader.Instance == null || DataLoader.Instance.dialogueData == null)
        {
            Debug.LogError("DataLoader未初始化或未设置对话数据");
            return;
        }

        loadedNodes.Clear();

        foreach (var nodeData in DataLoader.Instance.dialogueData.dialogueNodes)
        {
            DialogueNode node = ConvertToDialogueNode(nodeData);
            if (node != null)
            {
                loadedNodes[node.nodeId] = node;
                Debug.Log($"加载节点: {node.nodeId}, 选项数: {node.options.Count}");
            }
        }

        isDataLoaded = loadedNodes.Count > 0;
        Debug.Log($"对话数据加载完成: {loadedNodes.Count}个节点");
    }

    // 将ScriptableObject数据转换为运行时数据
    DialogueNode ConvertToDialogueNode(DialogueNodeData nodeData)
    {
        if (nodeData == null) return null;

        DialogueNode node = new DialogueNode
        {
            nodeId = nodeData.nodeId,
            dialogueText = nodeData.dialogueText,
            backgroundImage = nodeData.backgroundImage,
            leftCharacter = nodeData.leftCharacter,
            rightCharacter = nodeData.rightCharacter,
            nextNodeId = nodeData.nextNodeId
        };

        if (nodeData.options != null)
        {
            foreach (var optionData in nodeData.options)
            {
                DialogueOption option = new DialogueOption
                {
                    optionText = optionData.optionText,
                    nextNodeId = optionData.nextNodeId,
                    troopEffect = optionData.troopEffect,
                    foodEffect = optionData.foodEffect,
                    strategyEffect = optionData.strategyEffect,
                    riskEffect = optionData.riskEffect,
                    miniGameType = optionData.miniGameType,
                    condition = optionData.condition
                };
                node.options.Add(option);
            }
        }
        return node;
    }

    // 备用测试数据（当DataLoader失败时使用）
    void CreateBackupTestData()
    {
        // 节点1000-1：背景介绍1
        DialogueNode node1000_1 = new DialogueNode
        {
            nodeId = 100001,  // 用100001表示1000-1
            dialogueText = "建安五年秋，黄河之畔，肃杀之气弥漫。袁绍率河北四州之兵十余万，与曹操隔河对峙。这是一场决定北方命运的决战——官渡之战。从纸面看，这近乎一边倒：袁绍十一万，曹操两万，兵力比超5:1。袁绍旌旗蔽日；曹军如惊涛前的单薄堤坝。",
            backgroundImage = "bg_camp",
            leftCharacter = "",
            rightCharacter = ""
        };
        loadedNodes[node1000_1.nodeId] = node1000_1;

        // 节点1000-2：背景介绍2
        DialogueNode node1000_2 = new DialogueNode
        {
            nodeId = 100002,
            dialogueText = "曹操将决战地锁定官渡，距许都仅87公里，扼袁绍南下咽喉。此选择抵消兵力劣势，发挥内线优势。袁绍\"好谋无断\"，曹操务实果决。性格差异，已注定走向。前哨战中，关羽斩颜良，曹操破文丑，挫袁军锐气。然真正的较量，是后勤与忍耐的绞杀。",
            backgroundImage = "bg_camp",
            leftCharacter = "",
            rightCharacter = ""
        };
        loadedNodes[node1000_2.nodeId] = node1000_2;

        // 节点1001：剧情点1选择
        DialogueNode node1001 = new DialogueNode
        {
            nodeId = 1001,
            dialogueText = "建安五年秋八月，官渡。黄河奔流，冲不散两岸杀气。袁绍连营数十里，更筑土山、架高橹，弓弩手居高临下，矢下如雨。曹军顶盾取水，亦常横死。曹操看着穿透帐幕、钉入案几的箭矢，夜风呼啸如鬼哭。若不能破此空中优势，这四万兵，怕要全射死在这烂泥地里。",
            backgroundImage = "bg_battlefield",
            leftCharacter = "曹操",
            rightCharacter = ""
        };

        // 选项A
        DialogueOption optionA = new DialogueOption
        {
            optionText = "连夜赶造发石车（霹雳车），以牙还牙",
            nextNodeId = 1002,
            troopEffect = 1.0f,
            miniGameType = "puzzle"
        };
        node1001.options.Add(optionA);

        // 选项B
        DialogueOption optionB = new DialogueOption
        {
            optionText = "令将士挖掘地道，奇袭袁营",
            nextNodeId = 1003,
            foodEffect = -1.0f
        };
        node1001.options.Add(optionB);

        // 选项C
        DialogueOption optionC = new DialogueOption
        {
            optionText = "佯装败退，设下埋伏",
            nextNodeId = 1004,
            miniGameType = "slider"
        };
        node1001.options.Add(optionC);

        loadedNodes[node1001.nodeId] = node1001;

        // 节点1002：拼图成功
        DialogueNode node1002 = new DialogueNode
        {
            nodeId = 1002,
            dialogueText = "刘晔献策：\"袁绍高橹虽高，乃死物也。\"当夜工兵营灯火通明，依图纸赶制数十架战车。黎明，发石车一字排开，木臂弹起，雷霆轰鸣，曹操命名\"霹雳车\"。磨盘大石呼啸破空，高橹崩塌粉碎。袁军惊呼神兵，再无人敢登高。曹军欢呼震天，曹操抚须长笑。",
            backgroundImage = "bg_battlefield",
            leftCharacter = "曹操",
            rightCharacter = ""
        };
        DialogueOption continue1002 = new DialogueOption
        {
            optionText = "继续",
            nextNodeId = 200101  // 跳转到2001-1
        };
        node1002.options.Add(continue1002);
        loadedNodes[node1002.nodeId] = node1002;

        // 节点1003：挖地道失败
        DialogueNode node1003 = new DialogueNode
        {
            nodeId = 1003,
            dialogueText = "\"明枪易躲，暗箭难防。\"曹操令工兵挖掘地道。数千将士在潮湿地道中艰难前行。然袁绍非庸才，采纳谋士建议，于营内挖深堑。头顶土层松动，长矛刺下！地道成死亡陷阱，哭喊被泥土隔绝。精锐惨死，余众病倒，医帐躺满虚弱伤兵。粮官红字触目惊心，曹操心生寒意。",
            backgroundImage = "bg_battlefield",
            leftCharacter = "曹操",
            rightCharacter = ""
        };
        DialogueOption continue1003 = new DialogueOption
        {
            optionText = "继续",
            nextNodeId = 200101
        };
        node1003.options.Add(continue1003);
        loadedNodes[node1003.nodeId] = node1003;

        // 节点1004：埋伏输入（特殊节点，无选项）
        DialogueNode node1004 = new DialogueNode
        {
            nodeId = 1004,
            dialogueText = "\"兵者，诡道也。\"曹操令大军佯退，踢翻饭锅，制造恐慌假象。大军向延津隘口佯退。需要设置多少兵力埋伏？（合理范围：1.0-3.0）",
            backgroundImage = "bg_battlefield",
            leftCharacter = "曹操",
            rightCharacter = ""
            // options为空，使用滑块输入
        };
        loadedNodes[node1004.nodeId] = node1004;

        // 节点1005：埋伏成功
        DialogueNode node1005 = new DialogueNode
        {
            nodeId = 1005,
            dialogueText = "袁军前锋见曹军\"溃退\"，大喜，率骑兵争相追赶。待其钻入狭窄隘口，曹操令旗一挥！两侧伏兵如猛虎杀出，截断退路。马蹄声碎，刀光如雪。袁军自相践踏，血流漂橹。望着满地战马辎重，曹操知此不仅是胜仗，更是紧缺粮草的补给。",
            backgroundImage = "bg_battlefield",
            leftCharacter = "曹操",
            rightCharacter = ""
        };
        DialogueOption continue1005 = new DialogueOption
        {
            optionText = "继续",
            nextNodeId = 200101
        };
        node1005.options.Add(continue1005);
        loadedNodes[node1005.nodeId] = node1005;

        // 节点1006：埋伏失败
        DialogueNode node1006 = new DialogueNode
        {
            nodeId = 1006,
            dialogueText = "为演逼真，曹操令大军后撤过远，弃\"淇水\"天险。然沮授劝袁绍：\"曹操多谋，今退必诈。\"袁绍竟听了劝。曹军\"败退\"成真溃败，袁军营寨直逼眼皮。为夺回营盘，士兵顶雨冲锋，偏将战死。防线压缩，营外即是袁军刁斗声，曹操感前所未有的挫败。",
            backgroundImage = "bg_battlefield",
            leftCharacter = "曹操",
            rightCharacter = ""
        };
        DialogueOption continue1006 = new DialogueOption
        {
            optionText = "继续",
            nextNodeId = 200101
        };
        node1006.options.Add(continue1006);
        loadedNodes[node1006.nodeId] = node1006;

        // 节点2001-1：剧情点2-1
        DialogueNode node2001_1 = new DialogueNode
        {
            nodeId = 200101,
            dialogueText = "官渡相持半载，曹军粮尽。后方补给线脆弱，运粮士卒面如菜色。粮食配给掺杂野菜，士兵半饱难维持，逃亡者众。曹操捏着粮秣文书：全军存粮，仅够三日。退回许都的念头，从未如此诱人。他提笔给荀彧写信：\"今兵少粮尽，士卒疲乏，吾欲暂还许，文若以为如何？\"",
            backgroundImage = "bg_camp",
            leftCharacter = "曹操",
            rightCharacter = "",
            nextNodeId = 200102
        };
        loadedNodes[node2001_1.nodeId] = node2001_1;

        // 节点2001-2：剧情点2选择
        DialogueNode node2001_2 = new DialogueNode
        {
            nodeId = 200102,
            dialogueText = "数日后，荀彧回信至。字句如重锤：\"公以至弱当至强，若不能制，必为所乘，是天下之大机也。昔高祖、项羽莫肯先退，先退者势屈也。情见势竭，必将有变，此用奇之时，不可失也。\"曹操攥信，帐外饥卒呻吟，帐内墨香犹存。是退，还是守？",
            backgroundImage = "bg_camp",
            leftCharacter = "曹操",
            rightCharacter = ""
        };

        // 选项A：坚守
        DialogueOption option2A = new DialogueOption
        {
            optionText = "采纳文若之谏，坚守官渡，等待战机",
            nextNodeId = 2002
        };
        node2001_2.options.Add(option2A);

        // 选项B：撤退（IF线）
        DialogueOption option2B = new DialogueOption
        {
            optionText = "粮草乃根本，不如暂回许都，再图后举",
            nextNodeId = 9001  // IF线1开始
        };
        node2001_2.options.Add(option2B);

        // 选项C：商议
        DialogueOption option2C = new DialogueOption
        {
            optionText = "召集众将商议对策",
            nextNodeId = 2003,
            foodEffect = -0.5f
        };
        node2001_2.options.Add(option2C);

        loadedNodes[node2001_2.nodeId] = node2001_2;

        // 节点2002：坚守分支
        DialogueNode node2002 = new DialogueNode
        {
            nodeId = 2002,
            dialogueText = "\"文若所言极是。\"曹操将信置于案上，\"楚汉对峙，高祖未退，项羽势竭而败。今绍之众聚而不能用；我军扼其喉已半年矣。情见势竭，变必自生！\"他环视文武：\"传令：固垒深沟，持重待机。粮秣再省，也需再守十日！退者，斩！\"",
            backgroundImage = "bg_camp",
            leftCharacter = "曹操",
            rightCharacter = ""
        };
        DialogueOption continue2002 = new DialogueOption
        {
            optionText = "继续",
            nextNodeId = 3001  // 剧情点3
        };
        node2002.options.Add(continue2002);
        loadedNodes[node2002.nodeId] = node2002;

        // 节点2003：商议分支
        DialogueNode node2003 = new DialogueNode
        {
            nodeId = 2003,
            dialogueText = "曹操召集众将。曹洪主张死战，夏侯渊建议撤退，荀攸沉默。众人争论不休，从日中吵到日落。曹操看着这些跟随多年的将领，五味杂陈。最终决定坚守，然这一日争吵，已消耗宝贵粮草。",
            backgroundImage = "bg_camp",
            leftCharacter = "曹操",
            rightCharacter = ""
        };
        DialogueOption continue2003 = new DialogueOption
        {
            optionText = "继续",
            nextNodeId = 3001
        };
        node2003.options.Add(continue2003);
        loadedNodes[node2003.nodeId] = node2003;

        // IF线1-1：决定撤退
        DialogueNode nodeIF1_1 = new DialogueNode
        {
            nodeId = 9001,
            dialogueText = "建安五年十月，霜寒刺骨。曹操未拆荀彧来信，提笔下令：\"粮道已绝，军无战心。三日后拔营，退还许都。\"郭嘉病重，荀攸欲言被止。他选择\"避害\"：撤回许都，再图后举。至于撤退途中的追击、政治旗帜的动摇，都被\"活着撤回\"的诉求搁置。",
            backgroundImage = "bg_defeat",
            leftCharacter = "曹操",
            rightCharacter = ""
        };
        loadedNodes[nodeIF1_1.nodeId] = nodeIF1_1;

        // IF线1-2：结局
        DialogueNode nodeIF1_2 = new DialogueNode
        {
            nodeId = 9002,
            dialogueText = "袁绍闻报，派文丑、刘备率骑追击，主力尾随。曹军撤退即溃，饥饿与松弛摧毁纪律。文丑骑兵冲击后卫，夏侯渊、于禁拼死断后，节节败退。无名河畔，中军被击穿，败兵倒卷。曹操弃车乘马，向南疾走。退至许都，十损三四，官渡防线瓦解。\n\n═══════════════════════════════\n【结局：半途而废】\n因粮草不济而撤退，错失战机，功败垂成。\n═══════════════════════════════",
            backgroundImage = "bg_defeat",
            leftCharacter = "曹操",
            rightCharacter = ""
        };

        // 添加"查看结局"选项
        DialogueOption optionViewEnding = new DialogueOption
        {
            optionText = "查看结局",
            nextNodeId = 900201  // 跳转到结局标题页
        };
        nodeIF1_2.options.Add(optionViewEnding);

        // 节点900201：IF线1结局标题（最终页）
        DialogueNode nodeIF1_2_End = new DialogueNode
        {
            nodeId = 900201,
            dialogueText = "═══════════════════════════════\n【结局：半途而废】\n因粮草不济而撤退，错失战机，功败垂成。\n═══════════════════════════════",
            backgroundImage = "bg_defeat",
            leftCharacter = "",
            rightCharacter = ""
        };
        loadedNodes[nodeIF1_2_End.nodeId] = nodeIF1_2_End;

        Debug.Log("备用测试数据创建完成：包含背景、剧情点1-2、IF线1");

        // 节点3001：视角切换
        DialogueNode node3001 = new DialogueNode
        {
            nodeId = 3001,
            dialogueText = "正当曹操于曹营的饥饿与荀彧书信的激荡中挣扎时，黄河对岸的袁军大营，却是另一番景象。袁绍的中军大帐奢华宽阔，酒食终日不绝。但他眉宇间并无多少胜券在握的从容，反而时常流露出烦躁。案前，谋士们的争吵几乎成了日常。",
            backgroundImage = "bg_yuanshao_camp",
            leftCharacter = "",
            rightCharacter = ""
        };
        loadedNodes[node3001.nodeId] = node3001;

        // 节点3002：许攸夜访选择
        DialogueNode node3002 = new DialogueNode
        {
            nodeId = 3002,
            dialogueText = "建安五年冬，寒风卷着枯叶，在官渡营盘上打着旋儿。中军大帐内，烛火摇曳。曹操正倚案假寐，忽闻帐外争执：\"军中重地，不得擅闯！\"\"你去报与曹公，就说南阳故人许攸子远到了！\"曹操猛然惊醒，跣足而出。只见一人衣冠不整，满面风霜，正是弃袁绍而来的许攸。入帐落座，许攸直视曹操，意味深长地问道：\"明公孤军在此，四面受敌，袁本初拥兵十万，粮草如山。我且问明公，军中粮草，尚余几许？\"",
            backgroundImage = "bg_camp_night",
            leftCharacter = "曹操",
            rightCharacter = "许攸"
        };

        // 选项A：坦诚
        DialogueOption option3A = new DialogueOption
        {
            optionText = "坦诚相告，推心置腹",
            nextNodeId = 3003,
            strategyEffect = 1.0f
        };
        node3002.options.Add(option3A);

        // 选项B：说谎
        DialogueOption option3B = new DialogueOption
        {
            optionText = "虚与委蛇，试探虚实",
            nextNodeId = 3004,
            strategyEffect = 0.5f
        };
        node3002.options.Add(option3B);

        // 选项C：斩首（IF线2）
        DialogueOption option3C = new DialogueOption
        {
            optionText = "疑心生暗鬼，推出去斩首",
            nextNodeId = 9003
        };
        node3002.options.Add(option3C);

        loadedNodes[node3002.nodeId] = node3002;

        // 节点3003：坦诚结果
        DialogueNode node3003 = new DialogueNode
        {
            nodeId = 3003,
            dialogueText = "曹操长叹一声：\"子远既至，孤不敢相欺。军粮已告罄，不过仅剩一月之数。\"说罢深施一礼：\"若子远有良策以解倒悬之危，曹某愿以此身相托！\"许攸见曹操坦诚至此，心中隔阂烟消云散。他起身正色道：\"孟德能如此推心置腹，是以微末之卒抗衡强敌之根本也！袁绍辎重万余，尽屯乌巢，守将淳于琼嗜酒无备。若选精锐，衔枚疾走，袭而焚之，不出三日，袁绍大军自溃！\"",
            backgroundImage = "bg_camp_night",
            leftCharacter = "曹操",
            rightCharacter = "许攸"
        };
        DialogueOption continue3003 = new DialogueOption
        {
            optionText = "继续",
            nextNodeId = 4001
        };
        node3003.options.Add(continue3003);
        loadedNodes[node3003.nodeId] = node3003;

        // 节点3004：说谎结果
        DialogueNode node3004 = new DialogueNode
        {
            nodeId = 3004,
            dialogueText = "曹操沉吟道：\"军粮虽不比袁本初殷实，但支撑一年，尚无大碍。\"许攸嘴角勾起一抹似笑非笑：\"一年？\"曹操改口：\"即便一年不可，支撑半载，应当无虞。\"许攸逼问：\"半载？明公莫不是在骗我？\"曹操强撑：\"即便......三月，亦足可周旋。\"许攸大笑：\"孟德啊孟德，你军粮早已告罄，不过数日之用罢了！\"他轻叹一声：\"罢了，袁绍乌巢粮仓，乃其命门所在，若不速图，悔之晚矣。\"",
            backgroundImage = "bg_camp_night",
            leftCharacter = "曹操",
            rightCharacter = "许攸"
        };
        DialogueOption continue3004 = new DialogueOption
        {
            optionText = "继续",
            nextNodeId = 4001
        };
        node3004.options.Add(continue3004);
        loadedNodes[node3004.nodeId] = node3004;

        // IF线2-1：斩首
        DialogueNode node9003 = new DialogueNode
        {
            nodeId = 9003,
            dialogueText = "曹操拍案而起：\"大胆狂徒！分明是审配派来的细作！推出去斩首！\"许攸被拖拽而出，凄厉高呼：\"曹阿瞒！你这多疑匹夫！今日杀我，便是断了你唯一的生路！乌巢之粮若在，袁本初铁骑踏平此营只在朝夕！\"喊叫声渐弱，终归于死寂。",
            backgroundImage = "bg_camp_night",
            leftCharacter = "曹操",
            rightCharacter = "许攸",
            nextNodeId = 9004
        };
        loadedNodes[node9003.nodeId] = node9003;

        DialogueNode node9004 = new DialogueNode
        {
            nodeId = 9004,
            dialogueText = "曹操立于帐中，久久未动，背脊生寒。那一夜，乌巢守军依然酣睡，那把焚尽袁绍根基的火，尚未点燃便已埋葬。一月后，曹军粮尽，士卒逃亡，人相食。袁绍侦得曹军已乱，下令总攻。袁军势如破竹，曹营大乱。曹操仅率数百骑突围，狼狈逃回许昌。官渡一败，大势已去，北方遂定袁氏。",
            backgroundImage = "bg_defeat",
            leftCharacter = "",
            rightCharacter = ""
        };

        DialogueOption optionViewEnding2 = new DialogueOption
        {
            optionText = "查看结局",
            nextNodeId = 900403
        };
        node9004.options.Add(optionViewEnding2);

        loadedNodes[node9004.nodeId] = node9004;

        // 900403：结局标题（最终页）
        DialogueNode node900403 = new DialogueNode
        {
            nodeId = 900403,
            dialogueText = "═══════════════════════════════\n【结局：错失良机】\n因猜忌错杀来投谋士，失去关键情报，最终粮尽兵败。\n═══════════════════════════════",
            backgroundImage = "bg_defeat",
            leftCharacter = "",
            rightCharacter = ""
        };
        loadedNodes[node900403.nodeId] = node900403;

        Debug.Log("所有节点：");
        foreach (var kvp in loadedNodes)
        {
            Debug.Log($"节点 {kvp.Key}: {kvp.Value.dialogueText.Substring(0, 20)}...");
        }

        // ========== 剧情点4：奇袭乌巢 ==========

        // 节点4001：奇袭选择（2选项）
        DialogueNode node4001 = new DialogueNode
        {
            nodeId = 4001,
            dialogueText = "建安五年冬，官渡大帐内烛火幽微。许攸之计已在案，然\"乌巢\"二字重如千钧。曹军粮草已竭，若搏命，大本营将空虚；若不搏，必死无疑。曹操背负双手踱步良久，目光扫向帐下众将。",
            backgroundImage = "bg_camp_night",
            leftCharacter = "曹操",
            rightCharacter = ""
        };

        DialogueOption option4A = new DialogueOption
        {
            optionText = "孤当亲自前往，以振军威！",
            nextNodeId = 4002,
            riskEffect = 1.0f
        };
        node4001.options.Add(option4A);

        DialogueOption option4B = new DialogueOption
        {
            optionText = "令徐晃、张辽等率精兵前往",
            nextNodeId = 4003
        };
        node4001.options.Add(option4B);

        loadedNodes[node4001.nodeId] = node4001;

        // 节点4002：亲自前往结果（纯文本）
        DialogueNode node4002 = new DialogueNode
        {
            nodeId = 4002,
            dialogueText = "\"此战乃存亡之秋，唯孤亲往，方能置之死地而后生！\"曹操留曹洪守大营，自领五千步骑，人衔枚马裹蹄，悄然穿过袁军防线。诈称蒋奇之军，竟得放行。抵达乌巢时，淳于琼宿醉未醒。",
            backgroundImage = "bg_march_night",
            leftCharacter = "曹操",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node4002.nodeId] = node4002;

        // 节点4003：派将前往结果（纯文本）
        DialogueNode node4003 = new DialogueNode
        {
            nodeId = 4003,
            dialogueText = "\"文远胆勇，公明通略，此前截烧韩猛粮车，颇有心得。今夜此任，非二位不可。\"曹操坐镇官渡以安军心。张辽、徐晃领命而去，借着夜色掩护，兵不血刃穿过防线。",
            backgroundImage = "bg_camp_night",
            leftCharacter = "曹操",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node4003.nodeId] = node4003;

        // 节点4004：分支选择（2选项）
        DialogueNode node4004 = new DialogueNode
        {
            nodeId = 4004,
            dialogueText = "大军已至乌巢外围。此时淳于琼防备疏松，但袁绍援军随时可能赶到。是轻装疾进速战速决，还是稳扎稳打确保退路？",
            backgroundImage = "bg_wuchao_outskirts",
            leftCharacter = "曹操",
            rightCharacter = ""
        };

        DialogueOption option4BranchA = new DialogueOption
        {
            optionText = "率五千精骑轻装速进",
            nextNodeId = 4005,
            troopEffect = 1.5f,
            riskEffect = 0.5f
        };
        node4004.options.Add(option4BranchA);

        DialogueOption option4BranchB = new DialogueOption
        {
            optionText = "率一万步骑稳扎稳打",
            nextNodeId = 4006,
            troopEffect = 1.0f
        };
        node4004.options.Add(option4BranchB);

        loadedNodes[node4004.nodeId] = node4004;

        // 节点4005：轻装结果（纯文本）
        DialogueNode node4005 = new DialogueNode
        {
            nodeId = 4005,
            dialogueText = "曹军如神兵天降，淳于琼宿醉未醒便遭猛攻。曹操亲自擂鼓，不顾箭雨厉喝：\"不及贼巢，皆为阶下囚！\"将士以一当十，终焚尽乌巢粮草，火光映红半边天际。然曹操身陷死地，亲卫死伤殆尽，损耗极大。",
            backgroundImage = "bg_wuchao_burning",
            leftCharacter = "曹操",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node4005.nodeId] = node4005;

        // 节点4006：稳扎稳打结果（纯文本）
        DialogueNode node4006 = new DialogueNode
        {
            nodeId = 4006,
            dialogueText = "张辽、徐晃配合默契，趁守军不备四面放火。淳于琼措手不及，袁军援军赶到时，二人已依计烧毁粮草，率部且战且退，阵型不乱，毫发无损撤回本阵。曹操坐镇官渡，从容调度，未给袁绍可乘之机。",
            backgroundImage = "bg_wuchao_burning",
            leftCharacter = "",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node4006.nodeId] = node4006;

        Debug.Log("剧情点4节点创建完成：4001-4006");

        // ========== 剧情点5：乌巢激战 ==========

        // 节点5001：乌巢激战选择（3选项）
        DialogueNode node5001 = new DialogueNode
        {
            nodeId = 5001,
            dialogueText = "火光在曹操瞳孔中跳跃，将乌巢粮囤映照成橘红色的地狱。就在此刻，快马传来冰寒消息：\"袁绍未救乌巢，反遣张郃、高览直扑我官渡大营！\"大营若失，全军将成无根浮萍。曹操勒马，三条道路摆在面前。",
            backgroundImage = "bg_wuchao_burning",
            leftCharacter = "曹操",
            rightCharacter = ""
        };

        DialogueOption option5A = new DialogueOption
        {
            optionText = "不予理会，全力攻占乌巢！",
            nextNodeId = 5002
        };
        node5001.options.Add(option5A);

        DialogueOption option5B = new DialogueOption
        {
            optionText = "派曹洪分兵回守大营",
            nextNodeId = 5003
        };
        node5001.options.Add(option5B);

        DialogueOption option5C = new DialogueOption
        {
            optionText = "趁其大营空虚，直取袁绍！",
            nextNodeId = 5005
        };
        node5001.options.Add(option5C);

        loadedNodes[node5001.nodeId] = node5001;

        // 节点5002：全力攻占过程（纯文本）
        DialogueNode node5002 = new DialogueNode
        {
            nodeId = 5002,
            dialogueText = "曹操拔剑直指淳于琼中军：\"传令全军：不予理会，全力攻占乌巢！焚尽此间粮草，则袁军百万之众，皆为齑粉！破敌就在今日，有进无退！\"许褚率虎卫弃马步战，张辽骑兵外围游弋。随着最后抵抗被粉碎，冲天火柱一道接一道腾起，连绵成望不到边的火海。",
            backgroundImage = "bg_wuchao_burning",
            leftCharacter = "曹操",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node5002.nodeId] = node5002;

        // 节点500201：史实胜利"查看结局"按钮页（选项节点）
        DialogueNode node500201 = new DialogueNode
        {
            nodeId = 500201,
            dialogueText = "乌巢冲天的火光，成为了压垮袁绍集团的最后一根稻草。张郃、高览临阵倒戈，袁绍大军士气崩盘。曹操趁势回师，袁绍仅率八百亲骑仓皇逃回河北。官渡之战，以曹操奇迹般的彻底胜利告终。",
            backgroundImage = "bg_victory",
            leftCharacter = "",
            rightCharacter = ""
        };
        DialogueOption optionViewEndingHistorical = new DialogueOption
        {
            optionText = "查看结局",
            nextNodeId = 500202
        };
        node500201.options.Add(optionViewEndingHistorical);
        loadedNodes[node500201.nodeId] = node500201;

        // 节点500202：史实胜利结局标题（纯文本）
        DialogueNode node500202 = new DialogueNode
        {
            nodeId = 500202,
            dialogueText = "═══════════════════════════════\n【结局：官渡大捷】\n火烧乌巢，袁军崩溃，曹操统一北方。\n═══════════════════════════════",
            backgroundImage = "bg_victory",
            leftCharacter = "",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node500202.nodeId] = node500202;

        // 节点5003：分兵回援过程（纯文本）
        DialogueNode node5003 = new DialogueNode
        {
            nodeId = 5003,
            dialogueText = "曹操咬牙下令：\"曹洪！予你两千人马，火速回援官渡！不惜一切代价，守住大营！\"命令既下，精锐被抽调离场。乌巢前线攻势为之一滞，淳于琼趁势收拢残兵。官渡方向，曹洪援军堪堪挡住张郃、高览猛攻，但营栅破损，士卒疲惫。",
            backgroundImage = "bg_wuchao_burning",
            leftCharacter = "曹操",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node5003.nodeId] = node5003;

        // 节点5004：分兵回援"查看结局"按钮页（选项节点）
        DialogueNode node5004 = new DialogueNode
        {
            nodeId = 5004,
            dialogueText = "数日后，曹操未能完全达成战略目标，被迫撤离。袁绍乌巢粮草损失惨重，但未至根本。曹操军队在两线消耗中流尽最后一滴血，撤退回官渡的路上，尽是面带菜色的伤兵。",
            backgroundImage = "bg_defeat",
            leftCharacter = "",
            rightCharacter = ""
        };
        DialogueOption optionViewEndingBranchB = new DialogueOption
        {
            optionText = "查看结局",
            nextNodeId = 5004  // 临时，实际由代码判定
        };
        node5004.options.Add(optionViewEndingBranchB);
        loadedNodes[node5004.nodeId] = node5004;

        // 节点500301：IF3结局标题（纯文本）
        DialogueNode node500301 = new DialogueNode
        {
            nodeId = 500301,
            dialogueText = "═══════════════════════════════\n【结局：元气大伤】\n兵力粮草耗尽，统一进程延迟数年。\n═══════════════════════════════",
            backgroundImage = "bg_defeat",
            leftCharacter = "",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node500301.nodeId] = node500301;

        // 节点500401：IF4结局标题（纯文本）
        DialogueNode node500401 = new DialogueNode
        {
            nodeId = 500401,
            dialogueText = "═══════════════════════════════\n【结局：以退为进】\n兵力粮草尚足，守住大营重振旗鼓。\n═══════════════════════════════",
            backgroundImage = "bg_defeat",
            leftCharacter = "",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node500401.nodeId] = node500401;

        // 节点5005：围魏救赵过程（纯文本）
        DialogueNode node5005 = new DialogueNode
        {
            nodeId = 5005,
            dialogueText = "曹操剑锋直指袁绍大营：\"全军听令！弃乌巢，弃官渡，随孤直取袁绍本阵！\"这支军队因之前激进的策略早已淬炼成利刃。借着夜色，如黑色洪流涌向袁绍中枢。袁绍正因乌巢火光焦头烂额，做梦也没想到曹操竟敢置两处死地于不顾。",
            backgroundImage = "bg_march_night",
            leftCharacter = "曹操",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node5005.nodeId] = node5005;

        // 节点5006：围魏救赵"查看结局"按钮页（选项节点）
        DialogueNode node5006 = new DialogueNode
        {
            nodeId = 5006,
            dialogueText = "当曹操的马蹄声在帐外响起时，袁绍身边亲卫甚至没来得及举起长矛。士兵如狼入羊群，瞬间撕裂防御。这一夜，河北四州的主帅被俘，袁军指挥系统彻底瘫痪。张郃、高览闻讯倒戈，淳于琼不战而降。",
            backgroundImage = "bg_victory",
            leftCharacter = "",
            rightCharacter = ""
        };
        DialogueOption optionViewEndingBranchC = new DialogueOption
        {
            optionText = "查看结局",
            nextNodeId = 5006  // 临时，实际由代码判定
        };
        node5006.options.Add(optionViewEndingBranchC);
        loadedNodes[node5006.nodeId] = node5006;

        // 节点500501：IF5完美结局标题（纯文本）
        DialogueNode node500501 = new DialogueNode
        {
            nodeId = 500501,
            dialogueText = "═══════════════════════════════\n【结局：完美】\n雷霆一击，提前数年统一北方。\n═══════════════════════════════",
            backgroundImage = "bg_victory",
            leftCharacter = "",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node500501.nodeId] = node500501;

        // 节点500601：IF6惨败结局标题（纯文本）
        DialogueNode node500601 = new DialogueNode
        {
            nodeId = 500601,
            dialogueText = "═══════════════════════════════\n【结局：惨败】\n透支国运，全军覆没，一代枭雄陨落。\n═══════════════════════════════",
            backgroundImage = "bg_defeat",
            leftCharacter = "",
            rightCharacter = "",
            nextNodeId = 0
        };
        loadedNodes[node500601.nodeId] = node500601;

        Debug.Log("剧情点5节点创建完成：5001-500601");

    }

    // 根据ID查找对话节点
    DialogueNode FindNodeById(int nodeId)
    {
        if (loadedNodes.ContainsKey(nodeId))
            return loadedNodes[nodeId];

        Debug.LogWarning($"找不到对话节点: {nodeId}");
        return null;
    }

    // 显示指定ID的对话节点
    public void ShowDialogueNode(int nodeId)
    {
        currentNode = FindNodeById(nodeId);

        if (currentNode == null)
        {
            Debug.LogError($"无法显示节点：{nodeId}");
            return;
        }
        IsShowing = true;
        // ===== 新增：触发事件 =====
        OnDialogueNodeShown?.Invoke(nodeId);
        // =========================

        // ========== 触发节点显示事件 ==========
        OnDialogueNodeShown?.Invoke(nodeId);
        // =====================================

        if (!string.IsNullOrEmpty(currentNode.dialogueText))
        {
            string preview = currentNode.dialogueText.Length > 30
                ? currentNode.dialogueText.Substring(0, 30) + "..."
                : currentNode.dialogueText;
            Debug.Log($"DialogueSystem：显示节点 {nodeId} - {preview}");
        }
        else
        {
            Debug.Log($"DialogueSystem：显示节点 {nodeId} - 无文本内容");
        }
    }

    // 玩家选择了某个选项
    public void HandleOptionSelected(int optionIndex)
    {
        if (currentNode == null || optionIndex >= currentNode.options.Count)
            return;

        DialogueOption selectedOption = currentNode.options[optionIndex];
        Debug.Log($"玩家选择了：{selectedOption.optionText}");
        // 检查条件（如果有）
        if (!string.IsNullOrEmpty(selectedOption.condition) && !CheckCondition(selectedOption.condition))
        {
            Debug.Log($"条件不满足: {selectedOption.condition}");
            return;
        }

        OnOptionSelected?.Invoke(optionIndex, selectedOption);
    }

        // 检查条件
        bool CheckCondition(string condition)
    {
        // 这里可以扩展复杂的条件检查逻辑
        // 目前只实现简单的数值检查

        if (condition.Contains(">"))
        {
            // 示例: "troop>5"
            var parts = condition.Split('>');
            if (parts.Length == 2)
            {
                string resourceType = parts[0].Trim();
                float requiredValue = float.Parse(parts[1].Trim());

                float currentValue = GetResourceValue(resourceType);
                return currentValue > requiredValue;
            }
        }

        // 默认通过
        return true;
    }

    // 获取资源当前值
    float GetResourceValue(string resourceType)
    {
        if (ResourceManager.Instance == null) return 0;

        switch (resourceType.ToLower())
        {
            case "troop": return ResourceManager.Instance.GetTroop();
            case "food": return ResourceManager.Instance.GetFood();
            case "strategy": return ResourceManager.Instance.GetStrategy();
            case "risk": return ResourceManager.Instance.GetRisk();
            default: return 0;
        }
    }

    // 应用资源效果
    void ApplyResourceEffects(DialogueOption option)
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogError("ResourceManager未初始化");
            return;
        }

        if (option.troopEffect != 0)
            ResourceManager.Instance.ModifyTroop(option.troopEffect);

        if (option.foodEffect != 0)
            ResourceManager.Instance.ModifyFood(option.foodEffect);

        if (option.strategyEffect != 0)
            ResourceManager.Instance.ModifyStrategy(option.strategyEffect);

        if (option.riskEffect != 0)
            ResourceManager.Instance.ModifyRisk(option.riskEffect);
    }

    // 开始小游戏
    void StartMiniGame(string gameType, int nextNodeId)
    {
        Debug.Log($"开始小游戏: {gameType}");

        // 这里实现小游戏逻辑
        // 目前先模拟小游戏完成，直接跳转
        OnMiniGameCompleted(true, nextNodeId);
    }

    // 小游戏完成回调
    void OnMiniGameCompleted(bool success, int nextNodeId)
    {
        if (success)
        {
            ShowDialogueNode(nextNodeId);
        }
        else
        {
            Debug.Log("小游戏失败");
            // 可以显示失败提示或重试
        }
    }

    // 重新加载数据（策划修改后调用）
    public void ReloadDialogueData()
    {
        LoadAllDialogueData();
        Debug.Log("对话数据已重新加载");

        // 重新显示当前节点
        if (currentNode != null)
            ShowDialogueNode(currentNode.nodeId);
    }

    // 调试：打印所有节点
    public void PrintAllNodes()
    {
        Debug.Log("=== 所有对话节点 ===");
        foreach (var kvp in loadedNodes)
        {
            Debug.Log($"节点 {kvp.Key}: {kvp.Value.dialogueText}");
            foreach (var option in kvp.Value.options)
            {
                Debug.Log($"  - {option.optionText} → {option.nextNodeId}");
            }
        }
    }

    // ========== 新增：UI显示/隐藏控制 ==========
    // 隐藏对话UI（当需要显示特殊界面时）
    public void HideDialogueUI()
    {
        IsShowing = false;
        Debug.Log("DialogueSystem：对话已隐藏");
    }

    // 显示对话UI
    public void ShowDialogueUI()
    {
        IsShowing = true;
        Debug.Log("DialogueSystem：对话已显示（由FinalUIManager处理UI）");
    }
    // ========================================

    // 获取当前是否应该显示对话UI
    public bool ShouldShowDialogueUI()
    {
        return IsShowing && currentNode != null;
    }

    // 获取当前节点文本（供其他UI显示）
    public string GetCurrentDialogueText()
    {
        return currentNode != null ? currentNode.dialogueText : "";
    }

    // 获取当前节点选项（供其他UI显示）
    public List<DialogueOption> GetCurrentOptions()
    {
        return currentNode != null ? currentNode.options : new List<DialogueOption>();
    }

    // ========== 新增：绑定按钮事件 ==========
    public void BindOptionButtons()
    {
        if (optionButtons == null)
        {
            Debug.LogWarning("DialogueSystem.BindOptionButtons: optionButtons为空");
            return;
        }

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // 捕获局部变量
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => HandleOptionSelected(index));
        }
        Debug.Log($"DialogueSystem: 已绑定 {optionButtons.Length} 个选项按钮事件");
    }
    // =======================================

    public void StartDialogue()
    {
        if (loadedNodes.Count > 0)
        {
            ShowDialogueNode(100001);
            Debug.Log("DialogueSystem: 开始对话，从节点1000（背景介绍）开始");
        }
        else
        {
            Debug.LogError("DialogueSystem: 没有加载对话数据，无法开始对话");
        }
    }
}