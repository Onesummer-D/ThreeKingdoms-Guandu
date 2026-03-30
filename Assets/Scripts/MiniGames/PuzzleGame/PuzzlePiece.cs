using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector]
    public Vector2 correctPosition;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private PuzzleGameManager manager;
    private bool isPlaced = false;
    private Canvas parentCanvas;  // 新增：用于计算缩放

    public System.Action OnPiecePlaced;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 新增：获取父级 Canvas
        parentCanvas = GetComponentInParent<Canvas>();
    }

    // 新增：替代原来的 Initialize，只设置 manager，不覆盖 correctPosition
    public void SetManager(PuzzleGameManager mgr)
    {
        manager = mgr;
    }

    // 保留 Initialize 方法但标记为过时，防止其他代码调用
    [System.Obsolete("使用 SetManager 代替，correctPosition 由 Manager 直接设置")]
    public void Initialize(PuzzleGameManager mgr)
    {
        SetManager(mgr);
    }

    public void ResetPiece()
    {
        isPlaced = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        // 新增：将碎片移到最前面，避免被遮挡
        transform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        // 关键修复：考虑 Canvas 缩放，让拖拽更跟手
        if (parentCanvas != null)
        {
            // Screen Space - Overlay 模式下，delta 需要除以 scaleFactor
            rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
        }
        else
        {
            rectTransform.anchoredPosition += eventData.delta;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        // 新增调试：查看当前位置和目标位置
        Debug.Log($"[{gameObject.name}] 当前: {rectTransform.anchoredPosition}, 目标: {correctPosition}, 距离: {Vector2.Distance(rectTransform.anchoredPosition, correctPosition)}");

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        // 尝试吸附
        if (manager != null && manager.TrySnapPiece(this))
        {
            isPlaced = true;
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;

            // 关键：吸附后锁定位置，防止后续拖拽
            rectTransform.anchoredPosition = correctPosition;

            // ========== 新增：播放拼图正确音 ==========
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPuzzleCorrect();
            // =======================================

            OnPiecePlaced?.Invoke();
            Debug.Log($"碎片 {gameObject.name} 已放置到正确位置！");
        }
        else
        {
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;
        }
    }
}