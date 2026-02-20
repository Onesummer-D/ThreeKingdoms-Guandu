using UnityEngine;
using UnityEngine.UI;

public class ImageSwitcherButton : MonoBehaviour
{
    public string sceneName = "造发石车";  // 这个按钮对应什么场景

    void Start()
    {
        // 找到场景里的 ImageSwitcher
        ImageSwitcher switcher = FindObjectOfType<ImageSwitcher>();
        Debug.Log("找到的 ImageSwitcher：" + (switcher != null ? "有" : "没有"));
        // 给这个按钮添加点击事件
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(() => {
            Debug.Log("按钮被点击，要切换到：" + sceneName);
            if (switcher != null)
            {
                Debug.Log("=== 按钮被点击 ===");
                switcher.SwitchTo(sceneName);
            }
            else
            {
                Debug.LogError("找不到 ImageSwitcher！");
            }
        });
    }
}