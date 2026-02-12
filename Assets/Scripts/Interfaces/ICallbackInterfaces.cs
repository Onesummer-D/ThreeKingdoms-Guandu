// 这是你与同学B的"合同"，你们各自实现自己的部分

// 小游戏完成回调接口
public interface IMiniGameCallbacks
{
    // 拼图游戏完成
    void OnPuzzleGameCompleted(bool success);

    // 走格子游戏完成（返回成功等级：1=完美, 2=良好, 3=一般）
    void OnGridGameCompleted(int successLevel);

    // 数值输入完成
    void OnValueInputConfirmed(float inputValue);
}

// UI显示接口
public interface IUIService
{
    // 显示数值输入面板
    void ShowValueInputPanel(float minValue, float maxValue);

    // 更新资源条显示
    void UpdateResourceDisplay(float troop, float food, float strategy, float risk);

    // 显示IF线解锁提示
    void ShowIfLineUnlocked(string ifLineName);

    // 显示结局
    void ShowEnding(string endingTitle, string endingDescription);
}