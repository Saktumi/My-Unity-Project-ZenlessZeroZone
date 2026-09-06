using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;
using System.Collections.Generic;
#endif

/// <summary>
/// 切人系统工具集：队伍命名与染色、锚点查找、切入切出动画状态解析。
/// </summary>
public static class CharacterSwitchUtils
{
    public static string GetDemoName(int index, string[] demoMemberNames)
    {
        if (demoMemberNames != null && index < demoMemberNames.Length && !string.IsNullOrEmpty(demoMemberNames[index]))
        {
            return demoMemberNames[index];
        }
        return "AGENT " + (index + 1);
    }

    public static Color GetDemoColor(int index, Color[] demoMemberColors)
    {
        if (demoMemberColors != null && index < demoMemberColors.Length)
        {
            return demoMemberColors[index];
        }
        return Color.Lerp(Color.cyan, Color.magenta, (index * 0.37f) % 1f);
    }

    public static void TintRenderer(GameObject root, Color accent)
    {
        if (root == null)
        {
            return;
        }
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }
            var mats = renderers[i].materials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] != null && mats[m].HasProperty("_Color"))
                {
                    Color c = mats[m].color;
                    mats[m].color = new Color(c.r * accent.r, c.g * accent.g, c.b * accent.b, c.a);
                }
            }
        }
    }

    public static Transform FindAnchor(Player player)
    {
        if (player == null)
        {
            return null;
        }
        Transform anchor = FindChildByName(player.transform, "viewpoint");
        return anchor != null ? anchor : player.transform;
    }

    public static Transform FindChildByName(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }
        if (root.name == name)
        {
            return root;
        }
        for (int i = 0; i < root.childCount; i++)
        {
            var result = FindChildByName(root.GetChild(i), name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    public static int GetMemberOrder(CharacterSwitcher.TeamMember member)
    {
        if (member == null || member.player == null)
        {
            return int.MaxValue;
        }
        var marker = member.player.GetComponent<SwitchCharacterMember>()
            ?? member.player.GetComponentInParent<SwitchCharacterMember>();
        return marker != null ? marker.order : int.MaxValue;
    }

    public static bool IsInAnimatorState(Animator animator, string stateName)
    {
        return animator != null
            && !string.IsNullOrEmpty(stateName)
            && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    /// <summary>解析切入 / 切出动画状态名：优先配置，再回退常见命名。</summary>
    public static string ResolveSwitchState(Animator animator, bool isIn, string configured)
    {
        if (animator == null)
        {
            return null;
        }
        if (!string.IsNullOrEmpty(configured)
            && animator.HasState(0, Animator.StringToHash(configured)))
        {
            return configured;
        }

#if UNITY_EDITOR
        string scanned = ScanSwitchState(animator, isIn);
        if (!string.IsNullOrEmpty(scanned))
        {
            return scanned;
        }
#endif

        string[] candidates = isIn
            ? new[] { "SwitchIn", "Switch in", "SwitchIn_Normal", "切入", "Longinus_SwitchIn_Normal",
                "Swtich in", "SwtichIn" }   // 兼容部分素材里的拼写
            : new[] { "SwitchOut", "Switch out", "SwitchOut_Normal", "切出", "Longinus_SwitchOut_Normal" };
        for (int i = 0; i < candidates.Length; i++)
        {
            if (animator.HasState(0, Animator.StringToHash(candidates[i])))
            {
                return candidates[i];
            }
        }
        return null;
    }

#if UNITY_EDITOR
    private static string ScanSwitchState(Animator animator, bool isIn)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller is AnimatorOverrideController overrideController)
        {
            controller = overrideController.runtimeAnimatorController;
        }
        if (!(controller is AnimatorController animatorController))
        {
            return null;
        }

        var visited = new HashSet<AnimatorStateMachine>();
        for (int i = 0; i < animatorController.layers.Length; i++)
        {
            string found = ScanStateMachine(animatorController.layers[i].stateMachine, isIn, visited);
            if (!string.IsNullOrEmpty(found))
            {
                return found;
            }
        }
        return null;
    }

    private static string ScanStateMachine(AnimatorStateMachine machine, bool isIn, HashSet<AnimatorStateMachine> visited)
    {
        if (machine == null || !visited.Add(machine))
        {
            return null;
        }

        var states = machine.states;
        for (int i = 0; i < states.Length; i++)
        {
            var state = states[i].state;
            if (state == null)
            {
                continue;
            }
            string lower = state.name.ToLowerInvariant().Replace(" ", string.Empty);
            // 归一化名称，兼容素材里的错位拼写
            bool match = isIn
                ? lower.Contains("switchin") || lower.Contains("swtichin")
                    || state.name.Contains("切入") || state.name.Contains("入场")
                : lower.Contains("switchout") || state.name.Contains("切出") || state.name.Contains("出场");
            if (match)
            {
                return state.name;
            }
        }

        var machines = machine.stateMachines;
        for (int i = 0; i < machines.Length; i++)
        {
            string found = ScanStateMachine(machines[i].stateMachine, isIn, visited);
            if (!string.IsNullOrEmpty(found))
            {
                return found;
            }
        }
        return null;
    }
#endif
}
