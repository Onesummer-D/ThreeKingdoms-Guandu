using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class PuzzleGameManager : MonoBehaviour
{
    [Header("拼图设置")]
    public List<PuzzlePiece> puzzlePieces;
    public Transform piecesContainer;
    public float snapDistance = 80f;

    [Header("完成效果")]
    public Image completedCarImage;      // 图一：淡入的成品图
    public Image finalResultImage;       // 图三：最终成品展示（带背景板效果）
    public TMP_Text successText;
    public float fadeInDuration = 0.5f;
    public float successTextDuration = 1f;  // 文字显示时间
    public float finalResultDuration = 3f;  // 图三显示时间

    [Header("剧情对接")]
    public GameObject puzzlePanel;
    public float returnDelay = 1.5f;

    // ✅ 正确位置数组（6个碎片的正确位置）
    private Vector2[] correctPositions = new Vector2[]
    {
        new Vector2(-259, -63),
        new Vector2(237, -178),
        new Vector2(-204, -321),
        new Vector2(171, 302),
        new Vector2(-15, -28),
        new Vector2(230, 78)
    };

    // ✅ 碎片大小数组
    private Vector2[] pieceSizes = new Vector2[]
    {
        new Vector2(500, 500),
        new Vector2(620, 850),
        new Vector2(500, 500),
        new Vector2(600, 500),
        new Vector2(1000, 1000),
        new Vector2(650, 500)
    };

    // ✅ 私有变量
    private System.Action<bool> onCompleteCallback;
    private int nextDialogueNodeId = 0;
    private int placedPieceCount = 0;
    private bool isCompleted = false;
    private Coroutine completeCoroutine;

    void Start()
    {
        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);

        InitializePieces();
    }

    // ✅ 启动拼图游戏（供外部调用）
    public void StartPuzzleGame(System.Action<bool> callback = null, int nextNodeId = 0)
    {
        Debug.Log("启动拼图游戏");

        onCompleteCallback = callback;
        nextDialogueNodeId = nextNodeId;

        if (puzzlePanel != null)
            puzzlePanel.SetActive(true);

        ResetPuzzle();
        ShufflePieces();
    }

    void InitializePieces()
    {
        placedPieceCount = 0;
        isCompleted = false;

        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            puzzlePieces[i].OnPiecePlaced -= OnPiecePlaced;
            puzzlePieces[i].OnPiecePlaced += OnPiecePlaced;

            puzzlePieces[i].correctPosition = correctPositions[i];
            RectTransform rect = puzzlePieces[i].GetComponent<RectTransform>();
            rect.sizeDelta = pieceSizes[i];
            puzzlePieces[i].SetManager(this);
        }

        if (completedCarImage != null)
            completedCarImage.gameObject.SetActive(false);

        if (finalResultImage != null)
            finalResultImage.gameObject.SetActive(false);

        if (successText != null)
            successText.gameObject.SetActive(false);
    }

    void ShufflePieces()
    {
        RectTransform containerRect = piecesContainer.GetComponent<RectTransform>();
        if (containerRect == null) return;

        float width = containerRect.rect.width;
        float height = containerRect.rect.height;

        if (width <= 0) width = 800;
        if (height <= 0) height = 600;

        foreach (var piece in puzzlePieces)
        {
            float randomX = Random.Range(-width * 0.4f, width * 0.4f);
            float randomY = Random.Range(-height * 0.3f, -height * 0.1f);
            piece.GetComponent<RectTransform>().anchoredPosition = new Vector2(randomX, randomY);
        }
    }

    void OnPiecePlaced()
    {
        placedPieceCount++;
        Debug.Log($"碎片放置进度: {placedPieceCount}/{puzzlePieces.Count}");

        if (placedPieceCount >= puzzlePieces.Count && !isCompleted)
        {
            isCompleted = true;
            completeCoroutine = StartCoroutine(PuzzleCompleteSequence());
        }
    }

    IEnumerator PuzzleCompleteSequence()
    {
        Debug.Log("🎉 拼图完成！");

        // 1. 等待1秒
        yield return new WaitForSeconds(1f);

        // 2. 显示文字 + 淡入成品图
        if (successText != null && completedCarImage != null)
        {
            HideAllPieces();
            yield return StartCoroutine(ShowSuccessWithCar());
        }

        // 3. 显示一段时间后切换图三
        yield return new WaitForSeconds(successTextDuration);

        // 4. 显示图三（最终成品）
        if (finalResultImage != null)
        {
            yield return StartCoroutine(ShowFinalResult());
            yield return new WaitForSeconds(finalResultDuration);
        }

        // 5. 返回剧情
        yield return new WaitForSeconds(returnDelay);
        ReturnToStory();
    }

    void HideAllPieces()
    {
        foreach (var piece in puzzlePieces)
        {
            piece.gameObject.SetActive(false);
        }
        Debug.Log("拼图碎片已隐藏");
    }

    IEnumerator ShowSuccessWithCar()
    {
        // 显示文字
        successText.gameObject.SetActive(true);
        successText.text = "发石车打造成功！";

        // 淡入文字
        Color textColor = successText.color;
        textColor.a = 0;
        successText.color = textColor;

        // 同时淡入completedcar
        completedCarImage.gameObject.SetActive(true);
        Color carColor = completedCarImage.color;
        carColor.a = 0;
        completedCarImage.color = carColor;

        float elapsed = 0;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeInDuration);

            textColor.a = alpha;
            successText.color = textColor;

            carColor.a = alpha;
            completedCarImage.color = carColor;

            yield return null;
        }

        textColor.a = 1f;
        successText.color = textColor;
        carColor.a = 1f;
        completedCarImage.color = carColor;

        Debug.Log("发石车打造成功！显示完成");
    }

    IEnumerator ShowFinalResult()
    {
        // 隐藏文字和图一
        if (successText != null)
            successText.gameObject.SetActive(false);

        if (completedCarImage != null)
            completedCarImage.gameObject.SetActive(false);

        // 显示图三
        finalResultImage.gameObject.SetActive(true);

        Color color = finalResultImage.color;
        color.a = 0;
        finalResultImage.color = color;

        float elapsed = 0;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeInDuration);
            finalResultImage.color = color;
            yield return null;
        }

        color.a = 1f;
        finalResultImage.color = color;

        Debug.Log("图三（最终成品）显示完成");
    }

    void ReturnToStory()
    {
        Debug.Log("返回剧情节点...");

        ResetPuzzle();

        if (onCompleteCallback != null)
        {
            onCompleteCallback.Invoke(true);
            onCompleteCallback = null;
        }
        else
        {
            if (GameCallbacks.Instance != null)
            {
                GameCallbacks.Instance.OnPuzzleGameCompleted(true);
            }
        }

        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);
    }

    void ResetPuzzle()
    {
        placedPieceCount = 0;
        isCompleted = false;

        // 恢复显示所有碎片
        foreach (var piece in puzzlePieces)
        {
            piece.gameObject.SetActive(true);
            piece.ResetPiece();
        }

        if (completedCarImage != null)
            completedCarImage.gameObject.SetActive(false);

        if (finalResultImage != null)
            finalResultImage.gameObject.SetActive(false);

        if (successText != null)
            successText.gameObject.SetActive(false);
    }

    public bool TrySnapPiece(PuzzlePiece piece)
    {
        float distance = Vector2.Distance(
            piece.GetComponent<RectTransform>().anchoredPosition,
            piece.correctPosition
        );

        if (distance < snapDistance)
        {
            piece.GetComponent<RectTransform>().anchoredPosition = piece.correctPosition;
            return true;
        }

        return false;
    }
}