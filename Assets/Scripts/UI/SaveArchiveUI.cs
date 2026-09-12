using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SaveArchiveUI : MonoBehaviour
{
    private readonly Color overlayColor = new Color(.015f, .025f, .035f, .94f);
    private readonly Color panelColor = new Color(.075f, .11f, .15f, .99f);
    private readonly Color rowColor = new Color(.11f, .16f, .20f, .98f);
    private readonly Color textPrimary = new Color(.94f, .94f, .88f, 1f);
    private readonly Color textMuted = new Color(.69f, .76f, .78f, 1f);
    private readonly Color accentColor = new Color(.89f, .71f, .32f, 1f);

    private FinalUIManager uiManager;
    private LocalSaveManager saveManager;
    private GameObject overlay;
    private GameObject homeButton;
    private TMP_Text statusText;
    private Sprite buttonSprite;
    private bool openedFromGameplay;
    private int pendingOverwriteSlot;
    private readonly List<SlotRow> rows = new List<SlotRow>();

    public void Initialize(FinalUIManager manager, LocalSaveManager saves,
        GameObject startMenuPanel, GameObject gameInterfacePanel)
    {
        if (uiManager != null || manager == null || saves == null || startMenuPanel == null || gameInterfacePanel == null)
            return;

        uiManager = manager;
        saveManager = saves;
        Button startButton = startMenuPanel.GetComponentInChildren<Button>(true);
        if (startButton != null)
        {
            Image sourceImage = startButton.GetComponent<Image>();
            if (sourceImage != null) buttonSprite = sourceImage.sprite;
            homeButton = CreateMenuButton(startMenuPanel.transform, "查看存档",
                new Vector2(-330f, -320f), new Vector2(300f, 90f), () => Open(false));
        }

        CreateOverlay(gameInterfacePanel.transform.root);
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
        if (homeButton != null) Destroy(homeButton);
    }

    private void Open(bool fromGameplay)
    {
        if (overlay == null) return;
        openedFromGameplay = fromGameplay;
        pendingOverwriteSlot = 0;
        if (statusText != null) statusText.text = "";
        RefreshRows();
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    private void Close()
    {
        pendingOverwriteSlot = 0;
        if (overlay != null) overlay.SetActive(false);
    }

    private void CreateOverlay(Transform root)
    {
        overlay = CreateUIObject("SaveArchiveOverlay", root);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.AddComponent<Image>().color = overlayColor;

        GameObject panel = CreateUIObject("SaveArchivePanel", overlay.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        SetAnchored(panelRect, new Vector2(.12f, .08f), new Vector2(.88f, .92f));
        panel.AddComponent<Image>().color = panelColor;

        TMP_Text title = CreateText(panel.transform, "军帐存档", 54f, TextAlignmentOptions.Left, textPrimary);
        SetAnchored(title.rectTransform, new Vector2(0f, .86f), new Vector2(.72f, .98f));
        title.rectTransform.offsetMin = new Vector2(42f, 0f);

        Button close = CreateButton(panel.transform, "关闭", 36f, new Vector2(170f, 68f),
            new Color(.30f, .20f, .18f, 1f), Color.white);
        SetTopRight(close.GetComponent<RectTransform>(), new Vector2(-30f, -24f), new Vector2(170f, 68f));
        close.onClick.AddListener(Close);

        statusText = CreateText(panel.transform, "", 30f, TextAlignmentOptions.Left, textMuted);
        SetAnchored(statusText.rectTransform, new Vector2(0f, .76f), new Vector2(1f, .84f));
        statusText.rectTransform.offsetMin = new Vector2(44f, 0f);
        statusText.rectTransform.offsetMax = new Vector2(-44f, 0f);

        CreateSlotRow(panel.transform, LocalSaveManager.AutoSlotId, 0, .64f, "自动检查点");
        CreateSlotRow(panel.transform, "slot1", 1, .48f, "存档 1");
        CreateSlotRow(panel.transform, "slot2", 2, .32f, "存档 2");
        CreateSlotRow(panel.transform, "slot3", 3, .16f, "存档 3");
        overlay.SetActive(false);
    }

    private void CreateSlotRow(Transform parent, string slotId, int manualIndex, float anchorY, string fallbackTitle)
    {
        GameObject row = CreateUIObject(slotId + "Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        SetAnchored(rowRect, new Vector2(.04f, anchorY), new Vector2(.96f, anchorY + .13f));
        row.AddComponent<Image>().color = rowColor;

        TMP_Text title = CreateText(row.transform, fallbackTitle, 34f, TextAlignmentOptions.Left, textPrimary);
        SetAnchored(title.rectTransform, new Vector2(.025f, .20f), new Vector2(.64f, .84f));
        TMP_Text detail = CreateText(row.transform, "", 29f, TextAlignmentOptions.Left, textMuted);
        SetAnchored(detail.rectTransform, new Vector2(.025f, .05f), new Vector2(.64f, .38f));

        Button save = null;
        if (manualIndex > 0)
        {
            save = CreateButton(row.transform, "保存", 28f, new Vector2(128f, 54f),
                new Color(.44f, .32f, .22f, 1f), textPrimary);
            SetAnchored(save.GetComponent<RectTransform>(), new Vector2(.66f, .23f), new Vector2(.79f, .77f));
            int captured = manualIndex;
            save.onClick.AddListener(() => SaveManual(captured));
        }

        Button load = CreateButton(row.transform, "读取", 28f, new Vector2(128f, 54f),
            new Color(.44f, .32f, .22f, 1f), textPrimary);
        SetAnchored(load.GetComponent<RectTransform>(), new Vector2(.82f, .23f), new Vector2(.95f, .77f));
        load.onClick.AddListener(() => Load(slotId));

        rows.Add(new SlotRow(slotId, manualIndex, fallbackTitle, title, detail, save, load));
    }

    private void RefreshRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            SlotRow row = rows[i];
            RunSaveData data;
            string message;
            bool valid = saveManager.TryLoadSlot(row.slotId, out data, out message);
            if (valid)
            {
                row.title.text = data.displayName;
                row.detail.text = FormatDetail(data);
                row.detail.gameObject.SetActive(true);
            }
            else if (saveManager.SlotExists(row.slotId))
            {
                row.title.text = row.fallbackTitle + " · 无法读取";
                row.detail.text = message;
                row.detail.gameObject.SetActive(true);
            }
            else
            {
                row.title.text = row.fallbackTitle + " · 空";
                row.detail.text = "";
                row.detail.gameObject.SetActive(false);
            }

            row.load.interactable = valid;
            if (row.save != null)
            {
                row.save.interactable = openedFromGameplay;
                TMP_Text label = row.save.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = valid ? "覆盖" : "保存";
            }
        }
    }

    private void SaveManual(int slotIndex)
    {
        string slotId = "slot" + slotIndex;
        if (saveManager.SlotExists(slotId) && pendingOverwriteSlot != slotIndex)
        {
            pendingOverwriteSlot = slotIndex;
            if (statusText != null) statusText.text = "再次点击“覆盖”，确认替换存档 " + slotIndex + "。";
            return;
        }

        string message;
        bool success = uiManager.SaveCurrentRunToSlot(slotIndex, out message);
        pendingOverwriteSlot = 0;
        if (statusText != null) statusText.text = message;
        if (success) RefreshRows();
    }

    private void Load(string slotId)
    {
        string message;
        if (uiManager.LoadRunFromSlot(slotId, out message))
        {
            Close();
            return;
        }
        if (statusText != null) statusText.text = message;
        RefreshRows();
    }

    private static string FormatDetail(RunSaveData data)
    {
        string time = "保存时间未知";
        DateTime parsed;
        if (DateTime.TryParse(data.savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out parsed))
            time = parsed.ToLocalTime().ToString("MM-dd HH:mm");
        return time + "　兵力 " + Mathf.RoundToInt(data.troop) + "　粮草 " + Mathf.RoundToInt(data.food) +
            "　计策 " + Mathf.RoundToInt(data.strategy) + "　风险 " + Mathf.RoundToInt(data.risk);
    }

    private GameObject CreateMenuButton(Transform parent, string label,
        Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject created = CreateButton(parent, label, 42f, size,
            Color.white, new Color(.25f, .18f, .12f, 1f)).gameObject;
        created.name = label + "Button";
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Button button = created.GetComponent<Button>();
        button.onClick.AddListener(action);
        return created;
    }

    private Button CreateButton(Transform parent, string label, float fontSize, Vector2 size,
        Color background, Color foreground)
    {
        GameObject buttonObject = CreateUIObject(label + "Button", parent);
        buttonObject.GetComponent<RectTransform>().sizeDelta = size;
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = buttonSprite != null ? Image.Type.Simple : Image.Type.Sliced;
        image.color = background;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = CreateText(buttonObject.transform, label, fontSize, TextAlignmentOptions.Center, foreground);
        Stretch(text.rectTransform);
        return button;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static TMP_Text CreateText(Transform parent, string value, float size,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateUIObject("Text", parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF");
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
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

    private static void SetTopRight(RectTransform rect, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
    }

    private static void SetBottomLeft(RectTransform rect, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
    }

    private sealed class SlotRow
    {
        public readonly string slotId;
        public readonly int manualIndex;
        public readonly string fallbackTitle;
        public readonly TMP_Text title;
        public readonly TMP_Text detail;
        public readonly Button save;
        public readonly Button load;

        public SlotRow(string slotId, int manualIndex, string fallbackTitle,
            TMP_Text title, TMP_Text detail, Button save, Button load)
        {
            this.slotId = slotId;
            this.manualIndex = manualIndex;
            this.fallbackTitle = fallbackTitle;
            this.title = title;
            this.detail = detail;
            this.save = save;
            this.load = load;
        }
    }
}
