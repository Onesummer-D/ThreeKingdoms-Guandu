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

        // ========== 订阅GameCallbacks事件 ==========
        if (GameCallbacks.Instance != null)
        {
            GameCallbacks.Instance.OnPuzzleGameCompletedEvent += OnPuzzleGameCompleted;
            GameCallbacks.Instance.OnValueInputConfirmedEvent += OnValueInputConfirmed;
            GameCallbacks.Instance.OnGridGameCompletedEvent += OnGridGameCompleted;
            Debug.Log("PlotPointManager已订阅GameCallbacks事件");
        }
        // =============================================
    }

    // ========== 拼图游戏完成的事件处理方法 ==========
    private void OnPuzzleGameCompleted(bool success)
    {
        Debug.Log($"PlotPointManager收到拼图游戏完成事件：{(success ? "成功" : "失败")}");

        if (success)
        {
            // 兵力+1
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ModifyTroop(1.0f);
                Debug.Log("拼图完成！兵力+1");
            }

            // 触发拼图完成后的对话节点
            ShowPuzzleCompleteDialogue();
        }
    }

    private void ShowPuzzleCompleteDialogue()
    {

        // 使用DialogueSystem显示对话（节点1002应该是"投石机制造完成"的反馈）
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.ShowDialogueNode(1002);
            Debug.Log("触发拼图完成对话节点1002");
        }
        else
        {
            Debug.LogError("DialogueSystem未找到，无法显示拼图完成对话");
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

    // === 剧情点1：初战受挫 - 选项A：拼图游戏 ===
    public void HandleOptionA_Puzzle(int nextNodeId = 1002)
    {
        Debug.Log("【剧情点1-选项A】玩家选择了拼图游戏");
        Debug.Log("等待拼图游戏完成事件...");
        // 实际拼图游戏启动由FinalUIManager处理
    }

    // ========== 数值输入的事件处理方法 ==========
    private void OnValueInputConfirmed(float value)
    {
        Debug.Log($"PlotPointManager收到数值输入事件：{value}");
        HandleOptionC_Ambush(value);
    }

    private void OnGridGameCompleted(bool isWin)
    {
        Debug.Log($"PlotPointManager收到走格子游戏完成：{(isWin ? "胜利" : "失败")}");

        if (isWin)
        {
            // 胜利：兵力+12，粮草+10，风险+10（根据数值设定）
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ModifyTroop(12f);
                ResourceManager.Instance.ModifyFood(10f);
                ResourceManager.Instance.ModifyRisk(10f);
            }

            // ✅ 修正：跳转到剧情点五第1页（500101），不是4005
            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.ShowDialogueNode(500101);
            }
        }
        else
        {
            // 失败：触发IF线
            Debug.Log("走格子失败，触发失败结局");
            TriggerEnding("IF3");
        }
    }

    // === 在OnDestroy()中取消订阅 ===
    void OnDestroy()
    {
        if (GameCallbacks.Instance != null)
        {
            GameCallbacks.Instance.OnPuzzleGameCompletedEvent -= OnPuzzleGameCompleted;
            GameCallbacks.Instance.OnValueInputConfirmedEvent -= OnValueInputConfirmed;
            GameCallbacks.Instance.OnGridGameCompletedEvent -= OnGridGameCompleted;
        }
    }

    public void HandleOptionB_DigTunnel()
    {
        Debug.Log("【剧情点1-选项B】玩家选择了挖掘地道");

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyFood(-1.0f);
            Debug.Log("粮草-1，当前粮草：" + ResourceManager.Instance.GetFood());
        }

        // ✅ 关键修复：挖地道完成后跳转到剧情点二（200001）
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.ShowDialogueNode(200001);
            Debug.Log("挖地道完成，跳转到剧情点二：200001");
        }
    }

    // 找到这个方法，完全替换为：
    public void HandleOptionC_Ambush(float inputValue)
    {
        Debug.Log($"处理埋伏选项，输入值：{inputValue}");

        // 新数值判定：≤20为合理，>20为失败
        bool isSuccess = inputValue <= 20f;

        // 应用数值变化
        if (ResourceManager.Instance != null)
        {
            // 兵力扣除（玩家设置的值）
            ResourceManager.Instance.ModifyTroop(-inputValue);

            if (isSuccess)
            {
                // 合理范围（≤20）：计策成功率+20，粮草-5
                ResourceManager.Instance.ModifyStrategy(20f);
                ResourceManager.Instance.ModifyFood(-5f);
                Debug.Log($"埋伏成功：兵力-{inputValue}，计策成功率+20，粮草-5");
            }
            else
            {
                // 过高（>20）：计策成功率-20，粮草-10，风险+20
                ResourceManager.Instance.ModifyStrategy(-20f);
                ResourceManager.Instance.ModifyFood(-10f);
                ResourceManager.Instance.ModifyRisk(20f);
                Debug.Log($"埋伏失败：兵力-{inputValue}，计策成功率-20，粮草-10，风险+20");
            }
        }

        // 触发事件通知FinalUIManager
        OnAmbushResultProcessed?.Invoke(isSuccess, GetAmbushResultText(isSuccess), inputValue);
    }

    private string GetAmbushResultText(bool isSuccess)
    {
        if (isSuccess)
            return "诱敌成功！袁军主力被吸引至埋伏圈，大挫敌军锐气。";
        else
            return "诱敌失败！设置的诱敌兵力过多，反被袁军识破埋伏，遭到反包围。";
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

    // ========== 选项C的结果事件 ==========
    public static event System.Action<bool, string, float> OnAmbushResultProcessed;

    // === 剧情点2：粮草将尽 ===
    public void HandlePlotPoint2_OptionA_Stay()
    {
        Debug.Log("【剧情点2-选项A】采纳文若之谏，坚守官渡");
        Debug.Log("→ 直接进入下一段剧情");
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