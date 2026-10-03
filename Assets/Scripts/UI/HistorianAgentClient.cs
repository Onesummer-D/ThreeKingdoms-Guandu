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
    [SerializeField] private int timeoutSeconds = 8;

    public string BaseUrl { get { return baseUrl; } set { baseUrl = value ?? string.Empty; } }
    public bool IsConfigured { get { return !string.IsNullOrWhiteSpace(baseUrl); } }

    public void Ask(string question, string context, Action<HistorianAskResponse> completed,
        Action<string> failed)
    {
        if (!IsConfigured)
        {
            failed?.Invoke("史官服务地址未配置");
            return;
        }
        StartCoroutine(AskRoutine(question, context, completed, failed));
    }

    private IEnumerator AskRoutine(string question, string context,
        Action<HistorianAskResponse> completed, Action<string> failed)
    {
        HistorianAskRequest payload = new HistorianAskRequest
        {
            question = question ?? string.Empty,
            context = context ?? string.Empty
        };
        UnityWebRequest request = new UnityWebRequest(
            baseUrl.TrimEnd('/') + "/api/historian/ask", "POST");
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        request.uploadHandler = new UploadHandlerRaw(body);
        request.uploadHandler.contentType = "application/json";
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.Max(2, timeoutSeconds);
        request.SetRequestHeader("Accept", "application/json");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            failed?.Invoke(request.error ?? "史官服务不可用");
            request.Dispose();
            yield break;
        }
        HistorianAskResponse response = JsonUtility.FromJson<HistorianAskResponse>(request.downloadHandler.text);
        if (response == null || string.IsNullOrWhiteSpace(response.answer))
            failed?.Invoke("史官服务返回内容为空");
        else
            completed?.Invoke(response);
        request.Dispose();
    }
}
