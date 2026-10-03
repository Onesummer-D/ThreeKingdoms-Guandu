using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public sealed class PublicAdvisorCreateRequest
{
    public string runId;
    public int chapterIndex;
    public int nodeId;
    public string question;
    public string context;
    public PublicAdvisorResources resources;
    public string[] options;
}

[Serializable]
public sealed class PublicAdvisorResources
{
    public float troop;
    public float food;
    public float strategy;
    public float risk;
}

[Serializable]
public sealed class PublicAdvisorSessionResponse
{
    public bool ok;
    public string sessionId;
    public string state;
    public long expiresAt;
    public string joinUrl;
    public string qrUrl;
    public string hostToken;
    public int nextSuggestionId;
}

[Serializable]
public sealed class PublicAdvisorSuggestion
{
    public int id;
    public string guestLabel;
    public int optionIndex;
    public string optionText;
    public string reason;
    public string clientRequestId;
    public long createdAt;
}

[Serializable]
public sealed class PublicAdvisorSuggestionResponse
{
    public bool ok;
    public string state;
    public long expiresAt;
    public PublicAdvisorSuggestion[] suggestions;
}

/// <summary>
/// HTTPS polling transport for the public advisor relay. It owns no gameplay
/// decision; callers still review the suggestion locally before applying it.
/// An empty BaseUrl leaves the existing same-device handoff untouched.
/// </summary>
public sealed class PublicAdvisorClient : MonoBehaviour
{
    [SerializeField] private string baseUrl = "";
    [SerializeField] private float pollSeconds = 1.5f;
    [SerializeField] private int timeoutSeconds = 8;

    private string sessionId;
    private string hostToken;
    private int lastSuggestionId;
    private Coroutine pollRoutine;
    private bool requestInFlight;

    public event Action<PublicAdvisorSessionResponse> SessionCreated;
    public event Action<PublicAdvisorSuggestion> SuggestionReceived;
    public event Action<string> TransportError;
    public string BaseUrl { get { return baseUrl; } set { baseUrl = value ?? ""; } }
    public bool IsConfigured { get { return !string.IsNullOrWhiteSpace(baseUrl); } }
    public bool HasSession { get { return !string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(hostToken); } }
    public string JoinUrl { get; private set; }

    public void BeginSession(InviteSnapshotData snapshot, IList<string> optionTexts,
        string runId, string context)
    {
        if (!IsConfigured || snapshot == null || requestInFlight) return;
        StopSession(false);
        List<string> options = new List<string>();
        if (optionTexts != null)
            for (int i = 0; i < optionTexts.Count && i < 3; i++)
                if (!string.IsNullOrWhiteSpace(optionTexts[i])) options.Add(optionTexts[i]);
        if (options.Count == 0) return;

        PublicAdvisorCreateRequest request = new PublicAdvisorCreateRequest
        {
            runId = runId ?? string.Empty,
            chapterIndex = snapshot.ChapterIndex,
            nodeId = snapshot.CurrentNodeId,
            question = snapshot.DecisionContext ?? string.Empty,
            context = context ?? string.Empty,
            resources = new PublicAdvisorResources
            {
                troop = snapshot.Troop,
                food = snapshot.Food,
                strategy = snapshot.Strategy,
                risk = snapshot.Risk
            },
            options = options.ToArray()
        };
        StartCoroutine(CreateSessionRoutine(request));
    }

    public void SubmitDecision(bool accepted)
    {
        if (!HasSession || requestInFlight) return;
        StartCoroutine(DecisionRoutine(accepted));
    }

    public void DownloadQr(string qrUrl, Action<Texture2D> completed, Action<string> failed)
    {
        if (!HasSession || string.IsNullOrWhiteSpace(qrUrl))
        {
            failed?.Invoke("二维码地址为空");
            return;
        }
        StartCoroutine(DownloadQrRoutine(qrUrl, completed, failed));
    }

    public void StopSession(bool closeRemote = true)
    {
        if (pollRoutine != null) StopCoroutine(pollRoutine);
        pollRoutine = null;
        if (closeRemote && HasSession && !requestInFlight)
            StartCoroutine(CloseRoutine(sessionId, hostToken));
        sessionId = string.Empty;
        hostToken = string.Empty;
        JoinUrl = string.Empty;
        lastSuggestionId = 0;
    }

    private IEnumerator CreateSessionRoutine(PublicAdvisorCreateRequest request)
    {
        requestInFlight = true;
        string url = JoinUrlPath("/api/sessions");
        using (UnityWebRequest webRequest = MakeJsonRequest(url, "POST", JsonUtility.ToJson(request), ""))
        {
            yield return webRequest.SendWebRequest();
            requestInFlight = false;
            if (!IsSuccess(webRequest)) { ReportError(webRequest); yield break; }
            PublicAdvisorSessionResponse response = JsonUtility.FromJson<PublicAdvisorSessionResponse>(webRequest.downloadHandler.text);
            if (response == null || string.IsNullOrEmpty(response.sessionId) || string.IsNullOrEmpty(response.hostToken))
            {
                ReportError("公网会话返回内容不完整");
                yield break;
            }
            sessionId = response.sessionId;
            hostToken = response.hostToken;
            JoinUrl = response.joinUrl ?? string.Empty;
            lastSuggestionId = 0;
            SessionCreated?.Invoke(response);
            pollRoutine = StartCoroutine(PollRoutine());
        }
    }

    private IEnumerator PollRoutine()
    {
        while (HasSession)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(.5f, pollSeconds));
            if (!HasSession || requestInFlight) continue;
            requestInFlight = true;
            string url = JoinUrlPath("/api/sessions/" + Uri.EscapeDataString(sessionId) +
                "/suggestions?after=" + lastSuggestionId);
            using (UnityWebRequest webRequest = MakeJsonRequest(url, "GET", null, hostToken))
            {
                yield return webRequest.SendWebRequest();
                requestInFlight = false;
                if (!IsSuccess(webRequest)) { ReportError(webRequest); continue; }
                PublicAdvisorSuggestionResponse response = JsonUtility.FromJson<PublicAdvisorSuggestionResponse>(webRequest.downloadHandler.text);
                if (response == null || response.suggestions == null) continue;
                for (int i = 0; i < response.suggestions.Length; i++)
                {
                    PublicAdvisorSuggestion suggestion = response.suggestions[i];
                    if (suggestion == null || suggestion.id <= lastSuggestionId) continue;
                    lastSuggestionId = suggestion.id;
                    SuggestionReceived?.Invoke(suggestion);
                }
            }
        }
    }

    private IEnumerator DecisionRoutine(bool accepted)
    {
        requestInFlight = true;
        string url = JoinUrlPath("/api/sessions/" + Uri.EscapeDataString(sessionId) + "/decision");
        string body = "{\"accepted\":" + (accepted ? "true" : "false") + "}";
        using (UnityWebRequest webRequest = MakeJsonRequest(url, "POST", body, hostToken))
        {
            yield return webRequest.SendWebRequest();
            requestInFlight = false;
            if (!IsSuccess(webRequest)) ReportError(webRequest);
        }
    }

    private IEnumerator DownloadQrRoutine(string qrUrl, Action<Texture2D> completed,
        Action<string> failed)
    {
        using (UnityWebRequest request = new UnityWebRequest(JoinUrlPath(qrUrl), UnityWebRequest.kHttpVerbGET))
        {
            request.downloadHandler = new DownloadHandlerTexture(true);
            request.timeout = Mathf.Max(2, timeoutSeconds);
            if (!string.IsNullOrEmpty(hostToken)) request.SetRequestHeader("X-Host-Token", hostToken);
            request.SetRequestHeader("Accept", "image/png");
            yield return request.SendWebRequest();
            if (!IsSuccess(request))
            {
                failed?.Invoke(request.error ?? "二维码下载失败");
                yield break;
            }
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null) failed?.Invoke("二维码图片为空");
            else completed?.Invoke(texture);
        }
    }

    private IEnumerator CloseRoutine(string closingSessionId, string closingToken)
    {
        requestInFlight = true;
        string url = JoinUrlPath("/api/sessions/" + Uri.EscapeDataString(closingSessionId));
        using (UnityWebRequest webRequest = MakeJsonRequest(url, "DELETE", "{}", closingToken))
        {
            yield return webRequest.SendWebRequest();
            requestInFlight = false;
        }
    }

    private UnityWebRequest MakeJsonRequest(string url, string method, string body, string host)
    {
        UnityWebRequest request = new UnityWebRequest(url, method);
        if (body != null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.uploadHandler.contentType = "application/json";
        }
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.Max(2, timeoutSeconds);
        if (!string.IsNullOrEmpty(host)) request.SetRequestHeader("X-Host-Token", host);
        request.SetRequestHeader("Accept", "application/json");
        return request;
    }

    private string JoinUrlPath(string path)
    {
        return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
    }

    private static bool IsSuccess(UnityWebRequest request)
    {
        return request != null && request.result == UnityWebRequest.Result.Success;
    }

    private void ReportError(UnityWebRequest request)
    {
        string detail = request != null && !string.IsNullOrEmpty(request.error)
            ? request.error : "公网请求失败";
        ReportError(detail);
    }

    private void ReportError(string message)
    {
        TransportError?.Invoke(message);
    }
}
