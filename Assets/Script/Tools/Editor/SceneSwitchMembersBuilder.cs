using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切人成员工具：复制角色并登记到场景，供 CharacterSwitcher 直接使用。
/// </summary>
public static class SceneSwitchMembersBuilder
{
    private static readonly string[] k_DefaultNames = { "ANBY", "BILLY", "CORIN" };
    private static readonly Color[] k_DefaultColors =
    {
        new Color(0.72f, 0.82f, 1f),
        new Color(1f, 0.62f, 0.78f),
        new Color(0.95f, 0.72f, 0.45f),
    };

    [MenuItem("Tools/ZZZ/切人/把当前角色复制成 3 个场景切人成员", priority = 70)]
    public static void DuplicatePlayerIntoSceneMembers()
    {
        var basePlayer = Object.FindFirstObjectByType<Player>();
        if (basePlayer == null)
        {
            EditorUtility.DisplayDialog("ZZZ 切人",
                "当前场景没有可用的 Player（请打开 SampleScene 并选中战斗场景）。", "知道了");
            return;
        }

        var baseRoot = basePlayer.transform.root != null ? basePlayer.transform.root.gameObject : basePlayer.gameObject;
        Vector3 basePos = basePlayer.transform.position;
        Quaternion baseRot = basePlayer.transform.rotation;
        Vector3 forward = baseRot * Vector3.forward;

        EnsureMarker(baseRoot, k_DefaultNames[0], k_DefaultColors[0], 0);

        for (int i = 1; i < k_DefaultNames.Length; i++)
        {
            GameObject clone = (GameObject)Object.Instantiate(baseRoot);
            Undo.RegisterCreatedObjectUndo(clone, "Create Switch Member " + k_DefaultNames[i]);
            clone.name = "Player_" + k_DefaultNames[i];
            clone.transform.position = basePos - forward * (1.8f * i);
            clone.transform.rotation = baseRot;

            EnsureMarker(clone, k_DefaultNames[i], k_DefaultColors[i], i);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SceneSwitchMembersBuilder] 已把角色复制为场景切人成员："
            + string.Join(" / ", k_DefaultNames));
    }

    [MenuItem("Tools/ZZZ/切人/把场景现有角色注册为切人成员", priority = 71)]
    public static void RegisterExistingPlayers()
    {
        var players = Object.FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (players == null || players.Length == 0)
        {
            EditorUtility.DisplayDialog("ZZZ 切人", "场景里没有找到 Player。", "知道了");
            return;
        }

        int index = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
            {
                continue;
            }
            var root = players[i].transform.root != null ? players[i].transform.root.gameObject : players[i].gameObject;
            var marker = root.GetComponent<SwitchCharacterMember>();
            if (marker == null)
            {
                marker = Undo.AddComponent<SwitchCharacterMember>(root);
            }
            if (string.IsNullOrEmpty(marker.characterName))
            {
                marker.characterName = k_DefaultNames[index % k_DefaultNames.Length];
            }
            if (index == 0)
            {
                marker.order = 0;
            }
            else if (marker.order <= 0)
            {
                marker.order = index;
            }
            index++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SceneSwitchMembersBuilder] 已注册 " + index + " 个场景切人成员。");
    }

    private static void EnsureMarker(GameObject root, string name, Color accent, int order)
    {
        var marker = root.GetComponent<SwitchCharacterMember>();
        if (marker == null)
        {
            marker = Undo.AddComponent<SwitchCharacterMember>(root);
        }
        marker.characterName = name;
        marker.accent = accent;
        marker.order = order;
    }
}
