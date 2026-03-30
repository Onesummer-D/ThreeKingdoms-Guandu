using UnityEngine;

public class GridPlayer : MonoBehaviour
{
    private int gridX = 0; // 行（0-3）
    private int gridY = 0; // 列（0-4）
    private bool isActive = false;

    [Header("移动设置")]
    public float moveSpeed = 10f;
    private Vector3 targetPosition;
    private bool isMoving = false;

    void Awake()
    {
        targetPosition = transform.position;
        Debug.Log("[GridPlayer] Awake: 初始化完成");
    }

    void Update()
    {
        if (!isActive) return;

        // 平滑移动
        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 1f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
            return;
        }

        // 检测WASD
        HandleInput();
    }

    void HandleInput()
    {
        int newRow = gridX;  // 行
        int newCol = gridY;  // 列
        bool hasInput = false;

        if (Input.GetKeyDown(KeyCode.W))
        {
            newRow += 1; // 向上走（第1行→第2行）
            hasInput = true;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            newRow -= 1;
            hasInput = true;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            newCol -= 1; // 向左走（第2列→第1列）
            hasInput = true;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            newCol += 1;
            hasInput = true;
        }

        if (!hasInput) return;
        // ========== 新增：走格子移动音效 ==========
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPuzzleMove();
        // =====================================


        // 检查边界：4行5列
        if (newRow < 0 || newRow >= 4 || newCol < 0 || newCol >= 5)
        {
            Debug.Log($"[GridPlayer] 移动超出边界: 第{newRow + 1}行, 第{newCol + 1}列");
            return;
        }

        // ✅ 修复1：GetTileAt(行, 列) - 顺序要对！
        GridTile targetTile = GridGameManager.Instance.GetTileAt(newRow, newCol);
        if (targetTile == null)
        {
            Debug.LogWarning($"[GridPlayer] 目标格子不存在: 第{newRow + 1}行, 第{newCol + 1}列");
            return;
        }

        // 执行移动
        gridX = newRow;
        gridY = newCol;
        targetPosition = targetTile.transform.position;
        isMoving = true;

        Debug.Log($"[GridPlayer] 开始移动到: 第{gridX + 1}行, 第{gridY + 1}列");

        // ✅ 修复2：OnPlayerMove(行, 列) - 顺序要对！
        GridGameManager.Instance.OnPlayerMove(gridX, gridY);
    }

    public void SetStartPosition(int x, int y, Vector3 worldPosition)
    {
        // x=行，y=列（来自Manager的GridX, GridY）
        gridX = x;
        gridY = y;
        transform.position = worldPosition;
        targetPosition = worldPosition;
        isActive = true;
        isMoving = false;

        // 确保在最上层显示
        transform.SetAsLastSibling();

        Debug.Log($"[GridPlayer] 起点设置完成: 第{x + 1}行, 第{y + 1}列，Player已激活");
    }

    public void ResetPlayer()
    {
        isActive = false;
        isMoving = false;
        gameObject.SetActive(false);
        Debug.Log("[GridPlayer] 已重置");
    }
}