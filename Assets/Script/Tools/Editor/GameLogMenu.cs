using UnityEditor;
using UnityEngine;

/// <summary>编辑器菜单：Tools → ZZZ → 运行时日志开关。</summary>
public static class GameLogMenu
{
    private const string k_MenuItem = "Tools/ZZZ/运行时日志（开关）";

    [MenuItem(k_MenuItem)]
    private static void ToggleGameLog()
    {
        bool next = !GameLog.logEnabled;
        GameLog.SetEnabledFromEditor(next);
        Debug.Log($"[GameLog] 运行时日志已{(next ? "开启" : "关闭")}。");
    }

    [MenuItem(k_MenuItem, true)]
    private static bool ToggleGameLogValidate()
    {
        Menu.SetChecked(k_MenuItem, GameLog.logEnabled);
        return true;
    }
}
