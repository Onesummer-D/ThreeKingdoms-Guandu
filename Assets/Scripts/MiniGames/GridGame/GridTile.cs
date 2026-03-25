using UnityEngine;
using UnityEngine.UI;

public class GridTile : MonoBehaviour
{
    [Header("格子坐标（在Inspector中手动设置）")]
    [Range(0, 3)]
    public int gridX; 

    [Range(0, 4)]
    public int gridY;

    [Header("格子类型")]
    public bool isEnemy = false;
    public bool isEndPoint = false;

    // 属性
    public int GridX => gridX;
    public int GridY => gridY;
    public bool IsEnemy => isEnemy;
    public bool IsEndPoint => isEndPoint;
    public bool IsOccupied { get; private set; }

    private GridGameManager manager;
    private Image image;
    private Button button;
    private Color originalColor;

    void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();

        if (image != null)
        {
            originalColor = image.color;
        }

        // 根据isEnemy设置颜色
        UpdateVisual();
    }

    void Start()
    {
        // 确保有Button组件
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        button.onClick.AddListener(OnClick);
    }

    public void SetManager(GridGameManager mgr)
    {
        manager = mgr;
    }

    void OnClick()
    {
        if (manager != null)
        {
            manager.OnTileClicked(this);
        }
        else
        {
            Debug.LogWarning($"[GridTile] 点击了({gridX},{gridY})但manager为null");
        }
    }

    public void EnableClick(bool enable)
    {
        if (button != null)
        {
            button.interactable = enable;
        }
    }

    public void SetHighlight(bool highlight)
    {
        if (image == null) return;

        if (highlight)
        {
            image.color = new Color(0.3f, 1, 0.3f, 0.8f); // 绿色高亮
        }
        else
        {
            UpdateVisual();
        }
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
        if (image != null && occupied)
        {
            image.color = new Color(1, 1, 1, 0); // 透明
        }
    }

    void UpdateVisual()
    {
        if (image == null) return;

        if (isEnemy)
        {
            image.color = new Color(1, 0.2f, 0.2f, 0.7f); // 红色半透明
        }
        else if (isEndPoint)
        {
            image.color = new Color(1, 0.8f, 0.2f, 0.7f); // 黄色终点
        }
        else
        {
            image.color = new Color(1, 1, 1, 0.3f); // 白色半透明
        }
    }

    // 在Scene视图中显示坐标（方便调试）
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // 显示坐标文字
        UnityEditor.Handles.Label(transform.position, $"({gridX},{gridY})");
#endif
    }
}