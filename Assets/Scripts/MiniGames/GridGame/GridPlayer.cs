using UnityEngine;

public class GridPlayer : MonoBehaviour
{
    private int gridX = 0;
    private int gridY = 0;
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
        int newX = gridX;
        int newY = gridY;
        bool hasInput = false;

        if (Input.GetKeyDown(KeyCode.W))
        {
            newX += 1;
            hasInput = true;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            newX -= 1;
            hasInput = true;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            newY -= 1;
            hasInput = true;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            newY += 1;
            hasInput = true;
        }

        if (!hasInput) return;

        // 检查边界（4列5行）
        if (newX < 0 || newX >= 4 || newY < 0 || newY >= 5)
        {
            Debug.Log($"[GridPlayer] 移动超出边界: ({newX}, {newY})");
            return;
        }

        // 获取目标格子
        if (GridGameManager.Instance == null)
        {
            Debug.LogError("[GridPlayer] GridGameManager.Instance为null！");
            return;
        }

        GridTile targetTile = GridGameManager.Instance.GetTileAt(newX, newY);
        if (targetTile == null)
        {
            Debug.LogWarning($"[GridPlayer] 目标格子不存在: ({newX}, {newY})");
            return;
        }

        // 执行移动
        gridX = newX;
        gridY = newY;
        targetPosition = targetTile.transform.position;
        isMoving = true;

        Debug.Log($"[GridPlayer] 开始移动到: ({gridX}, {gridY})");

        // 通知Manager检查（包括终点判定）
        GridGameManager.Instance.OnPlayerMove(gridX, gridY);
    }

    public void SetStartPosition(int x, int y, Vector3 worldPosition)
    {
        gridX = x;
        gridY = y;
        transform.position = worldPosition;
        targetPosition = worldPosition;
        isActive = true;
        isMoving = false;

        // 确保在最上层显示
        transform.SetAsLastSibling();

        Debug.Log($"[GridPlayer] 起点设置完成: ({x}, {y})，Player已激活");
    }

    public void ResetPlayer()
    {
        isActive = false;
        isMoving = false;
        gameObject.SetActive(false);
        Debug.Log("[GridPlayer] 已重置");
    }
}