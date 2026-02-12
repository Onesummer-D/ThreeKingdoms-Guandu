using UnityEngine;
using System.Collections;  // 为了用协程
using System.Collections.Generic;

public class PlotPointManager : MonoBehaviour
{
    // 单例模式（重要！这样其他脚本能找到这个管理器）
    public static PlotPointManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("PlotPointManager初始化完成");
        }
        else
        {
            Destroy(gameObject);  // 如果有重复就销毁
        }
    }

    void Start()
    {
        Debug.Log("PlotPointManager开始工作！");
        TestFunction();

        // ========== 新增：订阅GameCallbacks事件 ==========
        if (GameCallbacks.Instance != null)
        {
            // 正确订阅事件（注意事件名称加了Event后缀）
            GameCallbacks.Instance.OnPuzzleGameCompletedEvent += OnPuzzleGameCompleted;
            GameCallbacks.Instance.OnValueInputConfirmedEvent += OnValueInputConfirmed;
            Debug.Log("PlotPointManager已订阅GameCallbacks事件");
        }
        // =============================================
    }

    // ========== 新增：拼图游戏完成的事件处理方法 ==========
    private void OnPuzzleGameCompleted(bool success)
    {
        Debug.Log($"PlotPointManager收到拼图游戏完成事件：{(success ? "成功" : "失败")}");

        // 处理游戏逻辑
        if (success)
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ModifyTroop(1.0f);
                Debug.Log("拼图完成！兵力+1");
            }

            ShowPuzzleCompleteDialogue();
        }
    }

    private void ShowPuzzleCompleteDialogue()
    {
        // 使用你已有的DialogueSystem显示对话
        if (DialogueSystem.Instance != null)
        {
            // 假设你在GuanduDialogueData.asset中配置了节点1002
            DialogueSystem.Instance.ShowDialogueNode(1002);
            Debug.Log("触发拼图完成对话节点1002");
        }
        else
        {
            // 备用方案：直接通知FinalUIManager继续
            Debug.LogWarning("DialogueSystem未找到，使用备用方案");
            StartCoroutine(FallbackToFinalUIManager());
        }
    }

    private IEnumerator FallbackToFinalUIManager()
    {
        // 等待一小会儿，确保资源已更新
        yield return new WaitForSeconds(0.5f);

        // 查找FinalUIManager
        FinalUIManager finalUI = FindObjectOfType<FinalUIManager>();
        if (finalUI != null)
        {
            // 调用我们新增的继续方法
            finalUI.ContinueToPlotPoint2();
        }
    }

    // 测试函数
    void TestFunction()
    {
        Debug.Log("=== 测试PlotPointManager ===");
        Debug.Log("如果你的控制台显示这行，说明脚本运行正常！");

        // 测试调用其他管理器
        if (ResourceManager.Instance != null)
        {
            Debug.Log("找到了ResourceManager！");
            Debug.Log("当前兵力：" + ResourceManager.Instance.GetTroop());
        }
    }

    // === 剧情点1：初战受挫 ===
    public void HandleOptionA_Puzzle()
    {
        Debug.Log("【剧情点1-选项A】玩家选择了拼图游戏");

        // 这个方法现在只做简单记录，逻辑由事件处理
        Debug.Log("等待拼图游戏完成事件...");

        // 启动协程
        StartCoroutine(SimulatePuzzleGame());
    }

    // ========== 新增：数值输入的事件处理方法 ==========
    private void OnValueInputConfirmed(float value)
    {
        Debug.Log($"PlotPointManager收到数值输入事件：{value}");

        // 调用原有的埋伏处理方法
        HandleOptionC_Ambush(value);
    }

    // === 在OnDestroy()中取消订阅 ===
    void OnDestroy()
    {
        // ========== 新增：取消订阅GameCallbacks事件 ==========
        if (GameCallbacks.Instance != null)
        {
            GameCallbacks.Instance.OnPuzzleGameCompletedEvent -= OnPuzzleGameCompleted;
            GameCallbacks.Instance.OnValueInputConfirmedEvent -= OnValueInputConfirmed;
        }
        // ====================================================
    }

    IEnumerator SimulatePuzzleGame()
    {
        Debug.Log("开始拼图游戏...（模拟）");

        // 等待2秒，模拟游戏过程
        yield return new WaitForSeconds(2.0f);

        Debug.Log("拼图完成！兵力+1");

        // 调用ResourceManager加兵力
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyTroop(1.0f);
            Debug.Log("当前兵力：" + ResourceManager.Instance.GetTroop());
        }
    }

    public void HandleOptionB_DigTunnel()
    {
        Debug.Log("【剧情点1-选项B】玩家选择了挖掘地道");
        Debug.Log("粮草-1");

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyFood(-1.0f);
            Debug.Log("当前粮草：" + ResourceManager.Instance.GetFood());
        }
    }

    // 埋伏判定
    public void HandleOptionC_Ambush(float inputValue)
    {
        Debug.Log($"处理埋伏选项，输入值：{inputValue}");

        // 判定逻辑：合理范围1.0-3.0
        bool isSuccess = (inputValue >= 1.0f && inputValue <= 3.0f);

        string resultText;
        if (isSuccess)
        {
            resultText = "埋伏成功！袁军中计，损失惨重。";
            if (ResourceManager.Instance != null)
                ResourceManager.Instance.ModifyStrategy(1.0f);  // 计策+1
        }
        else
        {
            resultText = "埋伏失败！袁军识破了计谋。";
            if (ResourceManager.Instance != null)
                ResourceManager.Instance.ModifyStrategy(-1.0f);  // 计策-1
        }

        // 触发事件通知UI
        OnAmbushResultProcessed?.Invoke(isSuccess, resultText, inputValue);
    }

    // === 结局触发方法 ===
    void TriggerEnding(string ifLineId)
    {
        Debug.Log($"PlotPointManager触发结局：{ifLineId}");

        // 检查EndingManager是否存在
        if (EndingManager.Instance != null)
        {
            EndingManager.Instance.TriggerEnding(ifLineId);
        }
        else
        {
            Debug.LogWarning("EndingManager未找到，游戏可能还没初始化完成");
            Debug.Log($"模拟触发结局：{ifLineId}");
        }
    }

    // ========== 新增：选项C的结果事件 ==========
    // 这个事件用来通知UI“埋伏选项的处理结果”
    // 参数1: bool - 是否成功 (true=成功, false=失败)
    // 参数2: string - 要显示的结局文本
    // 参数3: float - 玩家输入的具体值
    public static event System.Action<bool, string, float> OnAmbushResultProcessed;
    // ===========================================

    // === 剧情点2：粮草将尽 ===
    public void HandlePlotPoint2_OptionA_Stay()
    {
        Debug.Log("【剧情点2-选项A】采纳文若之谏，坚守官渡");
        Debug.Log("→ 直接进入下一段剧情");
        // 这里可以触发下一个对话节点
        // DialogueSystem.Instance.ShowDialogueNode(下一个节点ID);
    }

    public void HandlePlotPoint2_OptionB_Retreat()
    {
        Debug.Log("【剧情点2-选项B】粮草乃根本，不如暂回许都");
        Debug.Log("→ 触发IF线1：半途而废结局");

        // 解锁IF线1
        if (GameData.Instance != null)
        {
            GameData.Instance.UnlockIfLine("IF1");
            Debug.Log("已解锁IF线1：半途而废");

            // 触发结局
            TriggerEnding("IF1");
        }
    }

    public void HandlePerspectiveSwitch_XuYou()
    {
        Debug.Log("=== 视角切换至许攸 ===");
        Debug.Log("文字阐述袁绍方内部矛盾...");
        Debug.Log("许攸因审配扣押其家人，又不受袁绍重用，心生去意...");
        Debug.Log("许攸决定投奔曹操...");
    }

    // === 剧情点3：许攸夜访 ===
    public void HandlePlotPoint3_OptionA_Honest()
    {
        Debug.Log("【剧情点3-选项A】实不相瞒，粮草已尽，请公教我");
        Debug.Log("→ 许攸感其诚恳，献上'火烧乌巢'之计");

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyStrategy(1.0f);
            Debug.Log("计策成功率 +1.0");
            Debug.Log("当前计策：" + ResourceManager.Instance.GetStrategy());
        }

        // 触发下一个对话节点
        // DialogueSystem.Instance.ShowDialogueNode(下一个节点ID);
    }

    public void HandlePlotPoint3_OptionB_Lie()
    {
        Debug.Log("【剧情点3-选项B】尚可支撑一年......半年......三月耳");
        Debug.Log("→ 许攸戳穿谎言，但仍献计");

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyStrategy(0.5f);
            Debug.Log("计策成功率 +0.5（因说谎减半）");
            Debug.Log("当前计策：" + ResourceManager.Instance.GetStrategy());
        }
    }

    public void HandlePlotPoint3_OptionC_Suspect()
    {
        Debug.Log("【剧情点3-选项C】此乃诱敌之计，推出斩首！");
        Debug.Log("→ 怀疑许攸，错失关键情报");

        // 检查是否触发IF线2
        CheckIfLine2_Condition();
    }

    // 检查IF线2触发条件
    void CheckIfLine2_Condition()
    {
        // 条件：计策成功率低于某个值
        if (ResourceManager.Instance != null && ResourceManager.Instance.GetStrategy() < 3.0f)
        {
            Debug.Log("⚠️ 触发IF线2：错失良机结局");

            // 解锁IF线2
            if (GameData.Instance != null)
            {
                GameData.Instance.UnlockIfLine("IF2");
            }

            if (IfLineManager.Instance != null)
            {
                IfLineManager.Instance.UnlockIfLine(IfLineManager.IfLineType.IF2_错失良机);
            }

            // 触发结局
            TriggerEnding("IF2");
        }
        else
        {
            Debug.Log("计策成功率足够，未触发IF线2");
        }
    }

    // === 剧情点4：奇袭乌巢 ===
    public void HandlePlotPoint4_OptionA_LeadPersonally()
    {
        Debug.Log("【剧情点4-选项A】孤当亲自前往，以振军威！");
        Debug.Log("→ 曹操亲自带队，士气大振但风险增加");

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyRisk(1.0f);
            Debug.Log("风险 +1.0");
        }

        // 触发分支选择
        Invoke("ShowPlotPoint4_Branches", 1.0f);
    }

    public void HandlePlotPoint4_OptionB_Delegate()
    {
        Debug.Log("【剧情点4-选项B】令徐晃、张辽等率精兵前往");
        Debug.Log("→ 曹操不带队，稳妥但士气一般");
        // 无特殊效果
    }

    void ShowPlotPoint4_Branches()
    {
        Debug.Log("=== 分支选择 ===");
        Debug.Log("1. 率五千精骑轻装速进");
        Debug.Log("2. 率一万步骑稳扎稳打");

        // 这里应该弹出UI让玩家选择
        // 暂时模拟选择分支1
        HandlePlotPoint4_Branch1();
    }

    void HandlePlotPoint4_Branch1()
    {
        Debug.Log("【分支1】率五千精骑轻装速进");
        Debug.Log("→ 需要躲过袁绍侦察兵（走格子小游戏）");

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyTroop(1.5f);
            ResourceManager.Instance.ModifyRisk(0.5f);
            Debug.Log("兵力 +1.5，风险 +0.5");
        }
    }

    void HandlePlotPoint4_Branch2()
    {
        Debug.Log("【分支2】率一万步骑稳扎稳打");
        Debug.Log("→ 稳扎稳打，但速度较慢");

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyTroop(1.0f);
            Debug.Log("兵力 +1.0");
        }
    }

    // === 剧情点5：乌巢激战 ===
    public void HandlePlotPoint5_OptionA_FocusOnWuchao()
    {
        Debug.Log("【剧情点5-选项A】不予理会，全力攻占乌巢！");
        Debug.Log("→ 触发'火烧乌巢'动画，史实结局路线");

        // 检查史实结局条件
        CheckHistoricalEnding();
    }

    public void HandlePlotPoint5_OptionB_SplitForces()
    {
        Debug.Log("【剧情点5-选项B】派曹洪分兵回守大营");
        Debug.Log("→ 根据前期资源分流到IF3或IF4");

        // 检查资源决定结局
        if (ResourceManager.Instance != null)
        {
            if (ResourceManager.Instance.GetTroop() < 5.0f || ResourceManager.Instance.GetFood() < 5.0f)
            {
                Debug.Log("资源不足 → IF3: 元气大伤");
                TriggerEnding("IF3");
            }
            else
            {
                Debug.Log("资源足够 → IF4: 以退为进");
                TriggerEnding("IF4");
            }
        }
    }

    public void HandlePlotPoint5_OptionC_SurroundWei()
    {
        Debug.Log("【剧情点5-选项C】趁其大营空虚，直取袁绍！");
        Debug.Log("→ 围魏救赵，激进策略");

        // 根据前期选择决定结局
        if (ResourceManager.Instance != null)
        {
            bool isAggressive = ResourceManager.Instance.GetRisk() > 7.0f
                && ResourceManager.Instance.GetTroop() > 10.0f
                && ResourceManager.Instance.GetFood() > 10.0f;

            if (isAggressive)
            {
                Debug.Log("激进策略成功 → IF5: 完美结局");
                TriggerEnding("IF5");
            }
            else
            {
                Debug.Log("激进策略失败 → IF6: 惨败结局");
                TriggerEnding("IF6");
            }
        }
    }

    // 检查史实结局条件
    void CheckHistoricalEnding()
    {
        bool canWin = true;

        if (ResourceManager.Instance != null)
        {
            if (ResourceManager.Instance.GetTroop() < 3.0f)
            {
                Debug.LogWarning("兵力过低，无法取胜");
                canWin = false;
            }

            if (ResourceManager.Instance.GetFood() < 2.0f)
            {
                Debug.LogWarning("粮草过低，无法坚持");
                canWin = false;
            }
        }

        if (canWin)
        {
            Debug.Log("符合史实结局条件 → 触发史实胜利");
            TriggerEnding("HISTORICAL");
        }
        else
        {
            Debug.Log("条件不足，游戏失败");
            // 可以触发普通失败结局
        }
    }
}