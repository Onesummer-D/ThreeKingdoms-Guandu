using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Explicit command-line QA only; never runs during normal editor use.
[InitializeOnLoad]
public static class HistorianChatSmoke
{
    private const string Pending = "HistorianChatSmoke.Pending";
    private static IEnumerator routine;
    private static double deadline;
    private static int checks;
    private static readonly List<string> evidence = new List<string>();
    private static int previewPhase;
    private static double previewUntil;
    private static int previewFrame;
    private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/2026-10-14-historian-chat"));

    static HistorianChatSmoke() { EditorApplication.playModeStateChanged += StateChanged; }

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        Environment.SetEnvironmentVariable("GUANDU_QA_SAVE_DIRECTORY", Path.Combine(Output, "qa-saves"));
        SessionState.SetBool(Pending, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Guanduuuu.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    // Interactive check for actual keyboard/IME input. Scene is never saved.
    public static void Preview()
    {
        Environment.SetEnvironmentVariable("GUANDU_QA_SAVE_DIRECTORY", Path.Combine(Output, "qa-saves"));
        SessionState.SetBool("HistorianChatSmoke.Preview", true);
        EditorSceneManager.OpenScene("Assets/Scenes/Guanduuuu.unity", OpenSceneMode.Single);
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Show();
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("HistorianChatSmoke.Preview", false))
        {
            SessionState.SetBool("HistorianChatSmoke.Preview", false);
            previewPhase = 0;
            EditorApplication.update += PreviewTick;
        }
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        deadline = EditorApplication.timeSinceStartup + 160;
        routine = Verify();
        EditorApplication.update += Tick;
    }

    private static void PreviewTick()
    {
        if (FinalUIManager.Instance == null || HistorianChatUI.Instance == null || DialogueSystem.Instance == null) return;
        if (previewPhase == 0)
        {
            FinalUIManager.Instance.StartReplay();
            previewPhase = 1;
            previewUntil = EditorApplication.timeSinceStartup + .5;
            previewFrame = Time.frameCount + 12;
            return;
        }
        if (EditorApplication.timeSinceStartup < previewUntil || Time.frameCount < previewFrame || DialogueSystem.Instance.CurrentNode == null) return;
        DialogueSystem.Instance.ShowDialogueNode(300201);
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.update -= PreviewTick;
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Runtime QA timed out");
            if (routine.MoveNext()) return;
            File.WriteAllLines(Path.Combine(Output, "unity-runtime-checks.txt"), evidence);
            Debug.Log("HISTORIAN_RUNTIME_PASS " + checks);
            Finish(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            File.WriteAllText(Path.Combine(Output, "unity-runtime-failure.txt"), error.ToString());
            Finish(1);
        }
    }

    private static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(Pending, false);
        EditorApplication.Exit(code);
    }

    private static T Field<T>(object owner, string name)
    {
        return (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }

    private static void Check(bool passed, string label)
    {
        if (!passed) throw new Exception("FAIL " + label);
        checks++;
        evidence.Add("PASS " + label);
        Debug.Log("HISTORIAN_QA " + label);
    }

    private static IEnumerator Verify()
    {
        while (FinalUIManager.Instance == null || HistorianChatUI.Instance == null || DialogueSystem.Instance == null) yield return null;
        FinalUIManager.Instance.StartReplay();
        while (DialogueSystem.Instance.CurrentNode == null) yield return null;
        DialogueSystem.Instance.ShowDialogueNode(300201);
        HistorianChatUI chat = HistorianChatUI.Instance;
        HistorianAgentClient client = chat.GetComponent<HistorianAgentClient>();
        string originalUrl = client.BaseUrl;
        client.BaseUrl = "http://127.0.0.1:18081";
        GameObject launcher = Field<GameObject>(chat, "launcher");
        Check(launcher.activeInHierarchy && launcher.GetComponent<Image>().sprite != null, "transparent sprite launcher exists in gameplay");
        Capture("launcher.png", 1920, 1080);
        Time.timeScale = .75f;
        launcher.GetComponent<Button>().onClick.Invoke();
        yield return null;
        Check(chat.IsOpen && Time.timeScale == 0f, "icon opens chat and pauses game");
        Check(FinalUIManager.Instance.dialogueText.GetComponent<CanvasGroup>().alpha == 0f, "paused story text does not overlap composer");
        TMP_InputField input = Field<TMP_InputField>(chat, "input");
        Button send = Field<Button>(chat, "send");
        Check(!send.interactable, "blank input cannot send");
        input.text = "许攸带来了什么信息？";
        Check(send.interactable && input.characterLimit == 800, "free Chinese text enables send");
        int node = DialogueSystem.Instance.CurrentNode.nodeId;
        typeof(FinalUIManager).GetMethod("OnClickAreaClicked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(FinalUIManager.Instance, null);
        Check(DialogueSystem.Instance.CurrentNode.nodeId == node, "underlying story input is blocked");
        send.onClick.Invoke();
        send.onClick.Invoke();
        Check(Field<bool>(chat, "busy") && !send.interactable, "pending request prevents duplicate send");
        chat.Close();
        Check(!chat.IsOpen && Mathf.Approximately(Time.timeScale, .75f), "close restores previous time scale");
        Check(FinalUIManager.Instance.dialogueText.GetComponent<CanvasGroup>().alpha == 1f, "closing chat restores original story text");
        chat.Open();
        while (Field<bool>(chat, "busy")) yield return null;
        List<HistorianChatMessage> history = Field<List<HistorianChatMessage>>(chat, "history");
        Check(history.Count == 2 && history[1].role == "assistant", "answer arrives across close and reopen");
        RectTransform messages = Field<RectTransform>(chat, "content");
        Button source = messages.GetChild(2).GetComponentInChildren<Button>();
        Check(source != null, "answer has expandable sources");
        source.onClick.Invoke();
        yield return null;
        Check(messages.GetChild(2).GetChild(messages.GetChild(2).childCount - 1).gameObject.activeSelf, "source button expands details");
        source.onClick.Invoke();
        input.text = "曹操为什么相信他？";
        send.onClick.Invoke();
        while (Field<bool>(chat, "busy")) yield return null;
        Check(history.Count == 4, "follow-up keeps ordered conversation history");
        int settledFrame = Time.frameCount + 6;
        while (Time.frameCount < settledFrame) yield return null;
        ScrollRect conversation = Field<ScrollRect>(chat, "scroll");
        Check(conversation.content.rect.height <= conversation.viewport.rect.height || Math.Abs(conversation.verticalNormalizedPosition) < .01f,
            "completed answer scrolls to latest message (position=" + conversation.verticalNormalizedPosition + ", content=" + conversation.content.rect.height + ", viewport=" + conversation.viewport.rect.height + ")");
        Capture("chat-1920.png", 1920, 1080);
        Capture("chat-1280.png", 1280, 720);
        Capture("chat-1024.png", 1024, 768);
        chat.Close();
        chat.Open();
        Check(Field<List<HistorianChatMessage>>(chat, "history").Count == 4, "same-run reopen retains messages");
        client.BaseUrl = "http://127.0.0.1:9";
        input.text = "乌巢为什么重要？";
        send.onClick.Invoke();
        while (Field<bool>(chat, "busy")) yield return null;
        Check(history[history.Count - 1].content.Contains("[H06]") && Field<TMP_Text>(chat, "status").text.Contains("史料摘录"), "offline request uses bundled cited knowledge in chat");
        input.text = "帮我写一个Python排序程序";
        send.onClick.Invoke();
        while (Field<bool>(chat, "busy")) yield return null;
        Check(history[history.Count - 1].content.Contains("不足"), "offline out-of-scope question refuses");
        client.BaseUrl = "http://127.0.0.1:18081";
        input.text = "延迟回答测试：乌巢为什么重要？";
        send.onClick.Invoke();
        chat.ResetConversation();
        double until = EditorApplication.timeSinceStartup + 1.5;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(history.Count == 0 && !Field<bool>(chat, "busy") && messages.childCount == 1, "reset cancels stale response without polluting new chat");
        input.text = "乌巢为什么重要？";
        send.onClick.Invoke();
        while (Field<bool>(chat, "busy")) yield return null;
        FinalUIManager.Instance.StartReplay();
        yield return null;
        Check(!chat.IsOpen && Field<List<HistorianChatMessage>>(chat, "history").Count == 0, "new run closes chat and clears old history");
        client.BaseUrl = originalUrl;
        Time.timeScale = 1f;
    }

    // Render the actual scene UI through a camera at several target resolutions.
    // No mock HTML or recreated layout; temporarily switch canvas rendering only.
    private static void Capture(string filename, int width, int height)
    {
        Canvas[] all = UnityEngine.Object.FindObjectsOfType<Canvas>();
        List<Canvas> roots = new List<Canvas>();
        foreach (Canvas canvas in all) if (canvas.isRootCanvas) roots.Add(canvas);
        GameObject cameraObject = new GameObject("Historian QA Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -100f);
        RenderTexture target = new RenderTexture(width, height, 24);
        camera.targetTexture = target;
        List<RenderMode> modes = new List<RenderMode>();
        List<Camera> cameras = new List<Camera>();
        foreach (Canvas canvas in roots)
        {
            modes.Add(canvas.renderMode); cameras.Add(canvas.worldCamera);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) { scaler.enabled = false; scaler.enabled = true; }
        }
        Canvas.ForceUpdateCanvases();
        foreach (Canvas canvas in roots) LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
        if (HistorianChatUI.Instance != null && HistorianChatUI.Instance.IsOpen)
            Field<ScrollRect>(HistorianChatUI.Instance, "scroll").verticalNormalizedPosition = 0f;
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(Path.Combine(Output, filename), image.EncodeToPNG());
        RenderTexture.active = previous;
        for (int i = 0; i < roots.Count; i++)
        {
            roots[i].renderMode = modes[i]; roots[i].worldCamera = cameras[i];
            CanvasScaler scaler = roots[i].GetComponent<CanvasScaler>();
            if (scaler != null) { scaler.enabled = false; scaler.enabled = true; }
        }
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        Check(File.Exists(Path.Combine(Output, filename)), "scene render " + filename);
    }
}
