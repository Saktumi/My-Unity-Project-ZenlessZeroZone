using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 在战斗场景中生成一套可编辑的 QTE 选人面板，
/// 并自动接到 BattleBootstrap 上的 SwitchTimePanel 组件。
/// </summary>
public static class QtePanelSceneBuilder
{
    private const string k_BootstrapName = "BattleBootstrap";

    [MenuItem("Tools/ZZZ/QTE UI/生成默认选人面板（接入 BattleBootstrap）", priority = 60)]
    public static void BuildDefaultPanelInScene()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("ZZZ QTE UI",
                "当前场景没有 Canvas，请先打开 SampleScene 再执行。", "知道了");
            return;
        }

        var bootstrap = GameObject.Find(k_BootstrapName);
        if (bootstrap == null)
        {
            EditorUtility.DisplayDialog("ZZZ QTE UI",
                "当前场景找不到名为 \"" + k_BootstrapName + "\" 的物体。\n" +
                "请打开 SampleScene，或先手动把 SwitchTimePanel 挂到任意物体上再配置引用。", "知道了");
            return;
        }

        var panel = bootstrap.GetComponent<SwitchTimePanel>();
        if (panel == null)
        {
            panel = Undo.AddComponent<SwitchTimePanel>(bootstrap);
        }

        if (panel.uiRoot != null)
        {
            bool rebuild = EditorUtility.DisplayDialog("ZZZ QTE UI",
                "BattleBootstrap 上已经有一份生成的面板，是否删除并重新生成？", "重新生成", "取消");
            if (!rebuild)
            {
                return;
            }
            Undo.DestroyObjectImmediate(panel.uiRoot.gameObject);
            panel.uiRoot = null;
            panel.panelGroup = null;
            panel.leftCardImage = null;
            panel.rightCardImage = null;
            panel.leftAvatarImage = null;
            panel.rightAvatarImage = null;
            panel.leftNameText = null;
            panel.rightNameText = null;
            panel.countdownText = null;
            panel.hintText = null;
        }

        RectTransform root = panel.BuildDefaultUi(canvas.transform);
        if (root == null)
        {
            EditorUtility.DisplayDialog("ZZZ QTE UI", "生成失败，请查看 Console 日志。", "知道了");
            return;
        }

        Undo.RegisterCreatedObjectUndo(root.gameObject, "Generate QTE Panel");
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = root.gameObject;
        Debug.Log("[QtePanelSceneBuilder] 已生成 QTE 选人面板，并接到 BattleBootstrap.SwitchTimePanel，可在 Hierarchy 中直接修改。");
    }

    [MenuItem("Tools/ZZZ/QTE UI/选中场景中的 QTE 面板", priority = 61)]
    public static void SelectExistingPanel()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }
        var existing = canvas.transform.Find("SwitchTimePanel");
        if (existing != null)
        {
            Selection.activeTransform = existing;
        }
        else
        {
            Debug.Log("当前场景中还没有生成过 SwitchTimePanel，请先执行上一项菜单。");
        }
    }
}
