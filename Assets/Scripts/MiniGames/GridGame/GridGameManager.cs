using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridGameManager : MonoBehaviour
{
    public static GridGameManager Instance { get; private set; }

    [Header("UI引用")]
    public GameObject gridGamePanel;
    public Image mapBase;
    public Text statusText;
    public Button confirmButton;
    private System.Action<bool> onGameFinished; // ✅ 新增：游戏结束回调（true=胜利, false=失败）

    [Header("玩家")]
    public GridPlayer player;

    [Header("手动放置的格子（在Inspector中拖拽15个格子）")]
    [Tooltip("按顺序：第1列3个，第2列5个，第3列5个？不对，应该是5行3列=15个")]
    public List<GridTile> manualTiles = new List<GridTile>();

    private GridTile[,] gridTiles; // [列,行] = [x,y]
    private GridTile selectedTile;
    private bool isGameActive = false;
    private bool isStartPositionSelected = false;

    // 地图尺寸
    private const int COLUMNS = 4; 
    private const int ROWS = 5;  

    void Awake()
    {
        Instance = this;
        Debug.Log("[GridGameManager] Awake 执行了!"); // 白色日志

        // 初始化数组
        gridTiles = new GridTile[COLUMNS, ROWS];
    }

    void Start()
    {
        Debug.Log("[GridGameManager] Start 执行了!");

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.onClick.AddListener(OnConfirmStartPosition);
        }

        // 注册手动放置的格子
        RegisterManualTiles();

        if (gridGamePanel != null)
        {
            gridGamePanel.SetActive(false);
            Debug.Log("[GridGameManager] 初始状态：隐藏面板");
        }
    }

    void RegisterManualTiles()
    {
        Debug.Log($"[GridGameManager] 注册手动格子，共 {manualTiles.Count} 个");

        // 清空数组
        for (int x = 0; x < COLUMNS; x++)
            for (int y = 0; y < ROWS; y++)
                gridTiles[x, y] = null;

        // 遍历手动放置的格子，根据它们设置的坐标注册
        foreach (var tile in manualTiles)
        {
            if (tile == null) continue;

            int x = tile.GridX;
            int y = tile.GridY;

            if (x >= 0 && x < COLUMNS && y >= 0 && y < ROWS)
            {
                gridTiles[x, y] = tile;
                tile.SetManager(this);
                Debug.Log($"[GridGameManager] 注册格子: ({x}, {y})");
            }
            else
            {
                Debug.LogWarning($"[GridGameManager] 格子坐标越界: ({x}, {y})");
            }
        }

        // 检查缺失的格子
        int missingCount = 0;
        for (int x = 0; x < COLUMNS; x++)
            for (int y = 0; y < ROWS; y++)
                if (gridTiles[x, y] == null) missingCount++;

        if (missingCount > 0)
        {
            Debug.LogWarning($"[GridGameManager] 还有 {missingCount} 个格子未放置！");
        }
    }


    // ✅ 修改方法签名，接受回调参数（默认null兼容旧代码）
    public void StartGridGame(System.Action<bool> callback = null)
    {
        onGameFinished = callback; // 保存回调
        Debug.Log("[GridGameManager] 开始走格子游戏！");

        // ✅ 确保面板显示
        if (gridGamePanel != null)
        {
            gridGamePanel.SetActive(true);
            Debug.Log("[GridGameManager] 显示走格子面板");
        }

        isGameActive = true;
        isStartPositionSelected = false;
        selectedTile = null;

        if (statusText != null)
        {
            statusText.text = "请选择第1列（最左列）的任意格子作为起点";
        }

        // 重置所有格子状态
        foreach (var tile in manualTiles)
        {
            if (tile != null)
            {
                tile.EnableClick(true);
                tile.SetHighlight(false);
                tile.SetOccupied(false);
            }
        }

        // 隐藏玩家标记
        if (player != null)
        {
            player.gameObject.SetActive(false);
        }

        Debug.Log("[GridGameManager] 游戏初始化完成，等待玩家选择起点");
    }

    // 被GridTile调用
    public void OnTileClicked(GridTile tile)
    {
        if (!isGameActive || isStartPositionSelected) return;

        // 只允许选择第1列（x=0）
        if (tile.GridX != 0)
        {
            if (statusText != null)
                statusText.text = "只能从第1列（最左列）开始！请选择左侧的格子";
            return;
        }

        selectedTile = tile;

        // 高亮选中的格子，取消其他的高亮
        foreach (var t in manualTiles)
        {
            if (t != null) t.SetHighlight(t == tile);
        }

        if (statusText != null)
        {
            statusText.text = $"已选择起点: 第{tile.GridY + 1}行，点击确认开始";
        }

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
        }

        Debug.Log($"[GridGameManager] 选中起点: ({tile.GridX}, {tile.GridY})");
    }

    void OnConfirmStartPosition()
    {
        if (selectedTile == null) return;

        isStartPositionSelected = true;

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }

        // 禁用所有格子的点击
        foreach (var tile in manualTiles)
        {
            if (tile != null) tile.EnableClick(false);
        }

        // 隐藏选中的格子
        selectedTile.SetOccupied(true);

        // 激活玩家
        if (player != null)
        {
            player.gameObject.SetActive(true);

            Vector3 worldPos = selectedTile.transform.position;
            player.SetStartPosition(selectedTile.GridX, selectedTile.GridY, worldPos);
        }
        else
        {
            Debug.LogError("[GridGameManager] Player未赋值！请在Inspector中拖拽Player");
        }

        if (statusText != null)
        {
            statusText.text = "使用WASD移动，注意避开敌方！";
        }

        Debug.Log($"[GridGameManager] 游戏开始！玩家位置: ({selectedTile.GridX}, {selectedTile.GridY})");
    }

    public GridTile GetTileAt(int x, int y)
    {
        if (x < 0 || x >= COLUMNS || y < 0 || y >= ROWS) return null;
        return gridTiles[x, y];
    }

    public void OnPlayerMove(int x, int y)
    {
        GridTile tile = GetTileAt(x, y);
        if (tile == null) return;

        Debug.Log($"[GridGameManager] 玩家移动到: ({x}, {y})，敌人: {tile.IsEnemy}，终点: {tile.IsEndPoint}");

        // 检查是否是敌人
        if (tile.IsEnemy)
        {
            GameOver(false);
            return;
        }

        // 检查是否到达终点
        if (tile.IsEndPoint)
        {
            OnPlayerReachEnd();
            return;
        }

        // 检查是否到达第3列非敌人格（显示提示）
        if (x == 2 && !tile.IsEnemy)
        {
            if (statusText != null)
            {
                statusText.text = "已到达第3列！再按一次W键就胜利！";
            }
        }
    }

    public void OnPlayerReachEnd()
    {
        Debug.Log("[GridGameManager] 到达终点！胜利！");
        GameOver(true);
    }

    void GameOver(bool isWin)
    {
        isGameActive = false;

        if (isWin)
        {
            if (statusText != null)
                statusText.text = "🎉 胜利！成功奇袭乌巢！";
        }
        else
        {
            if (statusText != null)
                statusText.text = "💀 失败！被敌军发现！";
        }

        // ✅ 关键：调用回调通知 FinalUIManager（走格子失败处理）
        onGameFinished?.Invoke(isWin);

        // 调用回调通知剧情系统
        if (GameCallbacks.Instance != null)
        {
            GameCallbacks.Instance.OnGridGameCompleted(isWin);
        }

        StartCoroutine(ReturnToPlot(isWin));
    }

    IEnumerator ReturnToPlot(bool isWin)
    {
        yield return new WaitForSeconds(2f);

        Debug.Log("[GridGameManager] 返回剧情界面");

        // 1. 隐藏走格子面板
        if (gridGamePanel != null)
            gridGamePanel.SetActive(false);

        // 2. 调用FinalUIManager恢复UI
        if (FinalUIManager.Instance != null)
        {
            FinalUIManager.Instance.ReturnFromMiniGame();
        }

        // 3. 重置玩家状态
        if (player != null)
        {
            player.gameObject.SetActive(false);
            // 重置位置到屏幕外或初始位置
            player.transform.position = new Vector3(-1000, -1000, 0);
        }
    }

    public bool IsGameActive => isGameActive;
    public bool IsStartPositionSelected => isStartPositionSelected;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}