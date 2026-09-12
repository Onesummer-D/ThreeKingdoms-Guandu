using UnityEngine;
using System.Reflection;

public class DebugScriptOrder : MonoBehaviour
{
    void Awake() { Debug.Log($"{Time.frameCount} - {name}.Awake()"); }
    void Start() { Debug.Log($"{Time.frameCount} - {name}.Start()"); }
    void OnEnable() { Debug.Log($"{Time.frameCount} - {name}.OnEnable()"); }

    void Update()
    {
        if (Time.frameCount == 3)
        {
            Debug.Log("=== 检查所有组件 ===");
            var comps = GetComponents<MonoBehaviour>();
            foreach (var comp in comps)
            {
                Debug.Log($"组件: {comp.GetType().Name}, 启用: {comp.enabled}");
            }
        }
    }
}