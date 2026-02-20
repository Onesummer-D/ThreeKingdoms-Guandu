using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FinalUIManager : MonoBehaviour
{
    // ========== UI 组件引用 ==========
    [Header("界面面板")]
    public GameObject startMenuPanel;
    public GameObject gameInterfacePanel;

    [Header("对话界面")]
    public TMP_Text dialogueText;

    [Header("选项按钮")]
    public Button optionButton1;
    public Button optionButton2;
    public Button optionButton3;
    public TMP_Text option1Text;
    public TMP_Text option2Text;
    public TMP_Text option3Text;

    [Header("资源显示")]
    public TMP_Text troopText;
    public TMP_Text foodText;
    public TMP_Text strategyText;
    public TMP_Text riskText;

    [Header("数值输入界面")]
    public GameObject valueInputPanel;
    public Slider valueSlider;
    public TMP_Text sliderValueText;
    public Button confirmValueButton;

    [Header("角色立绘")]
    public Image leftCharacterImage;
    public Image rightCharacterImage;
    public Sprite caocaoSprite;
    public Sprite yuanshaoSprite;
    public Sprite xuyouSprite;

    [Header("继续按钮")]
    public GameObject continueButtonPrefab;  
    public TMP_Text continueButtonText;

    [Header("结局返回按钮")]
    public GameObject endingReturnButtonPrefab;
    public TMP_Text endingReturnButtonText;

    // ========== 游戏状态 ==========
    private int currentPlotPoint = 1;
    private float ambushInputValue = 2.0f;
    private bool isInIfLine = false;
    private int currentNextNodeId = 0;  // 新增：存储下一个节点ID
    private bool waitingForContinue = false;


    // ========== 初始化 ==========
    void Start()
    {
        Debug.Log("🎮 FinalUIManager 启动");

        // 初始状态：显示开始菜单，隐藏游戏界面
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(false);

        // ========== 检查并连接DialogueSystem ==========
        if (DialogueSystem.Instance != null)
        {
            Debug.Log("✅ DialogueSystem已就绪");

            // 订阅事件
            DialogueSystem.Instance.OnOptionSelected += HandleDialogueOption;
            DialogueSystem.Instance.OnDialogueNodeShown += OnDialogueNodeShown;

            Debug.Log("已订阅DialogueSystem事件");
            // 绑定DialogueSystem的按钮事件
        }
        else
        {
            Debug.LogError("⚠️ DialogueSystem未初始化！");
        }
        // =============================================


        // ========== 新增：订阅PlotPointManager事件 ==========
        PlotPointManager.OnAmbushResultProcessed += OnReceiveAmbushResult;
        // ===================================================

        // 设置初始界面状态
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(false);
        if (valueInputPanel != null) valueInputPanel.SetActive(false);

        // 隐藏角色立绘
        if (leftCharacterImage != null) leftCharacterImage.gameObject.SetActive(false);
        if (rightCharacterImage != null) rightCharacterImage.gameObject.SetActive(false);

        // 设置按钮事件
        SetupButtonEvents();

        // 订阅资源事件
        SubscribeToResourceEvents();

        // 初始资源显示
        UpdateResourceDisplay();

        // 初始化GameData
        InitializeGameData();

        // 绑定继续按钮点击事件
        if (continueButtonPrefab != null)
        {
            Button btn = continueButtonPrefab.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(OnContinueButtonClicked);

            // 确保初始隐藏
            continueButtonPrefab.SetActive(false);
        }

        if (endingReturnButtonPrefab != null)
        {
            Button btn = endingReturnButtonPrefab.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(OnEndingReturnButtonClicked);

            // 初始隐藏
            endingReturnButtonPrefab.SetActive(false);
        }
    }

    void InitializeGameData()
    {
        if (GameData.Instance == null)
        {
            GameObject gameDataObj = new GameObject("GameData");
            gameDataObj.AddComponent<GameData>();
            Debug.Log("创建GameData实例");
        }
    }

    // ========== 开始游戏 ==========
    public void StartGame()
    {
        Debug.Log("开始游戏");

        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(true);

        // 重置游戏状态
        currentPlotPoint = 1;
        isInIfLine = false;

        // 重新订阅确保连接
        SubscribeToResourceEvents();

        // 重置资源
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetAllResources();
        }

        // 清空解锁记录
        if (GameData.Instance != null)
        {
            GameData.Instance.unlockedIfLines.Clear();
        }

        // 加载第一个剧情点
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue();
        }
        else
        {
            Debug.LogError("无法开始游戏：DialogueSystem未初始化");
            ShowPlotPoint1();  // 备用方案
        }
    }

    // ========== 剧情点1：初战受挫 ==========
    void ShowPlotPoint1()
    {
        currentPlotPoint = 1;
        isInIfLine = false;

        // 确保 gameInterfacePanel 激活
        if (gameInterfacePanel != null)
        {
            gameInterfacePanel.SetActive(true);
        }

        // 使用DialogueSystem显示对话（从1000开始）
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue();  // 这会调用ShowDialogueNode(1000)
        }
        else
        {
            Debug.LogError("无法开始游戏：DialogueSystem未初始化");
        }
    }

    // ========== 选项处理 ==========
    void SetupButtonEvents()
    {
        if (optionButton1 != null)
            optionButton1.onClick.AddListener(() => OnOptionClicked(0));

        if (optionButton2 != null)
            optionButton2.onClick.AddListener(() => OnOptionClicked(1));

        if (optionButton3 != null)
            optionButton3.onClick.AddListener(() => OnOptionClicked(2));

        if (confirmValueButton != null)
            confirmValueButton.onClick.AddListener(OnConfirmValueInput);

        if (valueSlider != null)
            valueSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    void OnOptionClicked(int optionIndex)
    {
        // ===== 强行测试：不管什么情况都切换 =====
        Debug.Log("=== 强行测试：尝试切换图片 ===");
        FindObjectOfType<ImageSwitcher>()?.SwitchTo("造发石车");
        // ======================================
        if (isInIfLine) return; // 如果在IF线结局中，禁用选项

        Debug.Log($"当前剧情点: {currentPlotPoint}, 选择选项: {optionIndex + 1}");

        if (currentPlotPoint == 1)
        {
            switch (optionIndex)
            {
                case 0: StartCoroutine(HandlePuzzleGame()); break;
                case 1: HandleDigTunnel(); break;
                case 2: ShowValueInputPanel(); break;
            }
        }
        else if (currentPlotPoint == 2)
        {
            switch (optionIndex)
            {
                case 0: HandleOption2A(); break;
                case 1: HandleOption2B(); break;
                case 2: HandleOption2C(); break;
            }
        }
        else if (currentPlotPoint == 3)
        {
            switch (optionIndex)
            {
                case 0: HandleOption3A(); break;
                case 1: HandleOption3B(); break;
                case 2: HandleOption3C(); break;
            }
        }
    }

    // ========== 选项A：拼图游戏 ==========
    IEnumerator HandlePuzzleGame(int nextNodeId = 0)
    {
        SetOptionsActive(false);

        // 显示FinalUIManager的对话文本
        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(true);
            dialogueText.text = "命令工匠连夜赶造发石车...";
        }

        yield return new WaitForSeconds(1.0f);

        // 模拟拼图游戏
        if (dialogueText != null)
            dialogueText.text = "拼装发石车组件中...";

        yield return new WaitForSeconds(1.5f);

        // 通知GameCallbacks拼图完成
        if (GameCallbacks.Instance != null)
        {
            GameCallbacks.Instance.OnPuzzleGameCompleted(true);
            Debug.Log("✅ 已通知剧情系统：拼图完成");
        }

        yield return new WaitForSeconds(0.5f);

        // ========== 修改：拼图完成后，由DialogueSystem接管 ==========
        // 不再手动控制跳转，而是等待DialogueSystem显示节点1002
        // 节点1002是纯文本节点，会显示"继续"按钮
        if (nextNodeId > 0 && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.ShowDialogueNode(nextNodeId);
        }
        // ============================================================
    }

    // ========== 选项B：挖地道 ==========
    void HandleDigTunnel()
    {
        SetOptionsActive(false);

        if (dialogueText != null)
            dialogueText.text = "派遣精锐士兵挖掘地道...";

        // 只有这里才修改资源！
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyFood(-1.0f);
        }

        StartCoroutine(ShowDigTunnelResult());
    }

    IEnumerator ShowDigTunnelResult()
    {
        yield return new WaitForSeconds(1.0f);

        if (dialogueText != null)
            dialogueText.text = "地道挖掘中...";

        yield return new WaitForSeconds(1.5f);

        if (dialogueText != null)
            dialogueText.text = "地道挖掘成功，但消耗了大量粮草。";

        yield return new WaitForSeconds(1.5f);

        ShowPlotPoint2();
    }

    // ========== 新增：统一的小游戏处理入口 ==========
    void HandleMiniGame(string gameType, int nextNodeId)
    {
        Debug.Log($"开始小游戏: {gameType}");

        // 隐藏DialogueSystem的UI
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.HideDialogueUI();

        // 显示FinalUIManager的特殊界面
        switch (gameType.ToLower())
        {
            case "puzzle":
                StartCoroutine(HandlePuzzleGame(nextNodeId));
                break;
            case "slider":
                ShowValueInputPanel(nextNodeId);
                break;
            default:
                Debug.LogWarning($"未知的小游戏类型: {gameType}");
                // 直接继续
                if (DialogueSystem.Instance != null)
                    DialogueSystem.Instance.ShowDialogueNode(nextNodeId);
                break;
        }
    }
    // ===============================================


    // ========== 选项C：埋伏 ==========
    void ShowValueInputPanel(int nextNodeId = 0)
    {
        SetOptionsActive(false);
        HideContinueButton();  // 确保隐藏继续按钮

        if (dialogueText != null)
            dialogueText.text = "需要设置多少兵力进行埋伏？\n(合理范围：1.0 - 3.0)";

        if (valueInputPanel != null)
        {
            valueInputPanel.SetActive(true);
            if (valueSlider != null)
            {
                valueSlider.minValue = 1.0f;
                valueSlider.maxValue = 5.0f;
                valueSlider.value = ambushInputValue;
                OnSliderValueChanged(ambushInputValue);
            }
        }
        // 存储nextNodeId，在确认输入后使用
        currentNextNodeId = nextNodeId;
    }

    void OnSliderValueChanged(float value)
    {
        ambushInputValue = value;
        if (sliderValueText != null)
            sliderValueText.text = $"埋伏兵力: {value:F1}";
    }

    void OnConfirmValueInput()
    {
        if (valueInputPanel != null)
            valueInputPanel.SetActive(false);

        if (GameCallbacks.Instance != null)
        {
            GameCallbacks.Instance.OnValueInputConfirmed(ambushInputValue);
            Debug.Log($"✅ 滑块数值 {ambushInputValue:F1} 已提交至剧情系统");
        }

        // 埋伏处理完成后，等待PlotPointManager的事件回调
        // 结果会在OnReceiveAmbushResult中处理
    }

    IEnumerator ShowAmbushResultWithText(float inputValue, string resultText)
    {
        // 第一步：显示派出兵力
        if (dialogueText != null)
            dialogueText.text = $"派出 {inputValue:F1} 千兵力设下埋伏...";

        yield return new WaitForSeconds(1.5f);

        // 第二步：显示结果文本
        if (dialogueText != null)
            dialogueText.text = resultText;

        yield return new WaitForSeconds(2.0f);

        // 第三步：继续剧情
        ShowPlotPoint2();
    }

    // ========== 剧情点2：粮草将尽 ==========
    void ShowPlotPoint2()
    {
        currentPlotPoint = 2;

        // 显示曹操立绘（左侧）
        ShowCharacter("曹操", true);

        if (dialogueText != null)
            dialogueText.text = "军粮告急，曹操心生退意，荀彧来信劝其坚守待变。如何决断？";

        SetupOptionButtons(
            "采纳文若之谏，坚守官渡，等待战机",
            "粮草乃根本，不如暂回许都，再图后举",
            "召集众将商议对策"
        );

        SetOptionsActive(true);
        UpdateResourceDisplay();

        Debug.Log("显示剧情点2");
    }

    void HandleOption2A()
    {
        Debug.Log("【剧情点2-选项A】坚守官渡，等待战机");

        SetOptionsActive(false);

        if (dialogueText != null)
            dialogueText.text = "采纳荀彧的建议，坚守官渡等待战机...";

        StartCoroutine(ShowPlotPoint2A_Result());
    }

    IEnumerator ShowPlotPoint2A_Result()
    {
        yield return new WaitForSeconds(1.0f);

        if (dialogueText != null)
            dialogueText.text = "曹操决定坚守，军心稍定。";

        yield return new WaitForSeconds(1.5f);

        // 切换到许攸视角
        ShowCharacter("", true); // 隐藏左侧立绘
        ShowCharacter("许攸", false);

        if (dialogueText != null)
            dialogueText.text = "【视角切换至许攸】\n袁绍刚愎自用，不纳忠言。许攸家人被审配收监，心中愤懑...";

        yield return new WaitForSeconds(2.5f);

        ShowPlotPoint3();
    }

    void HandleOption2B()
    {
        Debug.Log("【剧情点2-选项B】撤退回许都，触发if线1");

        SetOptionsActive(false);
        isInIfLine = true;

        // ========== 新增：调用你的结局系统 ==========
        if (EndingManager.Instance != null)
        {
            EndingManager.Instance.TriggerEnding("IF1");
        }
        // =========================================
    }

    IEnumerator ShowIfLine1Ending()
    {
        if (dialogueText != null)
            dialogueText.text = "决定撤退回许都...";

        yield return new WaitForSeconds(1.5f);

        // 显示袁绍立绘（右侧）
        ShowCharacter("", true);
        ShowCharacter("袁绍", false);

        if (dialogueText != null)
            dialogueText.text = "曹军撤退途中...";

        yield return new WaitForSeconds(1.5f);

        if (dialogueText != null)
            dialogueText.text = "袁绍得知曹军撤退，立即派骑兵追击！";

        yield return new WaitForSeconds(1.5f);

        if (dialogueText != null)
        {
            dialogueText.text = "撤退途中，曹军阵型散乱，袁绍趁机掩杀，曹军溃败。\n\n" +
                               "═══════════════════════════════\n" +
                               "【结局：半途而废】\n" +
                               "因粮草不济而撤退，错失战机，功败垂成。\n" +
                               "═══════════════════════════════";
        }

        // 记录解锁的结局
        if (GameData.Instance != null)
        {
            GameData.Instance.UnlockIfLine("IF_Line_1_半途而废");
            Debug.Log("已解锁IF线1：半途而废结局");
        }

        // 显示返回按钮
        yield return new WaitForSeconds(0.5f);
        ShowEndingReturnButton("半途而废");
    }

    void HandleOption2C()
    {
        Debug.Log("【剧情点2-选项C】召集众将商议");

        SetOptionsActive(false);

        if (dialogueText != null)
            dialogueText.text = "召集众将商议对策...";

        StartCoroutine(ShowPlotPoint2C_Result());
    }

    IEnumerator ShowPlotPoint2C_Result()
    {
        yield return new WaitForSeconds(1.5f);

        if (dialogueText != null)
            dialogueText.text = "众将意见不一，争议不休...";

        yield return new WaitForSeconds(1.5f);

        // 粮草-0.5（因为争议拖延）
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyFood(-0.5f);
        }

        if (dialogueText != null)
            dialogueText.text = "争议消耗了时间，粮草进一步减少。最终决定坚守。";

        yield return new WaitForSeconds(2.0f);

        // 切换到许攸视角
        ShowCharacter("", true);
        ShowCharacter("许攸", false);

        if (dialogueText != null)
            dialogueText.text = "【视角切换至许攸】\n袁绍营中，许攸献计被拒，心生去意...";

        yield return new WaitForSeconds(2.0f);

        ShowPlotPoint3();
    }

    // ========== 剧情点3：许攸夜访 ==========
    void ShowPlotPoint3()
    {
        currentPlotPoint = 3;

        // 显示曹操（左）和许攸（右）立绘
        ShowCharacter("曹操", true);
        ShowCharacter("许攸", false);

        if (dialogueText != null)
            dialogueText.text = "许攸夜访曹营，试探性地问：\"明公军中粮草尚可支几时？\"";

        SetupOptionButtons(
            "实不相瞒，粮草已尽，请公教我。",
            "尚可支撑一年......半年......三月耳。",
            "此乃诱敌之计，推出斩首！（怀疑其心）"
        );

        SetOptionsActive(true);
        UpdateResourceDisplay();

        Debug.Log("显示剧情点3");
    }

    void HandleOption3A()
    {
        Debug.Log("【剧情点3-选项A】实话实说，粮草已尽");

        SetOptionsActive(false);

        if (dialogueText != null)
            dialogueText.text = "曹操叹息道：\"粮草已尽，请公教我破敌之策。\"";

        StartCoroutine(ShowOption3A_Result());
    }

    IEnumerator ShowOption3A_Result()
    {
        yield return new WaitForSeconds(1.5f);

        if (dialogueText != null)
            dialogueText.text = "许攸感动于曹操的诚恳：\"明公如此坦诚，攸当效死力！\"";

        yield return new WaitForSeconds(1.5f);

        // 计策成功率+1
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyStrategy(1.0f);
        }

        if (dialogueText != null)
            dialogueText.text = "许攸献计：\"袁绍辎重尽在乌巢，守将淳于琼嗜酒无备。明公可轻兵往袭，烧其粮草，不过三日，袁军自乱！\"";

        yield return new WaitForSeconds(2.5f);

        // 显示第四个剧情点（框架）
        ShowPlotPoint4();
    }

    void HandleOption3B()
    {
        Debug.Log("【剧情点3-选项B】说谎，粮草尚可支撑");

        SetOptionsActive(false);

        if (dialogueText != null)
            dialogueText.text = "曹操笑道：\"尚可支撑一年......\"\n许攸摇头：\"未必吧？\"\n\"......半年？\"\n\"明公休要瞒我！\"\n\"......三月耳。\"";

        StartCoroutine(ShowOption3B_Result());
    }

    IEnumerator ShowOption3B_Result()
    {
        yield return new WaitForSeconds(2.0f);

        if (dialogueText != null)
            dialogueText.text = "许攸大笑：\"世人皆言孟德奸雄，今果然也！粮草已尽，何欺我耶？\"";

        yield return new WaitForSeconds(1.5f);

        // 计策成功率+0.5
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyStrategy(0.5f);
        }

        if (dialogueText != null)
            dialogueText.text = "许攸仍献计：\"既如此，攸仍献一策：乌巢守备松懈，可速击之！\"";

        yield return new WaitForSeconds(2.0f);

        ShowPlotPoint4();
    }

    void HandleOption3C()
    {
        Debug.Log("【剧情点3-选项C】怀疑许攸，推出斩首");

        SetOptionsActive(false);
        isInIfLine = true;

        // ========== 新增：调用你的结局系统 ==========
        if (EndingManager.Instance != null)
        {
            EndingManager.Instance.TriggerEnding("IF2");
        }
        // =========================================
    }

    IEnumerator ShowIfLine2Ending()
    {
        if (dialogueText != null)
            dialogueText.text = "曹操拍案而起：\"许攸乃袁绍故吏，此必诈降诱我！推出斩首！\"";

        yield return new WaitForSeconds(1.5f);

        // 显示许攸立绘
        ShowCharacter("", true);
        ShowCharacter("许攸", false);

        if (dialogueText != null)
            dialogueText.text = "许攸仰天长叹：\"曹孟德，你疑心太重，必失天下！\"";

        yield return new WaitForSeconds(1.5f);

        if (dialogueText != null)
            dialogueText.text = "许攸被斩，曹营无人知晓乌巢虚实...";

        yield return new WaitForSeconds(1.5f);

        // 显示袁绍立绘
        ShowCharacter("袁绍", false);

        if (dialogueText != null)
        {
            dialogueText.text = "一月后，曹军粮尽，士卒逃亡。袁绍大军压境...\n\n" +
                               "═══════════════════════════════\n" +
                               "【结局：错失良机】\n" +
                               "因猜忌错杀来投谋士，失去关键情报，最终粮尽兵败。\n" +
                               "═══════════════════════════════";
        }

        // 记录解锁的结局
        if (GameData.Instance != null)
        {
            GameData.Instance.UnlockIfLine("IF_Line_2_错失良机");
            Debug.Log("已解锁IF线2：错失良机结局");
        }

        // 显示返回按钮
        yield return new WaitForSeconds(0.5f);
        ShowEndingReturnButton("错失良机");
    }

    // ========== 剧情点4：框架（为第三周准备） ==========
    void ShowPlotPoint4()
    {
        currentPlotPoint = 4;

        // 显示曹操立绘
        ShowCharacter("曹操", true);
        ShowCharacter("", false);

        if (dialogueText != null)
            dialogueText.text = "采纳许攸之计，决定奇袭乌巢！如何部署？\n\n（第四剧情点 - 第三周开发）";

        SetupOptionButtons(
            "孤当亲自前往，以振军威！",
            "令徐晃、张辽率精兵前往",
            "待开发选项"
        );

        SetOptionsActive(true);
        UpdateResourceDisplay();

        Debug.Log("显示剧情点4（框架）");
    }

    // ========== 角色立绘管理 ==========
    void ShowCharacter(string characterName, bool isLeft)
    {
        Image targetImage = isLeft ? leftCharacterImage : rightCharacterImage;

        if (targetImage == null)
        {
            Debug.LogWarning($"找不到{(isLeft ? "左侧" : "右侧")}角色图像组件");
            return;
        }

        if (string.IsNullOrEmpty(characterName))
        {
            targetImage.gameObject.SetActive(false);
            return;
        }

        Sprite sprite = GetCharacterSprite(characterName);
        if (sprite != null)
        {
            targetImage.sprite = sprite;
            targetImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"找不到角色立绘: {characterName}");
            targetImage.gameObject.SetActive(false);
        }
    }

    Sprite GetCharacterSprite(string name)
    {
        switch (name)
        {
            case "曹操":
                return caocaoSprite;
            case "袁绍":
                return yuanshaoSprite;
            case "许攸":
                return xuyouSprite;
            default:
                return null;
        }
    }

    // ========== 结局系统 ==========
    void ShowEndingReturnButton(string endingName)
    {
        if (endingReturnButtonPrefab != null)
        {
            endingReturnButtonPrefab.SetActive(true);
            if (endingReturnButtonText != null)
                endingReturnButtonText.text = "返回主菜单";
        }
        HideContinueButton();
    }

    private UnityEngine.Events.UnityAction currentContinueAction;

    private void ShowContinueButton(string buttonText, UnityEngine.Events.UnityAction action=null)
    {
        Debug.Log($"ShowContinueButton被调用，按钮文本：{buttonText}");

        // 检查引用
        if (continueButtonPrefab == null)
        {
            Debug.LogError("continueButtonPrefab为null！请在Inspector中拖入ContinueButton");
            return;
        }
        if (continueButtonText == null)
        {
            Debug.LogError("continueButtonText为null！请在Inspector中拖入Text(TMP)");
            return;
        }

        // 存储回调
        currentContinueAction = action;

        // 如果没有传入action，使用默认的OnContinueButtonClicked
        if (currentContinueAction == null)
        {
            currentContinueAction = OnContinueButtonClicked;
        }

        // 设置文本
        continueButtonText.text = buttonText;

        // 设置字体
        TMP_FontAsset chineseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMHEI SDF");
        if (chineseFont != null)
            continueButtonText.font = chineseFont;

        // 显示按钮
        continueButtonPrefab.SetActive(true);
        Debug.Log($"按钮已激活：{continueButtonPrefab.activeSelf}");

        // 隐藏选项
        SetOptionsActive(false);
    }

    // 继续按钮被点击时调用
    public void OnContinueButtonClicked()
    {
        Debug.Log("=== 继续按钮被点击，切换图片 ===");
        FindObjectOfType<ImageSwitcher>()?.SwitchTo("荀彧来信");
        if (DialogueSystem.Instance == null || DialogueSystem.Instance.CurrentNode == null)
        {
            Debug.LogError("无法处理继续按钮：DialogueSystem未就绪");
            return;
        }

        int currentNodeId = DialogueSystem.Instance.CurrentNode.nodeId;
        Debug.Log($"继续按钮被点击，当前节点：{currentNodeId}");

        HideContinueButton();
        waitingForContinue = false;

        // 根据当前节点决定跳转到哪里
        switch (currentNodeId)
        {
            case 100001:
                DialogueSystem.Instance.ShowDialogueNode(100002);
                break;

            case 100002:
                DialogueSystem.Instance.ShowDialogueNode(1001);
                break;

            case 1002:
            case 1003:
            case 1005:
            case 1006:
                DialogueSystem.Instance.ShowDialogueNode(200101);
                break;

            case 200101:
                DialogueSystem.Instance.ShowDialogueNode(200102);
                break;

            case 200102:
                Debug.LogWarning("200102是选项节点，不应该点击继续按钮");
                if (DialogueSystem.Instance.CurrentNode.options.Count > 0)
                {
                    SetupOptionButtonsFromDialogue(DialogueSystem.Instance.CurrentNode.options);
                }
                break;

            case 2002:
            case 2003:
                DialogueSystem.Instance.ShowDialogueNode(3001);
                break;

            case 3001:
                DialogueSystem.Instance.ShowDialogueNode(3002);
                break;

            case 3003:
            case 3004:
                DialogueSystem.Instance.ShowDialogueNode(4001);
                break;

            case 9001:
                DialogueSystem.Instance.ShowDialogueNode(9002);
                break;

            case 9002:
                DialogueSystem.Instance.ShowDialogueNode(900201);
                break;

            case 9003:
                DialogueSystem.Instance.ShowDialogueNode(9004);
                break;

            case 9004:
                Debug.LogWarning("9004是选项节点，不应该点击继续按钮");
                if (DialogueSystem.Instance.CurrentNode.options.Count > 0)
                {
                    SetupOptionButtonsFromDialogue(DialogueSystem.Instance.CurrentNode.options);
                }
                break;

            // ========== 剧情点4：奇袭乌巢 ==========
            case 4002:  // 亲自前往结果 → 分支选择
            case 4003:  // 派将前往结果 → 分支选择
                DialogueSystem.Instance.ShowDialogueNode(4004);
                break;

            case 4005:  // 轻装结果 → 剧情点5
            case 4006:  // 稳扎稳打结果 → 剧情点5
                DialogueSystem.Instance.ShowDialogueNode(5001);
                break;

            // ========== 剧情点5：乌巢激战 ==========
            case 5002:  // 全力攻占过程 → "查看结局"按钮页
                DialogueSystem.Instance.ShowDialogueNode(500201);
                break;

            case 5003:  // 分兵回援过程 → "查看结局"按钮页
                DialogueSystem.Instance.ShowDialogueNode(5004);
                break;

            case 5005:  // 围魏救赵过程 → "查看结局"按钮页
                DialogueSystem.Instance.ShowDialogueNode(5006);
                break;

            case 500202:
            case 500301:  // IF3
            case 500401:  // IF4
            case 500501:  // IF5 
            case 500601:  // IF6 
            case 900201:// IF1
            case 900403:  // IF2
                ReturnToMainMenu();
                break;

            default:
                // ✅ 新增：如果节点有nextNodeId，优先使用它
                if (DialogueSystem.Instance.CurrentNode.nextNodeId > 0)
                {
                    DialogueSystem.Instance.ShowDialogueNode(DialogueSystem.Instance.CurrentNode.nextNodeId);
                }
                else
                {
                    Debug.LogWarning($"节点{currentNodeId}的继续跳转未配置");
                }
                break;
        }
    }
    void OnEndingReturnButtonClicked()
    {
        // 返回主菜单
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(false);

        // 隐藏返回按钮
        if (endingReturnButtonPrefab != null)
            endingReturnButtonPrefab.SetActive(false);

        isInIfLine = false;

        // ===== 重置到初始状态 =====
        // 关掉所有其他背景
        GameObject[] allBgs = {
        GameObject.Find("chaoting"),
        GameObject.Find("战斗时背景图"),
        GameObject.Find("失败场景背景"),
        GameObject.Find("donghanm")
    };
        foreach (GameObject bg in allBgs)
        {
            if (bg != null) bg.SetActive(false);
        }

        // 打开战场背景
        GameObject battleBg = GameObject.Find("zhanshibe");
        if (battleBg != null) battleBg.SetActive(true);

        // 隐藏人物
        if (leftCharacterImage != null) leftCharacterImage.gameObject.SetActive(false);
        if (rightCharacterImage != null) rightCharacterImage.gameObject.SetActive(false);
        // ===========================

        Debug.Log("返回主菜单");
    }

    // 提取返回主菜单逻辑
    private void ReturnToMainMenu()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(false);
        if (leftCharacterImage != null) leftCharacterImage.gameObject.SetActive(false);
        if (rightCharacterImage != null) rightCharacterImage.gameObject.SetActive(false);
        isInIfLine = false;
    }

    private void HideContinueButton()
    {
        if (continueButtonPrefab != null)
            continueButtonPrefab.SetActive(false);
    }

    // ========== 新增：DialogueSystem选项回调 ==========
    private void HandleDialogueOption(int optionIndex, DialogueOption option)
    {
        Debug.Log($"FinalUIManager：处理选项 {optionIndex}, 文本: {option.optionText}");

        // 应用资源效果
        if (ResourceManager.Instance != null)
        {
            if (option.troopEffect != 0) ResourceManager.Instance.ModifyTroop(option.troopEffect);
            if (option.foodEffect != 0) ResourceManager.Instance.ModifyFood(option.foodEffect);
            if (option.strategyEffect != 0) ResourceManager.Instance.ModifyStrategy(option.strategyEffect);
            if (option.riskEffect != 0) ResourceManager.Instance.ModifyRisk(option.riskEffect);
        }

        // 处理剧情点5的选项（5001节点的3个选项）
        if (DialogueSystem.Instance.CurrentNode.nodeId == 5001)
        {
            switch (optionIndex)
            {
                case 0:  // 选项A：全力攻占乌巢
                    DialogueSystem.Instance.ShowDialogueNode(5002);
                    return;

                case 1:  // 选项B：分兵回守大营
                    DialogueSystem.Instance.ShowDialogueNode(5003);
                    return;

                case 2:  // 选项C：直取袁绍大营（围魏救赵）
                    DialogueSystem.Instance.ShowDialogueNode(5005);
                    return;
            }
        }

        // 处理500201节点的"查看结局"按钮（史实胜利）
        if (DialogueSystem.Instance.CurrentNode.nodeId == 500201)
        {
            DialogueSystem.Instance.ShowDialogueNode(500202);
            return;
        }

        // 处理5004节点的"查看结局"按钮（分兵回援）
        if (DialogueSystem.Instance.CurrentNode.nodeId == 5004)
        {
            HandlePlotPoint5_Branch_Result();  // 调用判定方法
            return;
        }

        // 处理5006节点的"查看结局"按钮（围魏救赵）
        if (DialogueSystem.Instance.CurrentNode.nodeId == 5006)
        {
            HandlePlotPoint5_Perfect_Branch();  // 调用判定方法
            return;
        }

        // 处理小游戏类型
        if (!string.IsNullOrEmpty(option.miniGameType))
        {
            HandleMiniGame(option.miniGameType, option.nextNodeId);
        }
        else if (option.nextNodeId > 0)
        {
            // 检查是否是IF线结局节点
            if (option.nextNodeId >= 9000 && option.nextNodeId < 10000)
            {
                // IF线特殊处理
                StartCoroutine(PlayIfLineSequence(option.nextNodeId));
            }
            else
            {
                // 普通选项，继续显示下一个节点
                if (DialogueSystem.Instance != null)
                {
                    DialogueSystem.Instance.ShowDialogueNode(option.nextNodeId);
                }
            }
        }
        else
        {
            Debug.Log($"选项没有指定下一个节点");
        }
    }

    // IF线播放序列
    IEnumerator PlayIfLineSequence(int startNodeId)
    {
        // 先显示IF线第一个节点
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.ShowDialogueNode(startNodeId);
        }
        yield return null;  // 等待一帧，让UI更新
    }

    // ========== DialogueSystem事件处理模块 ==========

    // 当DialogueSystem显示节点时
    private void OnDialogueNodeShown(int nodeId)
    {
        // ===== 新增：根据节点ID自动切换背景人物 =====
        SwitchImageByNodeId(nodeId);
        // ==========================================

        Debug.Log($"FinalUIManager: 显示节点 {nodeId}");

        if (gameInterfacePanel != null && !gameInterfacePanel.activeSelf)
            gameInterfacePanel.SetActive(true);

        SyncDialogueToFinalUI();
        UpdateResourceDisplay();
        UpdateCharacterDisplay();
        UpdateCurrentPlotPoint(nodeId);

        if (DialogueSystem.Instance == null || DialogueSystem.Instance.CurrentNode == null)
            return;

        var node = DialogueSystem.Instance.CurrentNode;

        bool isIFLineEnding = (nodeId == 900201 || nodeId == 900403);

        // 剧情点5的所有结局节点
        bool isPlotPoint5Ending = (nodeId == 500202 || nodeId == 500301 ||
                                   nodeId == 500401 || nodeId == 500501 || nodeId == 500601);

        // 文本兜底：如果节点文本包含"【结局："，强制视为结局节点
        bool isTextEnding = node.dialogueText != null && node.dialogueText.Contains("【结局：");

        if (isIFLineEnding || isPlotPoint5Ending || isTextEnding)
        {
            Debug.Log($"结局节点{nodeId}，显示返回主菜单按钮");
            SetOptionsActive(false);
            HideContinueButton();
            ShowEndingReturnButton("结局");
            return;  // 直接返回，不再执行后续逻辑
        }

        // 普通节点：确保返回主菜单按钮是隐藏的
        if (endingReturnButtonPrefab != null)
            endingReturnButtonPrefab.SetActive(false);

        // 关键判断：纯文本节点（options为空或数量为0）
        if (node.options == null || node.options.Count == 0)
        {
            // 纯文本节点：显示继续按钮，隐藏所有选项
            Debug.Log($"节点{nodeId}是纯文本节点，显示继续按钮");
            SetOptionsActive(false);  // 隐藏所有选项按钮
            HideContinueButton();
            ShowContinueButton("继续", () => OnContinueButtonClicked());
            waitingForContinue = true;
        }
        else
        {
            // 选项节点：显示选项，隐藏继续按钮
            Debug.Log($"节点{nodeId}是选项节点，显示{node.options.Count}个选项");
            HideContinueButton();
            SetupOptionButtonsFromDialogue(node.options);
            // 只显示需要的选项数量
            if (optionButton1 != null) optionButton1.gameObject.SetActive(node.options.Count >= 1);
            if (optionButton2 != null) optionButton2.gameObject.SetActive(node.options.Count >= 2);
            if (optionButton3 != null) optionButton3.gameObject.SetActive(node.options.Count >= 3);
            waitingForContinue = false;
        }
    }

    // 根据节点ID判断当前剧情点
    void UpdateCurrentPlotPoint(int nodeId)
    {
        // 剧情点1：初战受挫
        if (nodeId == 1001)  // 主节点
            currentPlotPoint = 1;
        else if (nodeId >= 1002 && nodeId <= 1006)  // 分支节点
            currentPlotPoint = 1;

        // 剧情点2：粮草将尽
        else if (nodeId == 200101 || nodeId == 200102 || nodeId == 2002 || nodeId == 2003)
            currentPlotPoint = 2;

        // IF线1：半途而废（9001-9002）
        else if (nodeId >= 9001 && nodeId <= 9002)
            currentPlotPoint = 2;  // 或者设为99表示结局

        // 剧情点3：许攸夜访
        else if (nodeId >= 3001 && nodeId <= 3004)
            currentPlotPoint = 3;

        // IF线2：错失良机（9003-900402）
        else if (nodeId >= 9003 && nodeId <= 900402)
            currentPlotPoint = 3;  // 或者设为99表示结局

        // 剧情点4：奇袭乌巢（假设从4001开始）
        else if (nodeId >= 4001 && nodeId <= 4006)
            currentPlotPoint = 4;

        // 剧情点5：乌巢激战（假设从5001开始）
        else if (nodeId >= 5001 && nodeId <= 5006)
            currentPlotPoint = 5;

        else
        {
            Debug.LogWarning($"未知的节点ID: {nodeId}, 无法确定剧情点");
        }
    }

    // 同步对话内容到FinalUI
    private void SyncDialogueToFinalUI()
    {
        if (DialogueSystem.Instance == null || !DialogueSystem.Instance.IsShowing)
        {
            // DialogueSystem没有显示，使用FinalUIManager自己的UI
            if (dialogueText != null)
                dialogueText.gameObject.SetActive(true);
            return;
        }

        // DialogueSystem正在显示，同步内容
        if (dialogueText != null && DialogueSystem.Instance.CurrentNode != null)
        {
            dialogueText.text = DialogueSystem.Instance.CurrentNode.dialogueText;
            dialogueText.gameObject.SetActive(true);

            TMP_FontAsset chineseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMHEI SDF");
            if (chineseFont != null)
                dialogueText.font = chineseFont;
        }
    }

    // 从DialogueSystem设置选项按钮
    private void SetupOptionButtonsFromDialogue(List<DialogueOption> options)
    {
        // 先全部隐藏
        if (optionButton1 != null) optionButton1.gameObject.SetActive(false);
        if (optionButton2 != null) optionButton2.gameObject.SetActive(false);
        if (optionButton3 != null) optionButton3.gameObject.SetActive(false);

        // 再根据需要显示
        if (options.Count >= 1 && option1Text != null && optionButton1 != null)
        {
            option1Text.text = options[0].optionText;
            option1Text.alignment = TextAlignmentOptions.Center;  // 强制居中
            optionButton1.onClick.RemoveAllListeners();
            optionButton1.onClick.AddListener(() => OnDialogueOptionClicked(0));
            optionButton1.gameObject.SetActive(true);  // 显示
        }

        if (options.Count >= 2 && option2Text != null && optionButton2 != null)
        {
            option2Text.text = options[1].optionText;
            option2Text.alignment = TextAlignmentOptions.Center;  // 强制居中
            optionButton2.onClick.RemoveAllListeners();
            optionButton2.onClick.AddListener(() => OnDialogueOptionClicked(1));
            optionButton2.gameObject.SetActive(true);  // 显示
        }

        if (options.Count >= 3 && option3Text != null && optionButton3 != null)
        {
            option3Text.text = options[2].optionText;
            option3Text.alignment = TextAlignmentOptions.Center;  // 强制居中

            // 强制刷新
            option3Text.ForceMeshUpdate();

            // 检查 RectTransform
            var rect = option3Text.GetComponent<RectTransform>();
            Debug.Log($"选项3宽度: {rect.rect.width}, 锚点: {rect.anchorMin} {rect.anchorMax}");

            optionButton3.onClick.RemoveAllListeners();
            optionButton3.onClick.AddListener(() => OnDialogueOptionClicked(2));
            optionButton3.gameObject.SetActive(true);  // 显示
            Debug.Log($"选项C文本：{options[2].optionText}");  // 调试
        }
    }

    // 当玩家点击FinalUIManager的选项按钮时
    private void OnDialogueOptionClicked(int optionIndex)
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.CurrentNode != null)
        {
            // 通知DialogueSystem选项被选择
            Debug.Log($"FinalUIManager: 玩家点击了选项 {optionIndex}");

            // 注意：这里不能直接调用DialogueSystem的内部方法
            // 应该通过事件机制，或者我们让HandleDialogueOption处理

            // 临时方案：直接调用HandleDialogueOption
            var options = DialogueSystem.Instance.GetCurrentOptions();
            if (optionIndex < options.Count)
            {
                HandleDialogueOption(optionIndex, options[optionIndex]);
            }
        }
    }

    // 更新角色显示
    private void UpdateCharacterDisplay()
    {
        if (DialogueSystem.Instance == null || DialogueSystem.Instance.CurrentNode == null) return;

        var node = DialogueSystem.Instance.CurrentNode;

        // 显示左侧角色
        if (!string.IsNullOrEmpty(node.leftCharacter))
            ShowCharacter(node.leftCharacter, true);
        else
            ShowCharacter("", true);

        // 显示右侧角色
        if (!string.IsNullOrEmpty(node.rightCharacter))
            ShowCharacter(node.rightCharacter, false);
        else
            ShowCharacter("", false);
    }

    // ========== 新增：供PlotPointManager调用的继续方法 ==========
    public void ContinueToPlotPoint2()
    {
        StartCoroutine(ContinueToPlotPoint2Coroutine());
    }

    private IEnumerator ContinueToPlotPoint2Coroutine()
    {
        // 清理可能存在的继续按钮
        GameObject existingBtn = GameObject.Find("ContinueButton");
        if (existingBtn != null)
        {
            Destroy(existingBtn);
        }

        if (dialogueText != null)
            dialogueText.text = "发石车组装完成！成功压制袁军箭楼，士气大振。\n兵力+1！";

        yield return new WaitForSeconds(1.5f);

        // 继续到剧情点2
        ShowPlotPoint2();
    }

    // ========== 工具方法 ==========
    void SetupOptionButtons(string text1, string text2, string text3)
    {
        if (option1Text != null) option1Text.text = text1;
        if (option2Text != null) option2Text.text = text2;
        if (option3Text != null) option3Text.text = text3;
    }

    void SetOptionsActive(bool active)
    {
        if (optionButton1 != null) optionButton1.gameObject.SetActive(active);
        if (optionButton2 != null) optionButton2.gameObject.SetActive(active);
        if (optionButton3 != null) optionButton3.gameObject.SetActive(active);

        // 隐藏数值输入面板
        if (!active && valueInputPanel != null)
        {
            valueInputPanel.SetActive(false);
        }
    }

    // ========== 资源系统 ==========
    void SubscribeToResourceEvents()
    {
        if (ResourceManager.Instance != null)
        {
            // 先取消订阅，避免重复
            ResourceManager.OnResourceChanged -= OnResourceChanged;
            // 重新订阅
            ResourceManager.OnResourceChanged += OnResourceChanged;
            Debug.Log("已订阅资源变化事件");
        }
        else
        {
            Debug.LogWarning("ResourceManager实例为空，无法订阅事件");
        }
    }

    void OnResourceChanged(ResourceChangeEvent changeEvent)
    {
        UpdateResourceDisplay();
        Debug.Log($"资源变化: {changeEvent.resourceType}, 新值: {changeEvent.newValue:F1}");
    }

    void UpdateResourceDisplay()
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning("ResourceManager为空，无法更新显示");
            return;
        }

        if (troopText != null)
            troopText.text = $"兵力: {ResourceManager.Instance.GetTroop():F1}";

        if (foodText != null)
            foodText.text = $"粮草: {ResourceManager.Instance.GetFood():F1}";

        if (strategyText != null)
            strategyText.text = $"计策: {ResourceManager.Instance.GetStrategy():F1}";

        if (riskText != null)
            riskText.text = $"风险: {ResourceManager.Instance.GetRisk():F1}";
    }

    // ========== 新增：接收埋伏结果事件的方法 ==========
    private void OnReceiveAmbushResult(bool isSuccess, string resultText, float inputValue)
    {
        Debug.Log($"收到埋伏结果：{(isSuccess ? "成功" : "失败")}, 值：{inputValue}");

        // 直接由DialogueSystem显示结果节点
    if (DialogueSystem.Instance != null)
    {
        int resultNodeId = isSuccess ? 1005 : 1006;
        DialogueSystem.Instance.ShowDialogueNode(resultNodeId);
    }
    }

    // ========== 清理 ==========
    void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.OnResourceChanged -= OnResourceChanged;
        }

        // ========== 新增：取消订阅PlotPointManager事件 ==========
        PlotPointManager.OnAmbushResultProcessed -= OnReceiveAmbushResult;
        // =======================================================
    }

    // ========== 剧情点5分支判定：分兵回援 ==========
    private void HandlePlotPoint5_Branch_Result()
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogError("ResourceManager未初始化");
            DialogueSystem.Instance.ShowDialogueNode(500301);  // 默认IF3
            return;
        }

        float troop = ResourceManager.Instance.GetTroop();
        float food = ResourceManager.Instance.GetFood();

        if (troop < 5.0f || food < 5.0f)
        {
            Debug.Log($"分兵回援判定：兵力{troop}，粮草{food} → IF3元气大伤");
            DialogueSystem.Instance.ShowDialogueNode(500301);
        }
        else
        {
            Debug.Log($"分兵回援判定：兵力{troop}，粮草{food} → IF4以退为进");
            DialogueSystem.Instance.ShowDialogueNode(500401);
        }
    }

    // ========== 剧情点5分支判定：围魏救赵 ==========
    private void HandlePlotPoint5_Perfect_Branch()
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogError("ResourceManager未初始化");
            DialogueSystem.Instance.ShowDialogueNode(500601);  // 默认IF6
            return;
        }

        float risk = ResourceManager.Instance.GetRisk();
        float troop = ResourceManager.Instance.GetTroop();
        float food = ResourceManager.Instance.GetFood();

        bool isPerfect = (risk > 7.0f && troop > 10.0f && food > 10.0f);

        if (isPerfect)
        {
            Debug.Log($"围魏救赵判定：风险{risk}，兵力{troop}，粮草{food} → IF5完美结局");
            DialogueSystem.Instance.ShowDialogueNode(500501);
        }
        else
        {
            Debug.Log($"围魏救赵判定：风险{risk}，兵力{troop}，粮草{food} → IF6惨败结局");
            DialogueSystem.Instance.ShowDialogueNode(500601);
        }
    }// ========== 新增：根据节点ID切换背景人物 ==========
    private void SwitchImageByNodeId(int nodeId)
    {
        ImageSwitcher switcher = FindObjectOfType<ImageSwitcher>();
        if (switcher == null)
        {
            Debug.LogError("找不到 ImageSwitcher！");
            return;
        }

        string sceneName = "";

        // 根据节点ID判断应该显示什么场景
        if (nodeId >= 1000 && nodeId < 2000)
        {
            // 剧情点1：初战受挫
            if (nodeId == 1001 || nodeId == 1002 || nodeId == 1005 || nodeId == 1006)
                sceneName = "造发石车";
            else if (nodeId == 1003)
                sceneName = "挖地道";
            else if (nodeId == 1004)
                sceneName = "佯装败退";
            else
                sceneName = "造发石车";
        }
        else if (nodeId >= 2000 && nodeId < 3000)
        {
            // 剧情点2：荀彧来信
            if (nodeId == 200101 || nodeId == 200102 || nodeId == 2002 || nodeId == 2003)
                sceneName = "荀彧来信";
        }
        else if (nodeId >= 3000 && nodeId < 4000)
        {
            // 剧情点3：许攸夜访
            if (nodeId == 3001 || nodeId == 3002 || nodeId == 3003 || nodeId == 3004)
                sceneName = "许攸夜访";
        }
        else if (nodeId >= 4000 && nodeId < 5000)
        {
            // 剧情点4：奇袭乌巢
            if (nodeId == 4001 || nodeId == 4002 || nodeId == 4003 || nodeId == 4004)
                sceneName = "奇袭乌巢";
            else if (nodeId == 4005 || nodeId == 4006)
                sceneName = "乌巢激战";
        }
        else if (nodeId >= 5000 && nodeId < 6000)
        {
            // 剧情点5：乌巢激战
            if (nodeId == 5001 || nodeId == 5002 || nodeId == 5003 || nodeId == 5005)
                sceneName = "乌巢激战";
            else if (nodeId == 500201 || nodeId == 500202)
                sceneName = "胜利结局";
            else if (nodeId == 500301 || nodeId == 500401 || nodeId == 500601)
                sceneName = "失败结局";
        }
        else if (nodeId >= 9000)
        {
            // IF线结局
            if (nodeId == 9001 || nodeId == 9002 || nodeId == 900201)
                sceneName = "半途而废";
            else if (nodeId == 9003 || nodeId == 9004 || nodeId == 900403)
                sceneName = "错失良机";
        }

        // 如果找到了场景名，就切换
        if (!string.IsNullOrEmpty(sceneName))
        {
            switcher.SwitchTo(sceneName);
        }
    }
    // ================================================
}