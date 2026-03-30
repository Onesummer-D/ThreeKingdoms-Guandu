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
    public RectTransform backgroundRectTransform;

    [Header("游戏界面 - 人物")]
    public Image avatarFrameImage;
    public Image avatarImage;
    public Image leftCharacterImage;
    public Image rightCharacterImage;

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

    [Header("解锁结局按钮")]
    public Button viewEndingButton;
    public TMP_Text viewEndingButtonText;

    private Dictionary<int, int> unlockEndingNodes = new Dictionary<int, int>
    {
        { 500307, 500308 },
        { 500405, 500406 },
        { 300409, 300410 },
        { 200308, 200309 },
        { 500210, 500211 }
    };

    [Header("小游戏引用")]
    public PuzzleGameManager puzzleGameManager;
    public GridGameManager gridGameManager;
    public GameObject digTunnelPanel;
    public Image digBeforeImage;
    public Image digAfterImage;
    public TMP_Text digHintText;
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

    [Header("火烧乌巢动画")]
    public GameObject fireAnimationPanel;
    public Image fireAnimationImage;
    public Image fireAnimationImage2;
    public Sprite[] fireSprites;
    public float frameDuration = 1.5f;
    public float fadeDuration = 0.5f;
    private bool isPlayingFireAnimation = false;

    [Header("彩蛋称号显示")]
    public GameObject achievementPanel;
    public Image achievementImage;
    public RectTransform achievementRect;

    [Header("资源变化飘字")]
    public TMP_Text troopChangeText;
    public TMP_Text foodChangeText;
    public TMP_Text strategyChangeText;
    public TMP_Text riskChangeText;
    public float changeTextDuration = 1.5f;

    [Header("发石车显示")]
    public Image catapultImage;

    private HashSet<int> achievementAnimationNodes = new HashSet<int>
    {
        500215,
    };

    public static FinalUIManager Instance { get; private set; }
    private bool waitingForContinue = false;
    private int currentNextNodeId = 0;
    private string currentLeftChar = "";
    private string currentRightChar = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (leftCharacterImage != null) leftCharRect = leftCharacterImage.rectTransform;
            if (rightCharacterImage != null) rightCharRect = rightCharacterImage.rectTransform;
            if (avatarImage != null) avatarRect = avatarImage.rectTransform;
            if (backgroundImage != null) backgroundRectTransform = backgroundImage.rectTransform;

            if (leftCharacterImage != null) leftCharacterImage.preserveAspect = true;
            if (rightCharacterImage != null) rightCharacterImage.preserveAspect = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ✅ 新增：Start方法 - 首页启动播放BGM
    void Start()
    {
        Debug.Log("🎮 FinalUIManager 启动");

        // ✅ 首页启动即播放战争背景音
        if (AudioManager.Instance != null && AudioManager.Instance.warBackgroundBGM != null)
        {
            // 检查当前是否还没开始对话（避免重复播放）
            if (DialogueSystem.Instance == null || !DialogueSystem.Instance.IsShowing)
            {
                AudioManager.Instance.PlayBGM(
                    AudioManager.Instance.warBackgroundBGM,
                    loop: true,
                    stopPrevious: false
                );
                Debug.Log("<color=magenta>首页启动，播放战争背景音</color>");
            }
        }

        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(false);
        if (backgroundIntroPanel != null) backgroundIntroPanel.SetActive(false);
        if (endingReturnButton != null) endingReturnButton.SetActive(false);
        if (achievementPanel != null) achievementPanel.SetActive(false);
        if (fireAnimationPanel != null) fireAnimationPanel.SetActive(false);
        if (catapultImage != null) catapultImage.gameObject.SetActive(false);

        HideAllCharacters();
        SetupButtonEvents();
    }

    void OnEnable()
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
        yield return null;
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnOptionSelected -= HandleDialogueOption;
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

    // ✅ 只有一个StartMiniGame方法（保留正确的版本）
    void StartMiniGame(string gameType, int nextNodeId)
    {
        Debug.Log($"启动小游戏: {gameType}");

        // ✅ 修改：降低BGM音量到20%，而不是暂停
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ReduceBGMVolume(0.2f);
        }

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
                    gridGameManager.StartGridGame((success) => OnGridGameFinished(success, nextNodeId));
                }
                else
                {
                    Debug.LogWarning("GridGameManager未赋值，直接继续剧情");
                    OnMiniGameFinished(true, nextNodeId);
                }
                break;

            case "digtunnel":
                if (digTunnelPanel != null)
                {
                    digTunnelPanel.SetActive(true);
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

    void SetupButtonEvents()
    {
        if (optionButton1 != null) optionButton1.onClick.AddListener(() => OnOptionClicked(0));
        if (optionButton2 != null) optionButton2.onClick.AddListener(() => OnOptionClicked(1));
        if (optionButton3 != null) optionButton3.onClick.AddListener(() => OnOptionClicked(2));
        if (clickAreaButton != null) clickAreaButton.onClick.AddListener(OnClickAreaClicked);
        if (backgroundIntroClickArea != null) backgroundIntroClickArea.onClick.AddListener(OnBackgroundIntroClicked);
        if (endingReturnButton != null)
        {
            Button btn = endingReturnButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(ReturnToMainMenu);
        }
        if (viewEndingButton != null)
        {
            viewEndingButton.onClick.AddListener(OnViewEndingClicked);
            viewEndingButton.gameObject.SetActive(false);
        }
    }

    public void StartGame()
    {
        Debug.Log("开始游戏");

        if (startMenuPanel != null) startMenuPanel.SetActive(false);

        if (ResourceManager.Instance != null) ResourceManager.Instance.ResetAllResources();
        UpdateResourceDisplay();
        StartCoroutine(DelayedStartDialogue());
    }

    IEnumerator DelayedStartDialogue()
    {
        yield return new WaitForSeconds(0.1f);
        if (DialogueSystem.Instance != null) DialogueSystem.Instance.StartDialogue();
    }

    // 🔥 火烧乌巢动画
    public void PlayFireAnimation(int nextNodeId)
    {
        if (isPlayingFireAnimation) return;
        StartCoroutine(FireAnimationCoroutine(nextNodeId));
    }

    private IEnumerator FireAnimationCoroutine(int nextNodeId)
    {
        isPlayingFireAnimation = true;
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(false);
        if (fireAnimationPanel != null) fireAnimationPanel.SetActive(true);

        Image currentImg = fireAnimationImage;
        Image nextImg = fireAnimationImage2;

        currentImg.sprite = fireSprites[0];
        currentImg.color = Color.white;
        nextImg.color = new Color(1, 1, 1, 0);

        for (int i = 0; i < fireSprites.Length; i++)
        {
            currentImg.sprite = fireSprites[i];
            currentImg.color = Color.white;

            if (i < fireSprites.Length - 1)
            {
                nextImg.sprite = fireSprites[i + 1];
                nextImg.color = new Color(1, 1, 1, 0);
            }

            float stayTime = (i == fireSprites.Length - 1) ? frameDuration + 1.0f : frameDuration - fadeDuration;
            yield return new WaitForSeconds(stayTime);

            if (i < fireSprites.Length - 1)
            {
                float timer = 0;
                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    float t = timer / fadeDuration;
                    currentImg.color = new Color(1, 1, 1, 1 - t);
                    nextImg.color = new Color(1, 1, 1, t);
                    yield return null;
                }
                currentImg.color = new Color(1, 1, 1, 0);
                nextImg.color = Color.white;

                Image temp = currentImg;
                currentImg = nextImg;
                nextImg = temp;
            }
            else
            {
                float timer = 0;
                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    float alpha = 1 - (timer / fadeDuration);
                    currentImg.color = new Color(1, 1, 1, alpha);
                    yield return null;
                }
                currentImg.color = new Color(1, 1, 1, 0);
            }
        }

        if (fireAnimationPanel != null) fireAnimationPanel.SetActive(false);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(true);
        isPlayingFireAnimation = false;
        DialogueSystem.Instance.ShowDialogueNode(nextNodeId);
    }

    private void HandleAchievementDisplay(DialogueNode node)
    {
        if (node.achievementSprite == null)
        {
            if (achievementPanel != null) achievementPanel.SetActive(false);
            return;
        }

        if (achievementPanel != null && achievementImage != null && achievementRect != null)
        {
            achievementPanel.SetActive(true);
            achievementImage.sprite = node.achievementSprite;
            achievementRect.anchoredPosition = node.achievementPosition;

            Debug.Log($"<color=yellow>显示彩蛋称号：节点{node.nodeId}</color>");

            if (achievementAnimationNodes.Contains(node.nodeId))
            {
                StopCoroutine("AchievementPopAnim");
                StartCoroutine(AchievementPopAnim(achievementRect.localScale.x));
            }
        }
    }

    private void HandleCatapultDisplay(int nodeId)
    {
        bool shouldShowCatapult = (nodeId >= 100203 && nodeId <= 100207);
        if (catapultImage != null)
            catapultImage.gameObject.SetActive(shouldShowCatapult);
    }

    private IEnumerator AchievementPopAnim(float finalScale)
    {
        float duration = 0.5f;
        float elapsed = 0;
        achievementRect.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale;
            if (t < 0.6f)
                scale = Mathf.Lerp(0, 1.2f, t / 0.6f);
            else
                scale = Mathf.Lerp(1.2f, 1f, (t - 0.6f) / 0.4f);

            if (achievementRect != null)
                achievementRect.localScale = Vector3.one * scale * finalScale;
            yield return null;
        }

        if (achievementRect != null)
            achievementRect.localScale = Vector3.one * finalScale;
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

        HandleAchievementDisplay(node);
        HandleCatapultDisplay(nodeId);

        if (unlockEndingNodes.ContainsKey(nodeId))
        {
            ShowUnlockEndingUI(node);
            return;
        }

        if (nodeId == 500308)
        {
            HandleNode500308();
            return;
        }
        if (nodeId == 500406)
        {
            HandleNode500406();
            return;
        }

        if (node.isBackgroundIntro)
        {
            if (gameInterfacePanel != null) gameInterfacePanel.SetActive(false);
            ShowBackgroundIntro(node);
            return;
        }
        else
        {
            HideBackgroundIntro();
        }

        if (gameInterfacePanel != null && !gameInterfacePanel.activeSelf)
            gameInterfacePanel.SetActive(true);

        UpdateBackground(node.backgroundImage);
        UpdateDialogueText(node.dialogueText);
        UpdateResourceDisplay();

        bool isEndingNode = IsEndingNode(nodeId);

        if (isEndingNode)
        {
            ShowEndingUI();
        }
        else if (node.options != null && node.options.Count > 0)
        {
            UpdateCharacters(node);
            HideClickArea();
            HideEndingReturnButton();
            SetupOptionButtons(node.options);
        }
        else
        {
            UpdateCharacters(node);
            SetOptionsActive(false);
            HideEndingReturnButton();
            ShowClickArea(node.nextNodeId);
        }

        StartCoroutine(ForceCanvasRefresh());
    }

    void HandleNode500308()
    {
        float troop = ResourceManager.Instance?.GetTroop() ?? 0;
        float food = ResourceManager.Instance?.GetFood() ?? 0;
        int targetNodeId = (troop > 70 && food > 50) ? 500317 : 500309;
        DialogueSystem.Instance.ShowDialogueNode(targetNodeId);
    }

    void HandleNode500406()
    {
        float risk = ResourceManager.Instance?.GetRisk() ?? 0;
        float troop = ResourceManager.Instance?.GetTroop() ?? 0;
        float food = ResourceManager.Instance?.GetFood() ?? 0;

        int targetNodeId;
        if (risk < 70 && troop > 70 && food > 50)
            targetNodeId = 500407;
        else if (risk > 70 && troop < 70 && food < 50)
            targetNodeId = 500414;
        else
            targetNodeId = 500407;

        DialogueSystem.Instance.ShowDialogueNode(targetNodeId);
    }

    void ShowUnlockEndingUI(DialogueNode node)
    {
        Debug.Log($"<color=yellow>显示解锁结局按钮 - 节点 {node.nodeId}</color>");

        if (gameInterfacePanel != null && !gameInterfacePanel.activeSelf)
            gameInterfacePanel.SetActive(true);

        UpdateBackground(node.backgroundImage);
        UpdateCharacters(node);
        UpdateDialogueText(node.dialogueText);
        UpdateResourceDisplay();

        HideClickArea();
        SetOptionsActive(false);
        HideEndingReturnButton();

        if (viewEndingButton != null)
        {
            viewEndingButton.gameObject.SetActive(true);
            viewEndingButton.transform.SetAsLastSibling();

            if (viewEndingButtonText != null)
            {
                if (node.nodeId == 500307)
                    viewEndingButtonText.text = "解锁IF线结局";
                else if (node.nodeId == 500405)
                    viewEndingButtonText.text = "解锁IF线结局";
                else
                    viewEndingButtonText.text = "查看结局";
            }
        }

        currentNextNodeId = unlockEndingNodes[node.nodeId];
    }

    void OnViewEndingClicked()
    {
        Debug.Log($"<color=green>点击解锁结局，跳转到节点：{currentNextNodeId}</color>");

        if (viewEndingButton != null)
            viewEndingButton.gameObject.SetActive(false);

        if (currentNextNodeId > 0 && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.ShowDialogueNode(currentNextNodeId);
        }
    }

    void ShowEndingUI()
    {
        Debug.Log("<color=red>显示结局UI - 清理所有非必要元素</color>");

        HideAllCharacters();

        if (avatarFrameImage != null)
            avatarFrameImage.gameObject.SetActive(false);

        if (dialogueText != null)
            dialogueText.gameObject.SetActive(false);

        Transform dialoguePanel = gameInterfacePanel?.transform.Find("DialoguePanel");
        if (dialoguePanel != null)
            dialoguePanel.gameObject.SetActive(false);

        Transform sidebar = gameInterfacePanel?.transform.Find("Sidebar");
        if (sidebar != null)
            sidebar.gameObject.SetActive(false);

        Transform resourcePanel = gameInterfacePanel?.transform.Find("ResourcePanel");
        if (resourcePanel != null)
            resourcePanel.gameObject.SetActive(false);

        SetOptionsActive(false);
        HideClickArea();

        if (endingReturnButton != null)
        {
            endingReturnButton.SetActive(true);
            endingReturnButton.transform.SetAsLastSibling();
            Debug.Log("<color=green>显示返回主菜单按钮</color>");
        }
        else
        {
            Debug.LogError("endingReturnButton未赋值！请在Inspector中设置");
        }

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
        yield return null;
        if (gameInterfacePanel != null)
        {
            Canvas.ForceUpdateCanvases();
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
                backgroundIntroImage.SetNativeSize();
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

    void UpdateCharacters(DialogueNode node)
    {
        Debug.Log($"<color=yellow>更新人物 - 头像:{node.avatarCharacter}, 左:{node.leftCharacter}, 右:{node.rightCharacter}</color>");

        HideAllCharacters();

        currentLeftChar = node.leftCharacter ?? "";
        currentRightChar = node.rightCharacter ?? "";

        StartCoroutine(ShowCharactersCoroutine(node));
    }

    IEnumerator ShowCharactersCoroutine(DialogueNode node)
    {
        yield return null;

        if (!string.IsNullOrEmpty(node.avatarCharacter))
            ShowAvatar(node.avatarCharacter);

        if (!string.IsNullOrEmpty(node.leftCharacter))
            ShowLeftCharacter(node.leftCharacter);

        if (!string.IsNullOrEmpty(node.rightCharacter))
            ShowRightCharacter(node.rightCharacter);
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

            if (characterName == "曹操")
                leftCharacterImage.rectTransform.localScale = new Vector3(-Mathf.Abs(leftCharacterImage.rectTransform.localScale.x),
                                                                        leftCharacterImage.rectTransform.localScale.y, 1);
            else
                leftCharacterImage.rectTransform.localScale = new Vector3(Mathf.Abs(leftCharacterImage.rectTransform.localScale.x),
                                                                         leftCharacterImage.rectTransform.localScale.y, 1);

            Debug.Log($"<color=green>左人物{characterName}显示</color>");
        }
    }

    void ShowRightCharacter(string characterName)
    {
        Sprite sprite = GetCharacterSprite(characterName, false);
        if (sprite != null && rightCharacterImage != null)
        {
            rightCharacterImage.sprite = sprite;
            rightCharacterImage.gameObject.SetActive(true);

            rightCharacterImage.rectTransform.localScale = new Vector3(Mathf.Abs(rightCharacterImage.rectTransform.localScale.x),
                                                                      rightCharacterImage.rectTransform.localScale.y, 1);

            Debug.Log($"<color=green>右人物{characterName}显示</color>");
        }
    }

    void ShowAvatar(string characterName)
    {
        Sprite avatar = GetAvatarSprite(characterName);
        if (avatar != null && avatarImage != null)
        {
            avatarImage.sprite = avatar;
            avatarImage.gameObject.SetActive(true);

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
        switch (characterName)
        {
            case "曹操":
                return caocaoLeft;
            case "袁绍":
                return isLeft ? yuanshaoRight : yuanshaoLeft;
            case "许攸":
                return isLeft ? xuyouRight : xuyouLeft;
            default:
                Debug.LogWarning($"未定义人物: {characterName}");
                return null;
        }
    }

    void UpdateBackground(Sprite bgSprite)
    {
        Debug.Log($"<color=blue>背景更新: {(bgSprite == null ? "NULL" : bgSprite.name)}</color>");

        if (backgroundImage != null)
        {
            backgroundImage.sprite = bgSprite;
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.SetNativeSize();
            backgroundImage.transform.SetAsFirstSibling();

            if (backgroundRectTransform != null)
            {
                backgroundRectTransform.anchorMin = Vector2.zero;
                backgroundRectTransform.anchorMax = Vector2.one;
                backgroundRectTransform.sizeDelta = Vector2.zero;
                backgroundRectTransform.anchoredPosition = Vector2.zero;
                backgroundRectTransform.localScale = Vector3.one;
            }

            backgroundImage.color = Color.white;
            backgroundImage.gameObject.SetActive(false);
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.transform.SetAsFirstSibling();

            Debug.Log($"<color=green>背景已更新: {bgSprite?.name}</color>");
        }
        else
        {
            Debug.LogError("backgroundImage为null！请在Inspector中赋值");
        }
    }

    void UpdateDialogueText(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
            dialogueText.gameObject.SetActive(true);
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
        if (optionButton1 != null) optionButton1.gameObject.SetActive(false);
        if (optionButton2 != null) optionButton2.gameObject.SetActive(false);
        if (optionButton3 != null) optionButton3.gameObject.SetActive(false);

        for (int i = 0; i < options.Count && i < 3; i++)
        {
            SetupSingleOption(i, options[i]);
        }

        StartCoroutine(ClearSelection());
    }

    IEnumerator ClearSelection()
    {
        yield return new WaitForSeconds(0.05f);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        if (optionButton1 != null) optionButton1.OnDeselect(null);
        if (optionButton2 != null) optionButton2.OnDeselect(null);
        if (optionButton3 != null) optionButton3.OnDeselect(null);
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

        bool hasResourceChange = ShowResourceChanges(option);

        if (hasResourceChange)
        {
            StartCoroutine(DelayedAction(() => ExecuteOptionAction(option), changeTextDuration));
        }
        else
        {
            ExecuteOptionAction(option);
        }
    }

    // ✅ 删除重复的StartMiniGame方法（之前这里有一个旧的，现在只保留一个）

    IEnumerator DigTunnelCoroutine(int nextNodeId)
    {
        Debug.Log("挖地道游戏开始...");

        digBeforeImage.gameObject.SetActive(true);
        digAfterImage.gameObject.SetActive(false);
        digTunnelClicked = false;

        if (digHintText != null)
        {
            digHintText.text = "点击地道，进行挖掘";
        }

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

        yield return new WaitUntil(() => digTunnelClicked);

        Debug.Log("玩家点击了挖掘，切换图片");

        if (digHintText != null)
        {
            digHintText.text = "恭喜！地道挖掘成功！";
        }
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDigSuccess();

        digBeforeImage.gameObject.SetActive(false);
        digAfterImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);

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
            float currentTroop = ResourceManager.Instance?.GetTroop() ?? 50;
            float remaining = currentTroop - value;
            sliderValueText.text = $"剩余兵力：{remaining}";
        }
    }

    void OnSliderConfirmed(int nextNodeId)
    {
        if (valueSlider != null)
        {
            float selectedValue = valueSlider.value;
            Debug.Log($"玩家选择舍弃兵力: {selectedValue}");

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ModifyTroop(-selectedValue);
                Debug.Log($"<color=red>扣除兵力 {selectedValue}，剩余兵力: {ResourceManager.Instance.GetTroop()}</color>");
            }

            if (valueInputPanel != null)
                valueInputPanel.SetActive(false);

            int targetNodeId = (selectedValue <= 20) ? 100501 : 100601;
            OnMiniGameFinished(true, targetNodeId);
        }
    }

    public void OnMiniGameFinished(bool success, int nextNodeId)
    {
        // ✅ 修改：恢复BGM音量
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RestoreBGMVolume();
        }

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

    void OnGridGameFinished(bool success, int nextNodeId)
    {
        if (!success)
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ModifyTroop(-20);
                Debug.Log("<color=red>【走格子失败】被敌军发现！兵力减少20！</color>");
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayAlert();
        }
        else
        {
            Debug.Log("<color=green>【走格子胜利】成功避开敌军！</color>");
            // ✅ 新增：胜利播放达成成就音效
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayAchievement();
        }

        int targetNodeId = (nextNodeId > 0) ? nextNodeId : 500101;

        Debug.Log($"<color=cyan>从走格子游戏返回，跳转到节点：{targetNodeId}</color>");

        OnMiniGameFinished(true, targetNodeId);
    }

    bool IsEndingNode(int nodeId)
    {
        var node = DialogueSystem.Instance?.CurrentNode;
        if (node == null) return false;

        bool hasNoOptions = (node.options == null || node.options.Count == 0);
        bool isRealEnding = (node.nextNodeId == 0) && hasNoOptions;

        if (isRealEnding) return true;

        int[] endingNodeIds = new int[]
        {
            200314,
            300416,
            500217,
            500313,
            500320,
            500411,
            500418
        };

        return System.Array.Exists(endingNodeIds, id => id == nodeId);
    }

    void ReturnToMainMenu()
    {
        Debug.Log("返回主菜单");

        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameInterfacePanel != null) gameInterfacePanel.SetActive(true);

        Transform dialoguePanel = gameInterfacePanel?.transform.Find("DialoguePanel");
        if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(true);

        Transform sidebar = gameInterfacePanel?.transform.Find("Sidebar");
        if (sidebar != null) sidebar.gameObject.SetActive(true);

        Transform resourcePanel = gameInterfacePanel?.transform.Find("ResourcePanel");
        if (resourcePanel != null) resourcePanel.gameObject.SetActive(true);

        if (dialogueText != null) dialogueText.gameObject.SetActive(true);
        if (avatarFrameImage != null) avatarFrameImage.gameObject.SetActive(true);

        HideAllCharacters();
        HideClickArea();
        SetOptionsActive(false);

        if (endingReturnButton != null)
            endingReturnButton.SetActive(false);

        if (viewEndingButton != null)
            viewEndingButton.gameObject.SetActive(false);

        if (achievementPanel != null)
            achievementPanel.SetActive(false);

        if (catapultImage != null)
            catapultImage.gameObject.SetActive(false);

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResetAllResources();

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
            troopText.text = $"{ResourceManager.Instance.GetTroop():F0}";
        if (foodText != null)
            foodText.text = $"{ResourceManager.Instance.GetFood():F0}";
        if (strategyText != null)
            strategyText.text = $"{ResourceManager.Instance.GetStrategy():F0}";
        if (riskText != null)
            riskText.text = $"{ResourceManager.Instance.GetRisk():F0}";
    }

    private bool ShowResourceChanges(DialogueOption option)
    {
        bool hasChange = false;

        if (option.troopEffect != 0)
        {
            ShowFloatingText(troopChangeText, "兵力", option.troopEffect, new Color(0.9f, 0.2f, 0.2f));
            hasChange = true;
        }
        if (option.foodEffect != 0)
        {
            ShowFloatingText(foodChangeText, "粮草", option.foodEffect, new Color(1f, 0.8f, 0.2f));
            hasChange = true;
        }
        if (option.strategyEffect != 0)
        {
            ShowFloatingText(strategyChangeText, "计策", option.strategyEffect, new Color(0.8f, 0.5f, 0.2f));
            hasChange = true;
        }
        if (option.riskEffect != 0)
        {
            ShowFloatingText(riskChangeText, "风险", option.riskEffect, new Color(0.6f, 0.6f, 0.6f));
            hasChange = true;
        }

        return hasChange;
    }

    private void ShowFloatingText(TMP_Text textComponent, string resourceName, float value, Color color)
    {
        if (textComponent == null) return;

        string sign = value > 0 ? "+" : "";
        textComponent.text = $"{resourceName}{sign}{value}";
        textComponent.color = color;

        float xOffset = 0;
        float yOffset = 350;

        switch (resourceName)
        {
            case "兵力":
                xOffset = -200;
                yOffset = 510;
                break;
            case "粮草":
                xOffset = -80;
                yOffset = 420;
                break;
            case "计策":
                xOffset = 80;
                yOffset = 330;
                break;
            case "风险":
                xOffset = 200;
                yOffset = 270;
                break;
        }

        textComponent.rectTransform.anchoredPosition = new Vector2(xOffset, yOffset);

        StartCoroutine(FloatingTextAnimation(textComponent));
    }

    private IEnumerator FloatingTextAnimation(TMP_Text text)
    {
        text.gameObject.SetActive(true);
        Vector3 startPos = text.rectTransform.anchoredPosition;
        float elapsed = 0;

        while (elapsed < changeTextDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / changeTextDuration;

            text.rectTransform.anchoredPosition = startPos + new Vector3(0, t * 50, 0);

            Color c = text.color;
            c.a = 1 - t;
            text.color = c;

            yield return null;
        }

        text.gameObject.SetActive(false);
        text.rectTransform.anchoredPosition = startPos;
        Color resetColor = text.color;
        resetColor.a = 1;
        text.color = resetColor;
    }

    private IEnumerator DelayedAction(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    private void ExecuteOptionAction(DialogueOption option)
    {
        // 剧情点5选项A：全力攻占乌巢（火烧动画）
        if (option.nextNodeId == 500201)
        {
            // ✅ 不停止BGM，播放火烧音效
            if (AudioManager.Instance != null && AudioManager.Instance.fireWuchaoSFX != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.fireWuchaoSFX);
            }

            PlayFireAnimation(500201);
            return;
        }

        if (!string.IsNullOrEmpty(option.miniGameType))
        {
            StartMiniGame(option.miniGameType, option.nextNodeId);
        }
        else if (option.nextNodeId > 0)
        {
            DialogueSystem.Instance.ShowDialogueNode(option.nextNodeId);
        }
    }

    public void OnDigBeforeClicked()
    {
        Debug.Log("点击了挖掘图片！");
        digTunnelClicked = true;

        // ✅ 确保调用挖掘音效（园艺-用铲子）
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDigging();

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyFood(-1.0f);
        }
    }
}