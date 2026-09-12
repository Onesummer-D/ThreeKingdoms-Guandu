using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Offline battle-text sharing and player introduction. No network room is
/// created here; a future transport must not treat a local hash as a join code.
/// </summary>
public sealed class InviteCoCreationUI : MonoBehaviour
{
    private readonly Color panelColor = new Color(0.075f, 0.11f, 0.15f, 0.98f);
    private readonly Color overlayColor = new Color(0.015f, 0.025f, 0.035f, 0.92f);
    private readonly Color textPrimary = new Color(0.94f, 0.94f, 0.88f, 1f);
    private readonly Color textMuted = new Color(0.69f, 0.76f, 0.78f, 1f);
    private readonly Color accentColor = new Color(0.89f, 0.71f, 0.32f, 1f);

    private CampaignMapUI campaignMapUI;
    private GameObject inviteOverlay;
    private GameObject invitePanel;
    private TMP_Text titleText;
    private TMP_Text statusText;
    private TMP_Text snapshotText;
    private InviteSnapshotData snapshot;
    private ScrollRect inviteScroll;
    private ScrollRect briefScroll;
    private GameObject playerBrief;
    private Button beginButton;
    private GameObject decisionInviteButton;
    private Image decisionInviteImage;
    private GameObject advisorGuestPanel;
    private GameObject advisorReviewPanel;
    private TMP_InputField advisorCodeInput;
    private TMP_InputField advisorNameInput;
    private TMP_InputField advisorReasonInput;
    private TMP_Text advisorGuestStatusText;
    private TMP_Text advisorReviewText;
    private Button advisorAcceptButton;
    private Button advisorDeclineButton;
    private readonly List<Button> advisorOptionButtons = new List<Button>();
    private int advisorSelectedOption = -1;
    private InviteSessionState inviteSession = new InviteSessionState();
    private bool initialized;

    private void OnDestroy()
    {
        if (campaignMapUI != null) campaignMapUI.OnClosed -= HandleMapClosed;
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueNodeShown -= HandleDecisionNodeShown;
            DialogueSystem.Instance.OnNodeTransition -= HandleDecisionNodeTransition;
        }
        if (inviteOverlay != null) Destroy(inviteOverlay);
        if (playerBrief != null) Destroy(playerBrief);
        inviteSession.Reset();
    }

    public void Initialize(CampaignMapUI mapUI, Transform uiRoot, Transform dialogueParent)
    {
        if (initialized || mapUI == null || uiRoot == null) return;
        initialized = true;
        campaignMapUI = mapUI;
        campaignMapUI.OnClosed -= HandleMapClosed;
        campaignMapUI.OnClosed += HandleMapClosed;
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueNodeShown -= HandleDecisionNodeShown;
            DialogueSystem.Instance.OnDialogueNodeShown += HandleDecisionNodeShown;
            DialogueSystem.Instance.OnNodeTransition -= HandleDecisionNodeTransition;
            DialogueSystem.Instance.OnNodeTransition += HandleDecisionNodeTransition;
        }
        CreateInviteOverlay(uiRoot);
        CreatePlayerBrief(uiRoot);
        CreateDecisionInviteButton(dialogueParent != null ? dialogueParent : uiRoot);
    }

    public void OpenInvitePanel()
    {
        if (!initialized || campaignMapUI == null || inviteOverlay == null) return;

        if (!campaignMapUI.TryBuildInviteSnapshot(out snapshot)) return;
        if (!inviteSession.Matches(snapshot) || inviteSession.Status == InviteSessionStatus.Declined)
        {
            inviteSession.Begin(snapshot, GetCurrentOptionTexts());
        }
        RenderInviteCard();
        inviteOverlay.SetActive(true);
        inviteOverlay.transform.SetAsLastSibling();
        SetDecisionInviteVisible(false);
        Canvas.ForceUpdateCanvases();
        inviteScroll.verticalNormalizedPosition = 1f;
        if (inviteSession.Status == InviteSessionStatus.GuestSubmitted ||
            inviteSession.Status == InviteSessionStatus.Accepted ||
            inviteSession.Status == InviteSessionStatus.Declined)
            ShowAdvisorReviewPanel();
        else
            SetAdvisorPanels(false, false);
    }

    private void CloseInvitePanel()
    {
        if (inviteOverlay != null) inviteOverlay.SetActive(false);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        snapshot = null;
        SetAdvisorPanels(false, false);
        RefreshDecisionInviteButton();
    }

    private void HandleMapClosed()
    {
        if (inviteOverlay != null) inviteOverlay.SetActive(false);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        snapshot = null;
        SetAdvisorPanels(false, false);
        // Closing a recap is not leaving the decision node. Re-evaluate the
        // current node so the invite affordance returns on every story line.
        RefreshDecisionInviteButton();
    }

    private void HandleDecisionNodeShown(int nodeId) { RefreshDecisionInviteButton(); }
    private void HandleDecisionNodeTransition(int fromNodeId, int toNodeId) { RefreshDecisionInviteButton(); }

    private void RefreshDecisionInviteButton()
    {
        DialogueNode node = DialogueSystem.Instance != null ? DialogueSystem.Instance.CurrentNode : null;
        bool decision = node != null && node.options != null && node.options.Count > 0 &&
            (node.nodeId == 1001 || node.nodeId == 2001 || node.nodeId == 3001 ||
             node.nodeId == 4001 || node.nodeId == 5001);
        SetDecisionInviteVisible(decision && (inviteOverlay == null || !inviteOverlay.activeSelf));
    }

    private void SetDecisionInviteVisible(bool visible)
    {
        if (decisionInviteButton != null)
        {
            if (visible)
            {
                SyncDecisionInviteLabel(decisionInviteButton.transform.parent);
                AlignDecisionInviteButton(decisionInviteButton.transform.parent);
            }
            decisionInviteButton.SetActive(visible);
        }
    }

    private void RenderInviteCard()
    {
        if (snapshot == null) return;
        if (titleText != null) titleText.text = "军议邀约";
        if (snapshotText != null)
        {
            snapshotText.text =
                "邀请方身份：曹操 · 曹军主将\n" +
                "受邀方身份：参谋\n\n" +
                "已走剧情：\n" + snapshot.StoryRecap + "\n\n" +
                "本局选择：" + StripChapterPrefix(snapshot.SelectedRouteSummary) + "\n" +
                "兵力 " + snapshot.Troop + "/" + ResourceManager.MAX_TROOP.ToString("F0") +
                "　粮草 " + snapshot.Food + "/" + ResourceManager.MAX_FOOD.ToString("F0") + "\n" +
                "计策 " + snapshot.Strategy + "/" + ResourceManager.MAX_STRATEGY.ToString("F0") +
                "　风险 " + snapshot.Risk + "/" + ResourceManager.MAX_RISK.ToString("F0") + "\n\n" +
                snapshot.DecisionContext;
        }
        if (statusText != null)
        {
            string code = string.IsNullOrWhiteSpace(inviteSession.InviteCode)
                ? "" : "\n本机接力码：" + inviteSession.InviteCode;
            statusText.text = code;
        }
    }

    private void CopyBattleInvitation()
    {
        if (snapshot == null) return;
        GUIUtility.systemCopyBuffer = "【官渡之战 · 请你来参谋】\n" +
            snapshotText.text +
            "\n\n请在聊天中回复建议的选项，理由可选。";
        if (statusText != null) statusText.text = "已复制，可粘贴到微信发送";
    }

    private void CreateInviteOverlay(Transform parent)
    {
        inviteOverlay = CreateUIObject("InviteCoCreationOverlay", parent);
        Stretch(inviteOverlay.GetComponent<RectTransform>());
        Image overlayImage = inviteOverlay.AddComponent<Image>();
        overlayImage.color = overlayColor;

        invitePanel = CreateUIObject("InviteCoCreationPanel", inviteOverlay.transform);
        RectTransform panelRect = invitePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.06f);
        panelRect.anchorMax = new Vector2(0.92f, 0.94f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = invitePanel.AddComponent<Image>();
        panelImage.color = panelColor;

        titleText = CreateText(invitePanel.transform, "军议邀约", 48f,
            TextAlignmentOptions.Left, textPrimary);
        SetHeader(titleText.rectTransform);

        Button closeButton = CreateButton(invitePanel.transform, "关闭", new Vector2(180f, 72f),
            new Vector2(-30f, -26f), new Color(0.30f, 0.20f, 0.18f, 1f));
        closeButton.onClick.AddListener(CloseInvitePanel);

        Transform content = CreateScrollContent(invitePanel.transform, out inviteScroll);
        CreateWrappedText(content, "战局快照", 44f, accentColor);
        snapshotText = CreateWrappedText(content, "", 38f, textPrimary);
        CreateWrappedText(content, "如何邀请", 44f, accentColor);
        CreateWrappedText(content,
            "复制战局邀请 → 粘贴到微信发给朋友 → 回到游戏作出决定", 38f, textPrimary);

        statusText = CreateText(invitePanel.transform, "", 30f, TextAlignmentOptions.Left, textMuted);
        SetAnchored(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        statusText.rectTransform.offsetMin = new Vector2(42f, 30f);
        statusText.rectTransform.offsetMax = new Vector2(-345f, 102f);
        statusText.enableWordWrapping = true;
        statusText.overflowMode = TextOverflowModes.Overflow;

        Button copyButton = CreateButton(invitePanel.transform, "复制战局邀请", new Vector2(290f, 72f),
            new Vector2(-30f, -474f), new Color(0.30f, 0.24f, 0.16f, 1f));
        SetBottomRight(copyButton.GetComponent<RectTransform>(), new Vector2(-30f, 30f),
            new Vector2(290f, 72f));
        copyButton.onClick.AddListener(CopyBattleInvitation);

        Button localHandoffButton = CreateButton(invitePanel.transform, "本机应邀加入",
            new Vector2(290f, 72f), Vector2.zero, new Color(.30f, .24f, .16f, 1f));
        SetBottomRight(localHandoffButton.GetComponent<RectTransform>(), new Vector2(-345f, 30f),
            new Vector2(290f, 72f));
        localHandoffButton.onClick.AddListener(OpenGuestAdvisorPanel);

        CreateAdvisorPanels();

        inviteOverlay.SetActive(false);
    }

    private List<string> GetCurrentOptionTexts()
    {
        List<string> result = new List<string>();
        if (DialogueSystem.Instance == null) return result;
        List<DialogueOption> options = DialogueSystem.Instance.GetCurrentOptions();
        if (options == null) return result;
        for (int i = 0; i < options.Count; i++)
            if (options[i] != null) result.Add(options[i].optionText ?? string.Empty);
        return result;
    }

    /// <summary>
    /// Clears only the runtime advisor handoff. This is called by the run
    /// manager on new game, load and menu return so an old suggestion can never
    /// leak into another route.
    /// </summary>
    public void ResetSession()
    {
        inviteSession.Reset();
        snapshot = null;
        advisorSelectedOption = -1;
        if (inviteOverlay != null) inviteOverlay.SetActive(false);
        SetAdvisorPanels(false, false);
        RefreshDecisionInviteButton();
    }

    public string GetCurrentEchoSummary()
    {
        return inviteSession != null ? inviteSession.EchoSummary : string.Empty;
    }

    private void CreateAdvisorPanels()
    {
        advisorGuestPanel = CreateUIObject("AdvisorGuestPanel", invitePanel.transform);
        SetAnchored(advisorGuestPanel.GetComponent<RectTransform>(), new Vector2(.04f, .14f), new Vector2(.96f, .84f));
        Image guestPanelImage = advisorGuestPanel.AddComponent<Image>();
        guestPanelImage.color = new Color(.06f, .095f, .125f, .995f);
        // The full-screen invite overlay is the modal blocker. Let child
        // controls receive pointer events instead of allowing this decorative
        // panel background to become a stale raycast target after reopening.
        guestPanelImage.raycastTarget = false;

        TMP_Text guestTitle = CreateText(advisorGuestPanel.transform, "应邀加入", 42f,
            TextAlignmentOptions.Left, textPrimary);
        SetAnchored(guestTitle.rectTransform, new Vector2(.04f, .88f), new Vector2(.96f, .99f));

        advisorCodeInput = CreateInput(advisorGuestPanel.transform, "本机接力码", 30f);
        SetAnchored(advisorCodeInput.GetComponent<RectTransform>(), new Vector2(.04f, .77f), new Vector2(.34f, .85f));
        advisorNameInput = CreateInput(advisorGuestPanel.transform, "参谋昵称", 30f);
        SetAnchored(advisorNameInput.GetComponent<RectTransform>(), new Vector2(.04f, .66f), new Vector2(.34f, .74f));

        TMP_Text optionTitle = CreateText(advisorGuestPanel.transform, "选择建议", 32f,
            TextAlignmentOptions.Left, accentColor);
        SetAnchored(optionTitle.rectTransform, new Vector2(.40f, .72f), new Vector2(.94f, .80f));
        TMP_Text reasonTitle = CreateText(advisorGuestPanel.transform, "一句理由", 32f,
            TextAlignmentOptions.Left, accentColor);
        SetAnchored(reasonTitle.rectTransform, new Vector2(.04f, .53f), new Vector2(.34f, .61f));
        advisorReasonInput = CreateInput(advisorGuestPanel.transform, "我建议这样做，因为……", 29f);
        SetAnchored(advisorReasonInput.GetComponent<RectTransform>(), new Vector2(.04f, .28f), new Vector2(.34f, .50f));
        advisorReasonInput.lineType = TMP_InputField.LineType.MultiLineNewline;

        advisorGuestStatusText = CreateText(advisorGuestPanel.transform, "", 26f,
            TextAlignmentOptions.Left, textMuted);
        SetAnchored(advisorGuestStatusText.rectTransform, new Vector2(.04f, .14f), new Vector2(.94f, .22f));

        Button submit = CreateButton(advisorGuestPanel.transform, "提交建议", new Vector2(230f, 64f),
            Vector2.zero, new Color(.44f, .32f, .22f, 1f));
        SetAnchored(submit.GetComponent<RectTransform>(), new Vector2(.72f, .04f), new Vector2(.95f, .14f));
        submit.onClick.AddListener(SubmitAdvisorSuggestion);

        Button back = CreateButton(advisorGuestPanel.transform, "返回快照", new Vector2(230f, 64f),
            Vector2.zero, new Color(.24f, .28f, .29f, 1f));
        SetAnchored(back.GetComponent<RectTransform>(), new Vector2(.46f, .04f), new Vector2(.69f, .14f));
        back.onClick.AddListener(CloseGuestAdvisorPanel);

        advisorReviewPanel = CreateUIObject("AdvisorReviewPanel", invitePanel.transform);
        SetAnchored(advisorReviewPanel.GetComponent<RectTransform>(), new Vector2(.04f, .14f), new Vector2(.96f, .84f));
        Image reviewPanelImage = advisorReviewPanel.AddComponent<Image>();
        reviewPanelImage.color = new Color(.06f, .095f, .125f, .995f);
        reviewPanelImage.raycastTarget = false;
        TMP_Text reviewTitle = CreateText(advisorReviewPanel.transform, "主将审核 · 共谋回声", 42f,
            TextAlignmentOptions.Left, textPrimary);
        SetAnchored(reviewTitle.rectTransform, new Vector2(.04f, .88f), new Vector2(.96f, .99f));
        advisorReviewText = CreateText(advisorReviewPanel.transform, "", 34f,
            TextAlignmentOptions.Left, textPrimary);
        advisorReviewText.enableWordWrapping = true;
        advisorReviewText.overflowMode = TextOverflowModes.Overflow;
        SetAnchored(advisorReviewText.rectTransform, new Vector2(.06f, .28f), new Vector2(.94f, .84f));

        Button accept = CreateButton(advisorReviewPanel.transform, "采纳建议", new Vector2(220f, 64f),
            Vector2.zero, new Color(.44f, .32f, .22f, 1f));
        SetAnchored(accept.GetComponent<RectTransform>(), new Vector2(.69f, .04f), new Vector2(.95f, .14f));
        accept.onClick.AddListener(AcceptAdvisorSuggestion);
        advisorAcceptButton = accept;

        Button decline = CreateButton(advisorReviewPanel.transform, "拒绝建议", new Vector2(220f, 64f),
            Vector2.zero, new Color(.30f, .20f, .18f, 1f));
        SetAnchored(decline.GetComponent<RectTransform>(), new Vector2(.44f, .04f), new Vector2(.67f, .14f));
        decline.onClick.AddListener(DeclineAdvisorSuggestion);
        advisorDeclineButton = decline;

        Button withdraw = CreateButton(advisorReviewPanel.transform, "撤销邀约", new Vector2(220f, 64f),
            Vector2.zero, new Color(.24f, .28f, .29f, 1f));
        SetAnchored(withdraw.GetComponent<RectTransform>(), new Vector2(.19f, .04f), new Vector2(.42f, .14f));
        withdraw.onClick.AddListener(WithdrawAdvisorInvitation);

        SetAdvisorPanels(false, false);
    }

    private void OpenGuestAdvisorPanel()
    {
        if (inviteSession == null || !inviteSession.IsActive) return;
        ClearSelectedUi();

        // Re-enable the guest panel before touching its TMP fields. When this
        // panel is reopened after being hidden, doing the reset while it is
        // inactive can leave TMP_InputField's internal activation state stale.
        SetAdvisorPanels(true, false);
        if (advisorGuestPanel != null) advisorGuestPanel.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        ResetAdvisorInput(advisorCodeInput, inviteSession.InviteCode);
        ResetAdvisorInput(advisorNameInput, string.Empty);
        ResetAdvisorInput(advisorReasonInput, string.Empty);
        advisorSelectedOption = -1;
        PopulateAdvisorOptions();
        if (advisorGuestStatusText != null)
            advisorGuestStatusText.text = "";
        if (titleText != null) titleText.text = "应邀加入";
    }

    private void CloseGuestAdvisorPanel()
    {
        ClearSelectedUi();
        SetAdvisorPanels(false, false);
        if (titleText != null) titleText.text = "军议邀约";
        Canvas.ForceUpdateCanvases();
    }

    private void PopulateAdvisorOptions()
    {
        for (int i = advisorOptionButtons.Count - 1; i >= 0; i--)
            if (advisorOptionButtons[i] != null) Destroy(advisorOptionButtons[i].gameObject);
        advisorOptionButtons.Clear();
        int count = inviteSession != null && inviteSession.OptionTexts != null
            ? inviteSession.OptionTexts.Count : 0;
        int visibleCount = Mathf.Min(count, 3);
        for (int i = 0; i < visibleCount; i++)
        {
            int optionIndex = i;
            Button optionButton = CreateButton(advisorGuestPanel.transform,
                (i + 1) + ". " + inviteSession.OptionTexts[i], new Vector2(0f, 0f),
                Vector2.zero, new Color(.18f, .24f, .26f, 1f));
            float top = .64f - i * .105f;
            SetAnchored(optionButton.GetComponent<RectTransform>(), new Vector2(.40f, top - .08f),
                new Vector2(.94f, top));
            optionButton.onClick.AddListener(() => SelectAdvisorOption(optionIndex));
            advisorOptionButtons.Add(optionButton);
        }
        if (count == 0 && advisorGuestStatusText != null)
            advisorGuestStatusText.text = "当前节点没有可提交的剧情选项。";
    }

    private void SelectAdvisorOption(int optionIndex)
    {
        advisorSelectedOption = optionIndex;
        for (int i = 0; i < advisorOptionButtons.Count; i++)
        {
            Image image = advisorOptionButtons[i] != null ? advisorOptionButtons[i].GetComponent<Image>() : null;
            if (image != null)
                image.color = i == optionIndex ? new Color(.55f, .42f, .22f, 1f) : new Color(.18f, .24f, .26f, 1f);
        }
    }

    private void SubmitAdvisorSuggestion()
    {
        if (inviteSession == null || !inviteSession.ValidateCode(advisorCodeInput != null ? advisorCodeInput.text : string.Empty))
        {
            if (advisorGuestStatusText != null) advisorGuestStatusText.text = "接力码无效或已过期，请回到主将快照重新发起。";
            return;
        }
        string guest = advisorNameInput != null ? advisorNameInput.text : string.Empty;
        string reason = advisorReasonInput != null ? advisorReasonInput.text : string.Empty;
        if (!inviteSession.SubmitSuggestion(guest, advisorSelectedOption, reason))
        {
            if (advisorGuestStatusText != null) advisorGuestStatusText.text = "请填写昵称并选择建议";
            return;
        }
        ShowAdvisorReviewPanel();
    }

    private void ShowAdvisorReviewPanel()
    {
        if (titleText != null) titleText.text = "主将审核";
        if (advisorReviewText != null)
        {
            InviteSuggestion suggestion = inviteSession != null ? inviteSession.Suggestion : null;
            if (suggestion == null)
            advisorReviewText.text = "尚未收到参谋建议。";
            else
            {
                string reason = string.IsNullOrWhiteSpace(suggestion.reason) ? "未填写" : suggestion.reason;
                advisorReviewText.text = "参谋：" + suggestion.guestLabel + "\n\n" +
                    "建议选项：" + suggestion.optionText + "\n\n" +
                    "理由：" + reason + "\n\n" +
                    (string.IsNullOrWhiteSpace(inviteSession.EchoSummary)
                        ? "请主将选择采纳或拒绝。采纳不会自动点击剧情选项。"
                        : inviteSession.EchoSummary);
            }
        }
        SetAdvisorPanels(false, true);
        bool reviewable = inviteSession != null && inviteSession.Status == InviteSessionStatus.GuestSubmitted;
        if (advisorAcceptButton != null) advisorAcceptButton.interactable = reviewable;
        if (advisorDeclineButton != null) advisorDeclineButton.interactable = reviewable;
    }

    private void AcceptAdvisorSuggestion()
    {
        if (inviteSession != null && inviteSession.AcceptSuggestion())
        {
            RecordAdvisorEcho(true);
            ShowAdvisorReviewPanel();
        }
    }

    private void DeclineAdvisorSuggestion()
    {
        if (inviteSession != null && inviteSession.DeclineSuggestion())
        {
            RecordAdvisorEcho(false);
            ShowAdvisorReviewPanel();
        }
    }

    private void RecordAdvisorEcho(bool accepted)
    {
        InviteSuggestion suggestion = inviteSession != null ? inviteSession.Suggestion : null;
        if (suggestion == null || RunHistoryTracker.Instance == null) return;
        RunHistoryTracker.Instance.RecordAdvisorEcho(inviteSession.NodeId,
            inviteSession.ChapterIndex, suggestion.optionIndex, suggestion.guestLabel,
            suggestion.optionText, suggestion.reason, accepted, inviteSession.EchoSummary);
    }

    private void WithdrawAdvisorInvitation()
    {
        inviteSession.Reset();
        snapshot = null;
        SetAdvisorPanels(false, false);
        if (titleText != null) titleText.text = "军议邀约";
        if (statusText != null) statusText.text = "邀约已撤销，可在当前决策点重新发起。";
        RefreshDecisionInviteButton();
    }

    private void SetAdvisorPanels(bool showGuest, bool showReview)
    {
        if (advisorGuestPanel != null) advisorGuestPanel.SetActive(showGuest);
        if (advisorReviewPanel != null) advisorReviewPanel.SetActive(showReview);
    }

    private static void ClearSelectedUi()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    private void CreateDecisionInviteButton(Transform parent)
    {
        decisionInviteButton = CreateUIObject("DecisionInviteButton", parent);
        RectTransform rect = decisionInviteButton.GetComponent<RectTransform>();
        // Keep the compact entry below the three decision choices and above
        // the dialogue paper, outside the dialogue text itself. This .44
        // anchor is only a fallback while the options are laying out; the
        // runtime alignment below replaces it with the measured option gap.
        rect.anchorMin = new Vector2(.5f, .44f);
        rect.anchorMax = new Vector2(.5f, .44f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        // Four Chinese characters only need a compact capsule. Keep the
        // height identical to the decision options while avoiding the long
        // empty sides that made this entry look oversized.
        rect.sizeDelta = new Vector2(300f, 80f);
        Image image = decisionInviteButton.AddComponent<Image>();
        decisionInviteImage = image;
        Sprite dialogueSprite = FindDialogueButtonSprite(parent);
        if (dialogueSprite != null)
        {
            image.sprite = dialogueSprite;
            // The source is an illustrated capsule rather than a 9-slice
            // panel; keep its rounded corners and paper texture intact.
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }
        image.color = new Color(.93f, .91f, .84f, .70f);
        Outline outline = decisionInviteButton.AddComponent<Outline>();
        outline.effectColor = new Color(.78f, .57f, .22f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
        Button button = decisionInviteButton.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, .98f, .92f, .72f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(.91f, .82f, .62f, 1f);
        colors.selectedColor = new Color(.96f, .89f, .72f, 1f);
        colors.disabledColor = new Color(.72f, .70f, .65f, .55f);
        button.colors = colors;
        button.onClick.AddListener(OpenInvitePanel);
        // Match the option labels at runtime so this stays correct if a
        // chapter/theme changes their font size or weight.
        TMP_Text label = CreateText(decisionInviteButton.transform, "军议邀约", 40f,
            TextAlignmentOptions.Center, new Color(.16f, .11f, .07f, 1f));
        label.enableAutoSizing = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.Center;
        Stretch(label.rectTransform);
        label.transform.SetAsLastSibling();
        SyncDecisionInviteLabel(parent);
        AlignDecisionInviteButton(parent);
        decisionInviteButton.SetActive(false);
    }

    private void SyncDecisionInviteLabel(Transform parent)
    {
        if (decisionInviteButton == null || parent == null) return;
        TMP_Text inviteLabel = decisionInviteButton.GetComponentInChildren<TMP_Text>(true);
        RectTransform option = FindDecisionOption(parent, "OptionButton1");
        TMP_Text optionLabel = option != null ? option.GetComponentInChildren<TMP_Text>(true) : null;
        if (inviteLabel == null || optionLabel == null) return;
        inviteLabel.fontSize = optionLabel.fontSize;
        inviteLabel.fontSizeMin = optionLabel.fontSizeMin;
        inviteLabel.fontSizeMax = optionLabel.fontSizeMax;
        inviteLabel.fontStyle = optionLabel.fontStyle;
        inviteLabel.characterSpacing = optionLabel.characterSpacing;
        inviteLabel.lineSpacing = optionLabel.lineSpacing;
    }

    private void AlignDecisionInviteButton(Transform parent)
    {
        if (decisionInviteButton == null || parent == null) return;
        RectTransform option2 = FindDecisionOption(parent, "OptionButton2");
        RectTransform option3 = FindDecisionOption(parent, "OptionButton3");
        if (option2 == null || option3 == null) return;

        Canvas.ForceUpdateCanvases();
        Vector3 option2Center = GetRectWorldCenter(option2);
        Vector3 option3Center = GetRectWorldCenter(option3);
        float optionSpacing = Mathf.Abs(option2Center.y - option3Center.y);
        if (optionSpacing < 1f) return;

        RectTransform inviteRect = decisionInviteButton.GetComponent<RectTransform>();
        inviteRect.anchorMin = new Vector2(.5f, .5f);
        inviteRect.anchorMax = new Vector2(.5f, .5f);
        inviteRect.pivot = new Vector2(.5f, .5f);
        // UI Y grows upward, so subtract one option spacing to place the
        // invite immediately below option 3 with the same gap as options 2/3.
        inviteRect.position = new Vector3(option3Center.x,
            option3Center.y - optionSpacing, option3Center.z);
    }

    private static RectTransform FindDecisionOption(Transform root, string objectName)
    {
        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null && rects[i].name == objectName) return rects[i];
        }
        return null;
    }

    private static Vector3 GetRectWorldCenter(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * .5f;
    }

    private void Update()
    {
        if (decisionInviteImage == null || decisionInviteButton == null || !decisionInviteButton.activeSelf) return;
        Color current = decisionInviteImage.color;
        current.a = .54f + Mathf.Sin(Time.unscaledTime * 3.4f) * .14f;
        decisionInviteImage.color = current;
    }

    private void LateUpdate()
    {
        // Dialogue options can be activated/repositioned after the node event
        // fires. Retry alignment while visible so every chapter lands at the
        // same measured edge-to-edge gap instead of keeping the fallback.
        if (decisionInviteButton != null && decisionInviteButton.activeSelf)
        {
            SyncDecisionInviteLabel(decisionInviteButton.transform.parent);
            AlignDecisionInviteButton(decisionInviteButton.transform.parent);
        }
    }

    private static string StripChapterPrefix(string value)
    {
        if (string.IsNullOrEmpty(value)) return "暂无";
        return Regex.Replace(value, @"\d+幕\s*[：:]\s*", string.Empty);
    }

    private static Sprite FindDialogueButtonSprite(Transform parent)
    {
        if (parent == null) return null;
        Button[] buttons = parent.GetComponentsInChildren<Button>(true);
        // Prefer the actual story-choice button so this entry point inherits
        // the same rounded, paper-like silhouette as the decision UI.
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || buttons[i].gameObject == null ||
                buttons[i].gameObject.name.IndexOf("OptionButton", StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            Image candidate = buttons[i].GetComponent<Image>();
            if (candidate != null && candidate.sprite != null)
                return candidate.sprite;
        }
        for (int i = 0; i < buttons.Length; i++)
        {
            Image candidate = buttons[i].GetComponent<Image>();
            if (candidate != null && candidate.sprite != null)
                return candidate.sprite;
        }
        return null;
    }

    public bool ShowPlayerBrief(Action onBegin)
    {
        if (!initialized || playerBrief == null || onBegin == null) return false;
        beginButton.onClick.RemoveAllListeners();
        beginButton.onClick.AddListener(() =>
        {
            playerBrief.SetActive(false);
            onBegin();
        });
        playerBrief.SetActive(true);
        playerBrief.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        briefScroll.verticalNormalizedPosition = 1f;
        return true;
    }

    private void CreatePlayerBrief(Transform parent)
    {
        playerBrief = CreateUIObject("PlayerIdentityBrief", parent);
        Stretch(playerBrief.GetComponent<RectTransform>());
        playerBrief.AddComponent<Image>().color = overlayColor;
        GameObject panel = CreateUIObject("BriefPanel", playerBrief.transform);
        SetAnchored(panel.GetComponent<RectTransform>(), new Vector2(.08f, .06f), new Vector2(.92f, .94f));
        panel.AddComponent<Image>().color = panelColor;
        TMP_Text title = CreateText(panel.transform, "出征之前", 48f, TextAlignmentOptions.Left, textPrimary);
        SetHeader(title.rectTransform);
        Button back = CreateButton(panel.transform, "返回", new Vector2(180f, 72f),
            new Vector2(-30f, -26f), new Color(.30f, .20f, .18f));
        back.onClick.AddListener(() => playerBrief.SetActive(false));
        Transform content = CreateScrollContent(panel.transform, out briefScroll);
        CreateWrappedText(content, "你将扮演曹操 · 曹军主将", 44f, accentColor);
        CreateWrappedText(content, "官渡两军相持，你要在粮草、兵力与风险之间作出取舍。\n" +
            "你决定曹军如何应战，选择会改变资源和后续剧情。", 38f, textPrimary);
        CreateWrappedText(content, "七条分支结局，关键决策可邀友参谋", 44f, accentColor);
        CreateWrappedText(content, "全作共有七条结局线，也有等待发现的彩蛋。\n" +
            "在需要拍板的节点，你可以打开“军议邀约”，请朋友针对当前战况给出建议。", 38f, textPrimary);
        CreateWrappedText(content, "人物与路线", 44f, accentColor);
        CreateWrappedText(content, "许攸、袁绍是这场战役的重要人物。当前版本只提供曹操视角，" +
            "尚未开放许攸或袁绍的独立可玩路线。\n" +
            "部分结局为游戏的架空推演，不能视为史实。", 38f, textPrimary);
        beginButton = CreateButton(panel.transform, "以曹操身份出征", new Vector2(370f, 72f),
            Vector2.zero, new Color(.44f, .32f, .22f));
        SetBottomRight(beginButton.GetComponent<RectTransform>(), new Vector2(-30f, 30f), new Vector2(370f, 72f));
        playerBrief.SetActive(false);
    }

    private static void SetHeader(RectTransform rect)
    {
        SetAnchored(rect, new Vector2(0f, 1f), new Vector2(1f, 1f));
        rect.offsetMin = new Vector2(42f, -102f);
        rect.offsetMax = new Vector2(-240f, -26f);
    }

    private static Transform CreateScrollContent(Transform parent, out ScrollRect scroll)
    {
        GameObject viewport = CreateUIObject("ScrollableBody", parent);
        RectTransform rect = viewport.GetComponent<RectTransform>();
        Stretch(rect);
        rect.offsetMin = new Vector2(42f, 126f);
        rect.offsetMax = new Vector2(-66f, -128f);
        // An almost transparent graphic receives drag events over text/gaps.
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, .001f);
        viewport.AddComponent<RectMask2D>();
        scroll = viewport.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 55f;
        scroll.viewport = rect;
        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 22f;
        layout.padding = new RectOffset(0, 12, 6, 18);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;

        GameObject bar = CreateUIObject("ScrollBar", parent);
        SetAnchored(bar.GetComponent<RectTransform>(), Vector2.right, Vector2.one);
        bar.GetComponent<RectTransform>().offsetMin = new Vector2(-42f, 126f);
        bar.GetComponent<RectTransform>().offsetMax = new Vector2(-26f, -128f);
        bar.AddComponent<Image>().color = new Color(.2f, .25f, .27f, .7f);
        GameObject handle = CreateUIObject("Handle", bar.transform);
        Stretch(handle.GetComponent<RectTransform>());
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(.65f, .56f, .35f, 1f);
        Scrollbar scrollbar = bar.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollbar.targetGraphic = handleImage;
        scroll.verticalScrollbar = scrollbar;
        return content.transform;
    }

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static TMP_Text CreateText(Transform parent, string value, float size,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateUIObject("Text", parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF");
        if (font != null)
        {
            text.font = font;
            if (font.material != null) text.fontSharedMaterial = font.material;
        }
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static TMP_Text CreateWrappedText(Transform parent, string value, float size, Color color)
    {
        TMP_Text text = CreateText(parent, value, size, TextAlignmentOptions.Left, color);
        text.enableWordWrapping = true;
        text.verticalAlignment = VerticalAlignmentOptions.Top;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = false;
        return text;
    }

    private static TMP_InputField CreateInput(Transform parent, string placeholder, float size)
    {
        GameObject fieldObject = CreateUIObject("Input", parent);
        Image background = fieldObject.AddComponent<Image>();
        background.color = new Color(.12f, .17f, .19f, 1f);
        TMP_InputField field = fieldObject.AddComponent<TMP_InputField>();
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.contentType = TMP_InputField.ContentType.Standard;
        field.targetGraphic = background;
        field.interactable = true;
        field.readOnly = false;
        background.raycastTarget = true;
        // Make focus visible immediately after a mouse click.  The default
        // TMP caret is only one pixel wide and inherits the text colour,
        // which is effectively lost against this dark panel at game scale.
        // A slightly wider accent caret keeps the normal blink but gives the
        // player an unambiguous editing-state cue.
        field.customCaretColor = true;
        field.caretColor = new Color(.96f, .82f, .42f, 1f);
        field.caretBlinkRate = .85f;
        field.caretWidth = 2;
        field.selectionColor = new Color(.89f, .71f, .32f, .32f);

        // TMP_InputField normally wires its own pointer handler, but the
        // invite panel is rebuilt/reparented while the modal overlay changes
        // sibling order.  Re-activate the field explicitly on click so a
        // second open cannot leave the input looking enabled while the
        // EventSystem still has no selected control.
        UnityEngine.EventSystems.EventTrigger trigger =
            fieldObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        UnityEngine.EventSystems.EventTrigger.Entry clickEntry =
            new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick
            };
        clickEntry.callback.AddListener(_ =>
        {
            if (field == null || !field.interactable || field.readOnly) return;
            field.Select();
            field.ActivateInputField();
            field.caretPosition = field.text != null ? field.text.Length : 0;
        });
        trigger.triggers.Add(clickEntry);

        GameObject textObject = CreateUIObject("Text", fieldObject.transform);
        SetAnchored(textObject.GetComponent<RectTransform>(), new Vector2(.04f, .02f), new Vector2(.96f, .98f));
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF");
        if (font != null)
        {
            text.font = font;
            if (font.material != null) text.fontSharedMaterial = font.material;
        }
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        field.textComponent = text;

        GameObject placeholderObject = CreateUIObject("Placeholder", fieldObject.transform);
        SetAnchored(placeholderObject.GetComponent<RectTransform>(), new Vector2(.04f, .02f), new Vector2(.96f, .98f));
        TextMeshProUGUI placeholderText = placeholderObject.AddComponent<TextMeshProUGUI>();
        if (font != null) placeholderText.font = font;
        placeholderText.text = placeholder;
        placeholderText.fontSize = size;
        placeholderText.color = new Color(.62f, .68f, .69f, 1f);
        placeholderText.alignment = TextAlignmentOptions.Left;
        placeholderText.raycastTarget = false;
        placeholderText.enableWordWrapping = false;
        field.placeholder = placeholderText;
        return field;
    }

    private static void ResetAdvisorInput(TMP_InputField field, string value)
    {
        if (field == null) return;
        if (!field.gameObject.activeSelf) field.gameObject.SetActive(true);
        field.enabled = true;
        field.interactable = true;
        field.readOnly = false;
        field.gameObject.transform.SetAsLastSibling();
        if (field.targetGraphic == null)
            field.targetGraphic = field.GetComponent<Graphic>();
        field.DeactivateInputField();
        field.SetTextWithoutNotify(value ?? string.Empty);
        field.caretPosition = field.text.Length;
        field.selectionAnchorPosition = field.caretPosition;
        field.selectionFocusPosition = field.caretPosition;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 size,
        Vector2 topRightOffset, Color color)
    {
        GameObject buttonObject = CreateUIObject(label + "Button", parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetTopRight(rect, topRightOffset, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = CreateText(buttonObject.transform, label, 38f,
            TextAlignmentOptions.Center, Color.white);
        Stretch(text.rectTransform);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(offset.x, offset.y);
        rect.sizeDelta = size;
    }

    private static void SetTopRight(RectTransform rect, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(offset.x, offset.y);
        rect.sizeDelta = size;
    }

    private static void SetBottomRight(RectTransform rect, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(offset.x, offset.y);
        rect.sizeDelta = size;
    }
}

public sealed class InviteSnapshotData
{
    public readonly int ChapterIndex;
    public readonly string ChapterTitle;
    public readonly int CurrentNodeId;
    public readonly string SelectedRouteSummary;
    public readonly int Troop;
    public readonly int Food;
    public readonly int Strategy;
    public readonly int Risk;
    public string StoryRecap { get; internal set; }
    public string DecisionContext { get; internal set; }

    public InviteSnapshotData(int chapterIndex, string chapterTitle, int currentNodeId,
        string selectedRouteSummary, int troop, int food, int strategy, int risk)
    {
        ChapterIndex = chapterIndex;
        ChapterTitle = chapterTitle;
        CurrentNodeId = currentNodeId;
        SelectedRouteSummary = selectedRouteSummary;
        Troop = troop;
        Food = food;
        Strategy = strategy;
        Risk = risk;
    }
}
