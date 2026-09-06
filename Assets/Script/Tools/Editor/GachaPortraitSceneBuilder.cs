using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 在 Lobby 场景为抽卡结果卡片与图鉴槽位生成立绘节点，
/// 生成后可在 Hierarchy 中直接调整布局。
/// </summary>
public static class GachaPortraitSceneBuilder
{
    private const string k_ScenePath = "Assets/Scenes/Lobby.unity";

    [MenuItem("Tools/ZZZ/抽卡/在 Lobby 场景生成抽卡/图鉴立绘节点")]
    public static void BuildPortraitNodes()
    {
        var scene = EnsureLobbyOpen();
        if (!scene.IsValid())
        {
            return;
        }

        int portraitCount = 0;
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var gacha = roots[i].GetComponentInChildren<GachaPanel>(true);
            if (gacha != null && gacha.EnsureResultPortrait() != null)
            {
                portraitCount++;
            }

            var codex = roots[i].GetComponentInChildren<CodexPanel>(true);
            if (codex != null)
            {
                portraitCount += codex.EnsureSlotPortraits();
            }
        }

        if (portraitCount == 0)
        {
            EditorUtility.DisplayDialog("ZZZ 抽卡立绘",
                "Lobby 场景里没有找到 GachaPanel / CodexPanel，请确认已打开大厅场景。", "知道了");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[GachaPortraitSceneBuilder] 已生成/确认 {portraitCount} 个立绘节点并保存 Lobby 场景。");
    }

    private static Scene EnsureLobbyOpen()
    {
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (scene.IsValid() && scene.path == k_ScenePath)
            {
                return scene;
            }
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return default;
        }
        return EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Single);
    }
}
