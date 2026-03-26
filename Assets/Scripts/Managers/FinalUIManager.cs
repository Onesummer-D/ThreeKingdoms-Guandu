using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FinalUIManager : MonoBehaviour
{
    [Header("界面面板")]
    public GameObject startMenuPanel;
    public GameObject gameInterfacePanel;
    public GameObject backgroundIntroPanel;

    [Header("背景介绍面板")]
    public Image backgroundIntroImage;
    public TMP_Text backgroundIntroText;
    public Button backgroundIntroClickArea;

    [Header("游戏界面 - 背景")]
    public Image backgroundImage;
    public RectTransform backgroundRectTransform; // 新增：直接引用RectTransform

    [Header("游戏界面 - 人物")]
    public Image avatarFrameImage;
    public Image avatarImage;
    public Image leftCharacterImage;
    public Image rightCharacterImage;

    // 新增：缓存RectTransform避免重复GetComponent
    private RectTransform leftCharRect;
    private RectTransform rightCharRect;
    private RectTransform avatarRect;

    [Header("游戏界面 - 对话")]
    public TMP_Text dialogueText;
    public Button clickAreaButton;
    public ClickHintAnimator clickHint;

    [Header("游戏界面 - 选项按钮")]
    public Button optionButton1;
    public Button optionButton2;
    public Button optionButton3;
    public TMP_Text option1Text;
    public TMP_Text option2Text;
    public TMP_Text option3Text;

    [Header("游戏界面 - 资源显示")]
    public TMP_Text troopText;
    public TMP_Text foodText;
    public TMP_Text strategyText;
    public TMP_Text riskText;

    [Header("游戏界面 - 结局返回按钮")]
    public GameObject endingReturnButton;

    [Header("小游戏引用")]
    public PuzzleGameManager puzzleGameManager;
    public GridGameManager gridGameManager;
    public GameObject digTunnelPanel;

    // ✅ 新增：挖地道图片引用
    public Image digBeforeImage;
    public Image digAfterImage;

    // ✅ 新增：点击状态标志（加在这里，类成员变量区域）
    private bool digTunnelClicked = false;

    [Header("滑块输入小游戏")]
    public GameObject valueInputPanel;
    public Slider valueSlider;
    public TMP_Text sliderValueText;
    public Button confirmButton;
    public TMP_Text titleText;

    [Header("人物立绘资源")]
    public Sprite caocaoLeft;
    public Sprite yuanshaoLeft;
    public Sprite yuanshaoRight;
    public Sprite xuyouLeft;
    public Sprite xuyouRight;

    [Header("人物头像资源")]
    public Sprite caocaoAvatar;
    public Sprite yuanshaoAvatar;
    public Sprite xuyouAvatar;

    public static FinalUIManager Instance { get; private set; }
    private bool waitingForContinue = false;
    private int currentNextNodeId = 0;

    // 新增：记录当前显示的人物，用于调试
    private string currentLeftChar = "";
    private string currentRightChar = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 缓存RectTransform引用
            if (leftCharacterImage != null) leftCharRect = leftCharacterImage.rectTransform;
            if (rightCharacterImage != null) rightCharRect = rightCharacterImage.rectTransform;
            if (avatarImage != null) avatarRect = avatarImage.rectTransform;
            if (backgroundImage != null) backgroundRectTransform = backgroundImage.rectTransform;

            // ✅ 关键修复：强制保持人物图片比例，防止拉伸变形
            if (leftCharacterImage != null) leftCharacterImage.preserveAspect = true;
            if (rightCharacterImage != null) rightCharacterImage.preserveAspect = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable() // 改为OnEnable订阅，避免Start时序问题
    {
        StartCoroutine(DeferredSubscription());
    }

    void OnDisable()
    {
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnOptionSelected -= HandleDialogueOption;
            DialogueSystem.Instance.OnDialogueNodeShown -= OnDialogueNodeShown;
        }
        if (ResourceManager.Instance != null)
        {
            ResourceManager.OnResourceChanged -= OnResourceChanged;
        }
    }

    IEnumerator DeferredSubscription()
    {
        // 等待一帧确保DialogueSystem已初始化
        yield return null;

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnOptionSelected -= HandleDialogueOption; // 先取消避免重复
            DialogueSystem.Instance.OnOptionSelected += HandleDialogueOption;
            DialogueSystem.Instance.OnDialogueNodeShown -= OnDialogueNodeShown;
            DialogueSystem.Instance.OnDialogueNodeShown += OnDialogueNodeShown;
            Debug.Log("FinalUIManager: 事件订阅成功");
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.OnResourceChanged -= OnResourceChanged;
            ResourceManager.OnResourceChanged += OnResourceChanged;
        }
    }

    void Start()
    {
        Debug.Log("🎮 FinalUIManager 启动");

        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(false);
        if (backgroundIntroPanel != null) backgroundIntroPanel.SetActive(false);
        if (endingReturnButton != null) endingReturnButton.SetActive(false);

        HideAllCharacters();
        SetupButtonEvents();
    }

    void SetupButtonEvents()
    {
        if (optionButton1 != null)
            optionButton1.onClick.AddListener(() => OnOptionClicked(0));
        if (optionButton2 != null)
            optionButton2.onClick.AddListener(() => OnOptionClicked(1));
        if (optionButton3 != null)
            optionButton3.onClick.AddListener(() => OnOptionClicked(2));

        if (clickAreaButton != null)
            clickAreaButton.onClick.AddListener(OnClickAreaClicked);

        if (backgroundIntroClickArea != null)
            backgroundIntroClickArea.onClick.AddListener(OnBackgroundIntroClicked);

        if (endingReturnButton != null)
        {
            Button btn = endingReturnButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(ReturnToMainMenu);
        }
    }

    public void StartGame()
    {
        Debug.Log("开始游戏");

        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(true);

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResetAllResources();

        UpdateResourceDisplay();

        // 延迟一帧启动对话，确保UI已经就绪
        StartCoroutine(DelayedStartDialogue());
    }

    IEnumerator DelayedStartDialogue()
    {
        yield return new WaitForSeconds(0.1f);
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.StartDialogue();
    }

    private void OnDialogueNodeShown(int nodeId)
    {
        Debug.Log($"<color=cyan>FinalUIManager: 显示节点 {nodeId}</color>");

        if (DialogueSystem.Instance?.CurrentNode == null)
        {
            Debug.LogError("CurrentNode为空！");
            return;
        }

        DialogueNode node = DialogueSystem.Instance.CurrentNode;

        // ===== 剧情点5 - 选项B判定（500308）=====
        if (nodeId == 500308)
        {
            Debug.Log("<color=yellow>【判定节点】选项B - 检查是否满足IF线4条件...</color>");

            float troop = ResourceManager.Instance?.GetTroop() ?? 0;
            float food = ResourceManager.Instance?.GetFood() ?? 0;

            Debug.Log($"当前资源 - 兵力:{troop}, 粮草:{food}");

            int targetNodeId;

            // IF线4条件：兵力>70 且 粮草>50
            if (troop > 70 && food > 50)
            {
                targetNodeId = 500317; // 解锁IF线4：以退为进
                Debug.Log("<color=green>【判定结果】满足条件（兵力>70且粮草>50）→ 进入IF线4：以退为进</color>");
            }
            else
            {
                targetNodeId = 500309; // 解锁IF线3：元气大伤
                Debug.Log("<color=green>【判定结果】不满足条件 → 进入IF线3：元气大伤</color>");
            }

            DialogueSystem.Instance.ShowDialogueNode(targetNodeId);
            return;
        }
        // ===== 选项B判定结束 =====

        // ===== 剧情点5 - 选项C判定（500406）=====
        if (nodeId == 500406)
        {
            Debug.Log("<color=yellow>【判定节点】选项C - 检查IF线5或IF线6条件...</color>");

            float risk = ResourceManager.Instance?.GetRisk() ?? 0;
            float troop = ResourceManager.Instance?.GetTroop() ?? 0;
            float food = ResourceManager.Instance?.GetFood() ?? 0;

            Debug.Log($"当前资源 - 风险:{risk}, 兵力:{troop}, 粮草:{food}");

            int targetNodeId;

            // IF线5条件：风险<70 且 兵力>70 且 粮草>50
            if (risk < 70 && troop > 70 && food > 50)
            {
                targetNodeId = 500407; // 解锁IF线5：完美
                Debug.Log("<color=green>【判定结果】满足IF线5条件（风险<70,兵力>70,粮草>50）→ 进入IF线5：完美</color>");
            }
            // IF线6条件：风险>70 且 兵力<70 且 粮草<50
            else if (risk > 70 && troop < 70 && food < 50)
            {
                targetNodeId = 500414; // 解锁IF线6：惨败
                Debug.Log("<color=green>【判定结果】满足IF线6条件（风险>70,兵力<70,粮草<50）→ 进入IF线6：惨败</color>");
            }
            else
            {
                // 中间状态：默认进入IF线5（或者你可以根据设计调整）
                targetNodeId = 500407;
                Debug.Log("<color=orange>【判定结果】处于中间状态，默认进入IF线5：完美</color>");
            }

            DialogueSystem.Instance.ShowDialogueNode(targetNodeId);
            return;
        }
        // ===== 选项C判定结束 =====

        // 背景介绍节点处理
        if (node.isBackgroundIntro)
        {
            ShowBackgroundIntro(node);
            return;
        }
        else
        {
            HideBackgroundIntro();
        }

        if (gameInterfacePanel != null && !gameInterfacePanel.activeSelf)
            gameInterfacePanel.SetActive(true);

        // 更新背景和文字
        UpdateBackground(node.backgroundImage);
        UpdateDialogueText(node.dialogueText);
        UpdateResourceDisplay();

        // 关键修复：使用严格的结局判断
        bool isEndingNode = IsEndingNode(nodeId);

        if (isEndingNode)
        {
            // 真正的结局：只显示背景和返回按钮
            ShowEndingUI();
        }
        else if (node.options != null && node.options.Count > 0)
        {
            // 选项节点（如1004滑块确认按钮）：显示人物和选项
            UpdateCharacters(node);
            HideClickArea(); // 隐藏点击区域，显示选项按钮
            HideEndingReturnButton();
            SetupOptionButtons(node.options);
        }
        else
        {
            // 普通剧情节点：显示人物和点击区域
            UpdateCharacters(node);
            SetOptionsActive(false);
            HideEndingReturnButton();
            ShowClickArea(node.nextNodeId);
        }

        StartCoroutine(ForceCanvasRefresh());
    }

    void ShowEndingUI()
    {
        Debug.Log("<color=red>显示结局UI - 清理所有非必要元素</color>");

        // 1. 隐藏所有人物（头像和左右人物）
        HideAllCharacters();

        // 2. 隐藏头像框
        if (avatarFrameImage != null)
            avatarFrameImage.gameObject.SetActive(false);

        // 3. 隐藏对话框和对话文字
        if (dialogueText != null)
            dialogueText.gameObject.SetActive(false);

        // 关键：隐藏整个 DialoguePanel（对话框背景框）
        Transform dialoguePanel = gameInterfacePanel?.transform.Find("DialoguePanel");
        if (dialoguePanel != null)
            dialoguePanel.gameObject.SetActive(false);

        // 4. 隐藏侧边栏（根据你的Hierarchy）
        Transform sidebar = gameInterfacePanel?.transform.Find("Sidebar");
        if (sidebar != null)
            sidebar.gameObject.SetActive(false);

        // 5. 隐藏资源面板
        Transform resourcePanel = gameInterfacePanel?.transform.Find("ResourcePanel");
        if (resourcePanel != null)
            resourcePanel.gameObject.SetActive(false);

        // 6. 隐藏选项按钮和点击区域
        SetOptionsActive(false);
        HideClickArea();

        // 7. 显示返回主菜单按钮（关键！）
        if (endingReturnButton != null)
        {
            endingReturnButton.SetActive(true);
            endingReturnButton.transform.SetAsLastSibling(); // 确保在最上层

            Debug.Log("<color=green>显示返回主菜单按钮</color>");
        }
        else
        {
            Debug.LogError("endingReturnButton未赋值！请在Inspector中设置");
        }

        // 8. 确保背景在最底层且激活
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.transform.SetAsFirstSibling();
        }
    }

    void HideEndingReturnButton()
    {
        if (endingReturnButton != null)
            endingReturnButton.SetActive(false);
    }

    IEnumerator ForceCanvasRefresh()
    {
        yield return null; // 等待一帧
        if (gameInterfacePanel != null)
        {
            Canvas.ForceUpdateCanvases();
            // 强制重建所有布局
            var layouts = gameInterfacePanel.GetComponentsInChildren<LayoutGroup>(true);
            foreach (var layout in layouts)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }
        }
    }

    void ShowBackgroundIntro(DialogueNode node)
    {
        Debug.Log("显示背景介绍面板");

        if (gameInterfacePanel != null)
            gameInterfacePanel.SetActive(false);

        if (backgroundIntroPanel != null)
        {
            backgroundIntroPanel.SetActive(true);

            if (backgroundIntroImage != null && node.backgroundImage != null)
            {
                backgroundIntroImage.sprite = node.backgroundImage;
                backgroundIntroImage.SetNativeSize(); // 修复尺寸
                // 背景介绍图通常需要全屏，根据你的需求调整
                RectTransform rt = backgroundIntroImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }

            if (backgroundIntroText != null)
                backgroundIntroText.text = node.dialogueText;
        }

        HideClickArea();
        SetOptionsActive(false);
        if (endingReturnButton != null)
            endingReturnButton.SetActive(false);
    }

    void HideBackgroundIntro()
    {
        if (backgroundIntroPanel != null)
            backgroundIntroPanel.SetActive(false);
    }

    void OnBackgroundIntroClicked()
    {
        if (DialogueSystem.Instance?.CurrentNode != null)
        {
            int nextId = DialogueSystem.Instance.CurrentNode.nextNodeId;
            if (nextId > 0)
                DialogueSystem.Instance.ShowDialogueNode(nextId);
        }
    }

    // ========== 核心修复：人物显示逻辑 ==========
    void UpdateCharacters(DialogueNode node)
    {
        Debug.Log($"<color=yellow>更新人物 - 头像:{node.avatarCharacter}, 左:{node.leftCharacter}, 右:{node.rightCharacter}</color>");
        // 检查资源引用是否为null
        Debug.Log($"资源检查 - caocaoAvatar: {(caocaoAvatar == null ? "NULL" : caocaoAvatar.name)}");
        Debug.Log($"资源检查 - caocaoLeft: {(caocaoLeft == null ? "NULL" : caocaoLeft.name)}");
        Debug.Log($"资源检查 - yuanshaoLeft: {(yuanshaoLeft == null ? "NULL" : yuanshaoLeft.name)}");
        Debug.Log($"资源检查 - yuanshaoRight: {(yuanshaoRight == null ? "NULL" : yuanshaoRight.name)}");

        // 先全部隐藏（清理旧状态）
        HideAllCharacters();

        // 记录当前人物
        currentLeftChar = node.leftCharacter ?? "";
        currentRightChar = node.rightCharacter ?? "";

        // 逐个显示（延迟一帧显示避免UI闪烁）
        StartCoroutine(ShowCharactersCoroutine(node));
    }

    IEnumerator ShowCharactersCoroutine(DialogueNode node)
    {
        yield return null; // 等待HideAllCharacters生效

        if (!string.IsNullOrEmpty(node.avatarCharacter))
        {
            ShowAvatar(node.avatarCharacter);
        }

        if (!string.IsNullOrEmpty(node.leftCharacter))
        {
            ShowLeftCharacter(node.leftCharacter);
        }

        if (!string.IsNullOrEmpty(node.rightCharacter))
        {
            ShowRightCharacter(node.rightCharacter);
        }
    }

    void HideAllCharacters()
    {
        if (avatarFrameImage != null) avatarFrameImage.gameObject.SetActive(false);
        if (avatarImage != null) avatarImage.gameObject.SetActive(false);
        if (leftCharacterImage != null) leftCharacterImage.gameObject.SetActive(false);
        if (rightCharacterImage != null) rightCharacterImage.gameObject.SetActive(false);

        currentLeftChar = "";
        currentRightChar = "";
    }

    void ShowLeftCharacter(string characterName)
    {
        Sprite sprite = GetCharacterSprite(characterName, true);
        if (sprite != null && leftCharacterImage != null)
        {
            leftCharacterImage.sprite = sprite;
            leftCharacterImage.gameObject.SetActive(true);

            // 只处理曹操翻转，其他完全不动（不设置尺寸、不设置锚点）
            if (characterName == "曹操")
                leftCharacterImage.rectTransform.localScale = new Vector3(-Mathf.Abs(leftCharacterImage.rectTransform.localScale.x),
                                                                        leftCharacterImage.rectTransform.localScale.y, 1);
            else
                leftCharacterImage.rectTransform.localScale = new Vector3(Mathf.Abs(leftCharacterImage.rectTransform.localScale.x),
                                                                         leftCharacterImage.rectTransform.localScale.y, 1);

            Debug.Log($"<color=green>左人物{characterName}显示，Scale保持: {leftCharacterImage.rectTransform.localScale}</color>");
        }
    }

    void ShowRightCharacter(string characterName)
    {
        Sprite sprite = GetCharacterSprite(characterName, false);
        if (sprite != null && rightCharacterImage != null)
        {
            rightCharacterImage.sprite = sprite;
            rightCharacterImage.gameObject.SetActive(true);

            // 右人物都面朝左（不翻转Scale X）
            rightCharacterImage.rectTransform.localScale = new Vector3(Mathf.Abs(rightCharacterImage.rectTransform.localScale.x),
                                                                      rightCharacterImage.rectTransform.localScale.y, 1);

            Debug.Log($"<color=green>右人物{characterName}显示，Scale保持: {rightCharacterImage.rectTransform.localScale}</color>");
        }
    }

    void ShowAvatar(string characterName)
    {
        Sprite avatar = GetAvatarSprite(characterName);
        if (avatar != null && avatarImage != null)
        {
            avatarImage.sprite = avatar;
            avatarImage.gameObject.SetActive(true);

            // 只显示头像，不控制头像框（如果引用丢失就不显示框）
            if (avatarFrameImage != null)
                avatarFrameImage.gameObject.SetActive(true);

            Debug.Log($"<color=green>显示头像: {characterName}</color>");
        }
        else
        {
            Debug.LogWarning($"头像或Image组件缺失: {characterName}");
        }
    }

    Sprite GetAvatarSprite(string characterName)
    {
        switch (characterName)
        {
            case "曹操": return caocaoAvatar;
            case "袁绍": return yuanshaoAvatar;
            case "许攸": return xuyouAvatar;
            default:
                Debug.LogWarning($"未定义头像: {characterName}");
                return null;
        }
    }

    Sprite GetCharacterSprite(string characterName, bool isLeft)
    {
        // isLeft=true表示人物站在左边，应该面朝右（朝屏幕中心）
        switch (characterName)
        {
            case "曹操":
                // 曹操只有朝左版本，站左站右都用这个，通过flip控制朝向
                return caocaoLeft;
            case "袁绍":
                // 站左边用Right版本（面朝右），站右边用Left版本（面朝左）
                return isLeft ? yuanshaoRight : yuanshaoLeft;
            case "许攸":
                return isLeft ? xuyouRight : xuyouLeft;
            default:
                Debug.LogWarning($"未定义人物: {characterName}");
                return null;
        }
    }

    // ========== 核心修复：背景更新 ==========
    void UpdateBackground(Sprite bgSprite)
    {
        Debug.Log($"<color=blue>========== 背景更新 ==========</color>");
        Debug.Log($"传入Sprite: {(bgSprite == null ? "NULL" : bgSprite.name)}");
        Debug.Log($"backgroundImage组件: {(backgroundImage == null ? "NULL" : "存在")}");

        if (backgroundImage != null)
        {
            Debug.Log($"当前背景图GameObject状态: {backgroundImage.gameObject.activeSelf}");
            Debug.Log($"即将设置新背景: {(bgSprite ? bgSprite.name : "null")}");

            backgroundImage.sprite = bgSprite;
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.SetNativeSize();

            // 关键修复：确保背景在最底层
            backgroundImage.transform.SetAsFirstSibling();

            if (backgroundRectTransform != null)
            {
                backgroundRectTransform.anchorMin = Vector2.zero;
                backgroundRectTransform.anchorMax = Vector2.one;
                backgroundRectTransform.sizeDelta = Vector2.zero;
                backgroundRectTransform.anchoredPosition = Vector2.zero;
                backgroundRectTransform.localScale = Vector3.one;

                Debug.Log($"背景Rect设置完成 - Size: {backgroundRectTransform.sizeDelta}");
            }
        }
        else
        {
            Debug.LogError("backgroundImage为null！请在Inspector中赋值");
        }

        // 强制修复：确保颜色不透明且启用
        backgroundImage.color = Color.white;
        backgroundImage.gameObject.SetActive(false); // 先禁用再启用强制刷新
        backgroundImage.gameObject.SetActive(true);

        // 确保层级正确（背景在最底层）
        backgroundImage.transform.SetAsFirstSibling();

        Debug.Log($"<color=green>背景已更新为: {bgSprite.name}, 尺寸: {backgroundRectTransform.sizeDelta}</color>");
    }

    void UpdateDialogueText(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
            dialogueText.gameObject.SetActive(true);

            // 强制刷新文字布局
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueText.rectTransform);
        }
    }

    void ShowClickArea(int nextNodeId)
    {
        Debug.Log($"<color=red>ShowClickArea被调用，nextNodeId={nextNodeId}</color>");
        currentNextNodeId = nextNodeId;
        waitingForContinue = true;

        if (clickAreaButton != null)
            clickAreaButton.gameObject.SetActive(true);

        if (clickHint != null)
            clickHint.Show();
    }

    void HideClickArea()
    {
        waitingForContinue = false;
        if (clickAreaButton != null)
            clickAreaButton.gameObject.SetActive(false);
        if (clickHint != null)
            clickHint.Hide();
    }

    void OnClickAreaClicked()
    {
        if (!waitingForContinue) return;

        HideClickArea();

        if (currentNextNodeId > 0 && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.ShowDialogueNode(currentNextNodeId);
        }
    }

    void SetupOptionButtons(List<DialogueOption> options)
    {
        // 先全部隐藏
        if (optionButton1 != null) optionButton1.gameObject.SetActive(false);
        if (optionButton2 != null) optionButton2.gameObject.SetActive(false);
        if (optionButton3 != null) optionButton3.gameObject.SetActive(false);

        for (int i = 0; i < options.Count && i < 3; i++)
        {
            SetupSingleOption(i, options[i]);
        }
    }

    void SetupSingleOption(int index, DialogueOption option)
    {
        Button btn = null;
        TMP_Text txt = null;

        switch (index)
        {
            case 0: btn = optionButton1; txt = option1Text; break;
            case 1: btn = optionButton2; txt = option2Text; break;
            case 2: btn = optionButton3; txt = option3Text; break;
        }

        if (btn != null && txt != null)
        {
            txt.text = option.optionText;
            btn.onClick.RemoveAllListeners();
            int capturedIndex = index;
            btn.onClick.AddListener(() => OnOptionClicked(capturedIndex));
            btn.gameObject.SetActive(true);

            // 强制刷新按钮布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(btn.GetComponent<RectTransform>());
        }
    }

    void SetOptionsActive(bool active)
    {
        if (optionButton1 != null) optionButton1.gameObject.SetActive(active);
        if (optionButton2 != null) optionButton2.gameObject.SetActive(active);
        if (optionButton3 != null) optionButton3.gameObject.SetActive(active);
    }

    void OnOptionClicked(int optionIndex)
    {
        if (DialogueSystem.Instance?.CurrentNode == null) return;

        var options = DialogueSystem.Instance.CurrentNode.options;
        if (optionIndex < options.Count)
        {
            DialogueSystem.Instance.HandleOptionSelected(optionIndex);
        }
    }

    private void HandleDialogueOption(int optionIndex, DialogueOption option)
    {
        Debug.Log($"FinalUIManager: 处理选项 {optionIndex}");

        if (!string.IsNullOrEmpty(option.miniGameType))
        {
            StartMiniGame(option.miniGameType, option.nextNodeId);
        }
        else if (option.nextNodeId > 0)
        {
            DialogueSystem.Instance.ShowDialogueNode(option.nextNodeId);
        }
    }

    void StartMiniGame(string gameType, int nextNodeId)
    {
        Debug.Log($"启动小游戏: {gameType}");

        if (gameInterfacePanel != null)
            gameInterfacePanel.SetActive(false);

        switch (gameType.ToLower())
        {
            case "puzzle":
                if (puzzleGameManager != null)
                    puzzleGameManager.StartPuzzleGame((success) => OnMiniGameFinished(success, nextNodeId), nextNodeId);
                break;

            case "grid":
                if (gridGameManager != null)
                {
                    // 传入回调，处理游戏结果
                    gridGameManager.StartGridGame((success) => OnGridGameFinished(success, nextNodeId));
                }
                else
                {
                    Debug.LogWarning("GridGameManager未赋值，直接继续剧情");
                    OnMiniGameFinished(true, nextNodeId);
                }
                break;

            case "digtunnel":
                // ✅ 修复：添加挖地道逻辑
                if (digTunnelPanel != null)
                {
                    digTunnelPanel.SetActive(true);
                    // 这里需要调用你的挖地道游戏管理器，如果没有就延时后返回
                    StartCoroutine(DigTunnelCoroutine(nextNodeId));
                }
                else
                {
                    Debug.LogWarning("DigTunnelPanel未赋值，直接跳转");
                    OnMiniGameFinished(true, nextNodeId);
                }
                break;

            case "slider":
                StartSliderGame(nextNodeId);
                break;

            default:
                OnMiniGameFinished(true, nextNodeId);
                break;
        }
    }

    // ✅ 新的挖地道协程（方案A：点击后显示2秒自动结束）
    IEnumerator DigTunnelCoroutine(int nextNodeId)
    {
        Debug.Log("挖地道游戏开始...");

        // 初始化状态：显示挖掘前图片，隐藏挖掘后图片
        digBeforeImage.gameObject.SetActive(true);
        digAfterImage.gameObject.SetActive(false);

        // 重置点击标志
        digTunnelClicked = false;

        // 自动给DigBeforeImage添加点击监听
        Button beforeBtn = digBeforeImage.GetComponent<Button>();
        if (beforeBtn != null)
        {
            beforeBtn.onClick.RemoveAllListeners();
            beforeBtn.onClick.AddListener(OnDigBeforeClicked);
        }
        else
        {
            Debug.LogError("DigBeforeImage没有Button组件！请在Unity中添加");
        }

        // 关键：等待玩家点击，而不是等待时间
        yield return new WaitUntil(() => digTunnelClicked);

        Debug.Log("玩家点击了挖掘，切换图片");

        // 切换图片：隐藏Before，显示After
        digBeforeImage.gameObject.SetActive(false);
        digAfterImage.gameObject.SetActive(true);

        // 方案A：自动等待2秒后结束
        yield return new WaitForSeconds(2f);

        // 关闭面板，继续剧情
        digTunnelPanel.SetActive(false);
        OnMiniGameFinished(true, nextNodeId);
    }

    void StartSliderGame(int nextNodeId)
    {
        Debug.Log("启动滑块小游戏");

        if (valueInputPanel != null)
        {
            valueInputPanel.SetActive(true);

            if (valueSlider != null)
            {
                valueSlider.minValue = 0;
                valueSlider.maxValue = 30;
                valueSlider.value = 10;

                valueSlider.onValueChanged.RemoveAllListeners();
                valueSlider.onValueChanged.AddListener(OnSliderValueChanged);

                OnSliderValueChanged(valueSlider.value);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() => OnSliderConfirmed(nextNodeId));
            }

            if (titleText != null)
            {
                titleText.text = "为诱敌深入，需弃多少兵力为饵？";
            }
        }
        else
        {
            Debug.LogError("ValueInputPanel未赋值！");
            OnMiniGameFinished(true, nextNodeId);
        }
    }

    void OnSliderValueChanged(float value)
    {
        if (sliderValueText != null)
        {
            sliderValueText.text = Mathf.RoundToInt(value).ToString();
        }
    }

    void OnSliderConfirmed(int nextNodeId)
    {
        if (valueSlider != null)
        {
            float selectedValue = valueSlider.value;
            Debug.Log($"玩家选择兵力值: {selectedValue}");

            if (valueInputPanel != null)
                valueInputPanel.SetActive(false);

            int targetNodeId = (selectedValue <= 20) ? 100501 : 100601;
            OnMiniGameFinished(true, targetNodeId);
        }
    }

    public void OnMiniGameFinished(bool success, int nextNodeId)
    {
        if (gameInterfacePanel != null)
            gameInterfacePanel.SetActive(true);

        if (digTunnelPanel != null)
            digTunnelPanel.SetActive(false);

        Debug.Log("从小游戏返回，准备显示节点：" + nextNodeId);

        if (nextNodeId > 0 && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.ShowDialogueNode(nextNodeId);
        }
    }

    public void ReturnFromMiniGame()
    {
        Debug.Log("FinalUIManager: 从小游戏返回");

        if (gameInterfacePanel != null)
            gameInterfacePanel.SetActive(true);

        if (clickHint != null)
            clickHint.Hide();
        if (clickAreaButton != null)
            clickAreaButton.gameObject.SetActive(false);

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.CurrentNode != null)
        {
            int currentNodeId = DialogueSystem.Instance.CurrentNode.nodeId;
            Debug.Log($"FinalUIManager: 刷新节点 {currentNodeId} 的UI显示");
            OnDialogueNodeShown(currentNodeId);
        }
        else
        {
            Debug.LogWarning("FinalUIManager: DialogueSystem 未就绪，无法刷新UI");
            HideClickArea();
            SetOptionsActive(false);
        }
    }

    // 走格子游戏结束回调
    void OnGridGameFinished(bool success, int nextNodeId)
    {
        if (!success)
        {
            // 被敌军发现，兵力-20
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ModifyTroop(-20);
                Debug.Log("<color=red>【走格子失败】被敌军发现！兵力减少20！</color>");
            }
            else
            {
                Debug.LogWarning("ResourceManager未找到，无法扣除兵力");
            }

            // 可以在这里加一些失败提示UI，比如：
            // ShowFailTip("被敌军发现！损失20兵力...");
        }
        else
        {
            Debug.Log("<color=green>【走格子胜利】成功避开敌军！</color>");
        }

        // 无论成功失败，都继续剧情点5（500101）
        // 如果配置中的nextNodeId为0，默认使用500101
        int targetNodeId = (nextNodeId > 0) ? nextNodeId : 500101;

        Debug.Log($"<color=cyan>从走格子游戏返回，跳转到节点：{targetNodeId}</color>");

        // 调用原有的MiniGame结束逻辑，显示游戏主界面并跳转
        OnMiniGameFinished(true, targetNodeId);
    }

    bool IsEndingNode(int nodeId)
    {
        var node = DialogueSystem.Instance?.CurrentNode;
        if (node == null) return false;

        // 真正的结局节点判断标准：
        // 1. nextNodeId为0（没有后续节点）
        // 2. 没有选项（不是选择题或小游戏节点）
        bool hasNoOptions = (node.options == null || node.options.Count == 0);
        bool isRealEnding = (node.nextNodeId == 0) && hasNoOptions;

        if (isRealEnding) return true;

        // 手动备份列表（万一上面逻辑没覆盖到的特殊情况）
        int[] endingNodeIds = new int[]
        {
            200314, // 剧情点2 - 半途而废最后一页
            300416,
            500217,
            500313,
            500320,
            500411,
            500418// 等你找到其他五条线的最后一页ID再填进来
        };

        return System.Array.Exists(endingNodeIds, id => id == nodeId);
    }

    void ReturnToMainMenu()
    {
        Debug.Log("返回主菜单");

        // 1. 切换面板
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(true); // 先激活才能操作子对象

        // 2. 恢复所有被隐藏的UI元素（关键！）

        // 恢复 DialoguePanel（关键！）
        Transform dialoguePanel = gameInterfacePanel?.transform.Find("DialoguePanel");
        if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(true);

        // 恢复侧边栏
        Transform sidebar = gameInterfacePanel?.transform.Find("Sidebar");
        if (sidebar != null) sidebar.gameObject.SetActive(true);

        // 恢复资源面板
        Transform resourcePanel = gameInterfacePanel?.transform.Find("ResourcePanel");
        if (resourcePanel != null) resourcePanel.gameObject.SetActive(true);

        // 恢复对话框文字
        if (dialogueText != null) dialogueText.gameObject.SetActive(true);

        // 恢复头像框
        if (avatarFrameImage != null) avatarFrameImage.gameObject.SetActive(true);

        // 3. 清理结局状态
        HideAllCharacters();
        HideClickArea();
        SetOptionsActive(false);

        if (endingReturnButton != null)
            endingReturnButton.SetActive(false);

        // 4. 重置游戏数据
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResetAllResources();

        // 5. 最后隐藏游戏主界面
        if (gameInterfacePanel != null)
            gameInterfacePanel.SetActive(false);
    }

    void OnResourceChanged(ResourceChangeEvent changeEvent)
    {
        UpdateResourceDisplay();
    }

    void UpdateResourceDisplay()
    {
        if (ResourceManager.Instance == null) return;

        if (troopText != null)
            troopText.text = $"兵力: {ResourceManager.Instance.GetTroop():F0}";
        if (foodText != null)
            foodText.text = $"粮草: {ResourceManager.Instance.GetFood():F0}";
        if (strategyText != null)
            strategyText.text = $"计策: {ResourceManager.Instance.GetStrategy():F0}";
        if (riskText != null)
            riskText.text = $"风险: {ResourceManager.Instance.GetRisk():F0}";
    }

    // 点击挖掘前图片的回调
    public void OnDigBeforeClicked()
    {
        Debug.Log("点击了挖掘图片！");
        digTunnelClicked = true;

        // 资源消耗
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyFood(-1.0f);
        }
    }
}