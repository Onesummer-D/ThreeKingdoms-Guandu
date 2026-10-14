using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public sealed class HistorianAskRequest
{
    public string question;
    public string context;
    public HistorianChatMessage[] history;
}

[Serializable]
public sealed class HistorianChatMessage
{
    public string role;
    public string content;
    public HistorianChatMessage(string role, string content) { this.role = role; this.content = content; }
}

[Serializable]
public sealed class HistorianCitation
{
    public string id;
    public string title;
    public string source;
}

[Serializable]
public sealed class HistorianAskResponse
{
    public bool ok;
    public string answer;
    public string mode;
    public string auditId;
    public string answerVersion;
    public string[] retrieved;
    public HistorianCitation[] citations;
}

/// <summary>
/// Optional client for the grounded historian endpoint. The caller receives
/// the answer and citations; it never changes the current route or resources.
/// </summary>
public sealed class HistorianAgentClient : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://81.70.40.146:8080";
    [SerializeField] private int timeoutSeconds = 12;
    private Coroutine pending;
    private UnityWebRequest activeRequest;

    public string BaseUrl { get { return baseUrl; } set { baseUrl = value ?? string.Empty; } }
    public bool IsConfigured { get { return !string.IsNullOrWhiteSpace(baseUrl); } }

    public void Ask(string question, string context, Action<HistorianAskResponse> completed,
        Action<string> failed)
    {
        Ask(question, context, null, completed, failed);
    }

    public void Ask(string question, string context, HistorianChatMessage[] history,
        Action<HistorianAskResponse> completed, Action<string> failed)
    {
        CancelPending();
        if (!IsConfigured)
        {
            failed?.Invoke("史官服务地址未配置");
            return;
        }
        pending = StartCoroutine(AskRoutine(question, context, history, completed, failed));
    }

    public void CancelPending()
    {
        if (pending != null) StopCoroutine(pending);
        pending = null;
        if (activeRequest != null) { activeRequest.Abort(); activeRequest.Dispose(); activeRequest = null; }
    }

    private void OnDisable() { CancelPending(); }

    private IEnumerator AskRoutine(string question, string context, HistorianChatMessage[] history,
        Action<HistorianAskResponse> completed, Action<string> failed)
    {
        HistorianAskRequest payload = new HistorianAskRequest
        {
            question = question ?? string.Empty,
            context = context ?? string.Empty,
            history = history ?? new HistorianChatMessage[0]
        };
        UnityWebRequest request = new UnityWebRequest(
            baseUrl.TrimEnd('/') + "/api/historian/ask", "POST");
        activeRequest = request;
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        request.uploadHandler = new UploadHandlerRaw(body);
        request.uploadHandler.contentType = "application/json";
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.Max(12, timeoutSeconds);
        request.SetRequestHeader("Accept", "application/json");
        yield return request.SendWebRequest();

        activeRequest = null;
        pending = null;
        // Dispose before invoking callbacks so a reset or retry cannot dispose twice.
        string responseBody = request.downloadHandler.text;
        string requestError = request.error;
        bool success = request.result == UnityWebRequest.Result.Success;
        request.Dispose();

        if (!success)
        {
            failed?.Invoke(requestError ?? "史官服务不可用");
            yield break;
        }
        HistorianAskResponse response = null;
        try { response = JsonUtility.FromJson<HistorianAskResponse>(responseBody); }
        catch (ArgumentException) { }
        if (response == null || !response.ok || string.IsNullOrWhiteSpace(response.answer))
            failed?.Invoke("史官服务返回内容为空");
        else
            completed?.Invoke(response);
    }
}
