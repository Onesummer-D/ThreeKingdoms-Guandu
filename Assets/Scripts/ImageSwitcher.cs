using UnityEngine;
using UnityEngine.UI;

public class ImageSwitcher : MonoBehaviour
{
    [Header("背景")]
    public GameObject backgroundNormal;      // 东汉末年时局图
    public GameObject backgroundBattle;      // 战场背景图
    public GameObject backgroundTunnel;      // 地道内场图
    public GameObject backgroundFighting;    // 战斗时背景图
    public GameObject backgroundCourt;       // 朝廷背景图
    public GameObject backgroundFail;        // 失败场景背景

    [Header("人物")]
    public GameObject leftCharacter;          // 左侧人物位置
    public GameObject rightCharacter;         // 右侧人物位置
    public Sprite caoCao;
    public Sprite yuanShao;
    public Sprite xuYou;

    [Header("人物尺寸预设")]
    public Vector2 caoCaoSize = new Vector2(900, 850);    // 左侧曹操
    public Vector2 yuanShaoSize = new Vector2(900, 850);  // 右侧袁绍
    public Vector2 xuYouSize = new Vector2(800, 1000);    // 右侧许攸

    [Header("当前剧情场景")]
    public string currentScene = "造发石车";  // 默认值

    void Start()
    {
        // 确保开始时所有人物隐藏
        HideAllCharacters();
    }

    void SetLeftCharacter(Sprite sprite, Vector2 size)
    {
        leftCharacter.SetActive(true);
        Image img = leftCharacter.GetComponent<Image>();
        img.sprite = sprite;

        RectTransform rt = leftCharacter.GetComponent<RectTransform>();
        rt.sizeDelta = size;
    }

    void SetRightCharacter(Sprite sprite, Vector2 size)
    {
        rightCharacter.SetActive(true);
        Image img = rightCharacter.GetComponent<Image>();
        img.sprite = sprite;

        RectTransform rt = rightCharacter.GetComponent<RectTransform>();
        rt.sizeDelta = size;
    }

    void HideLeftCharacter()
    {
        leftCharacter.SetActive(false);
    }

    void HideRightCharacter()
    {
        rightCharacter.SetActive(false);
    }

    void HideAllCharacters()
    {
        leftCharacter.SetActive(false);
        rightCharacter.SetActive(false);
    }

    // 选项按钮直接调用这个
    public void OnOption1Clicked()
    {
        Debug.Log("选项1被点击，当前场景：" + currentScene);
        SwitchTo(currentScene);
    }

    public void OnOption2Clicked()
    {
        Debug.Log("选项2被点击，当前场景：" + currentScene);
        SwitchTo(currentScene);
    }

    public void OnOption3Clicked()
    {
        Debug.Log("选项3被点击，当前场景：" + currentScene);
        SwitchTo(currentScene);
    }

    // 真正的切换逻辑
    public void SwitchTo(string sceneName)
    {
        Debug.Log("=== ImageSwitcher.SwitchTo 被调用，场景：" + sceneName + " ===");
        // 先关掉所有背景
        if (backgroundNormal != null) backgroundNormal.SetActive(false);
        if (backgroundBattle != null) backgroundBattle.SetActive(false);
        if (backgroundTunnel != null) backgroundTunnel.SetActive(false);
        if (backgroundFighting != null) backgroundFighting.SetActive(false);
        if (backgroundCourt != null) backgroundCourt.SetActive(false);
        if (backgroundFail != null) backgroundFail.SetActive(false);

        // 关掉所有人物
        HideAllCharacters();

        // 根据场景名打开对应的
        switch (sceneName)
        {
            // 剧情点1：初战受挫
            case "造发石车":
                if (backgroundBattle != null) backgroundBattle.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                SetRightCharacter(yuanShao, yuanShaoSize);
                break;

            case "挖地道":
                if (backgroundTunnel != null) backgroundTunnel.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                break;

            case "佯装败退":
                if (backgroundBattle != null) backgroundBattle.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                break;

            // 剧情点2：荀彧来信
            case "荀彧来信":
                if (backgroundCourt != null) backgroundCourt.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                break;

            // 剧情点3：许攸夜访
            case "许攸夜访":
                if (backgroundBattle != null) backgroundBattle.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                SetRightCharacter(xuYou, xuYouSize);
                break;

            // 剧情点4：奇袭乌巢
            case "奇袭乌巢":
                if (backgroundFighting != null) backgroundFighting.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                break;

            // 剧情点5：乌巢激战
            case "乌巢激战":
                if (backgroundFighting != null) backgroundFighting.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                SetRightCharacter(yuanShao, yuanShaoSize);
                break;

            // 结局
            case "胜利结局":
                if (backgroundNormal != null) backgroundNormal.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                break;

            case "失败结局":
                if (backgroundFail != null) backgroundFail.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                SetRightCharacter(yuanShao, yuanShaoSize);
                break;

            case "半途而废":  // if线1
                if (backgroundFail != null) backgroundFail.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                break;

            case "错失良机":  // if线2
                if (backgroundFail != null) backgroundFail.SetActive(true);
                SetLeftCharacter(caoCao, caoCaoSize);
                SetRightCharacter(yuanShao, yuanShaoSize);
                break;
        }
    }
}