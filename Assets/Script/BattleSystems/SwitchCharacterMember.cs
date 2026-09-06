using UnityEngine;

/// <summary>场景切人成员标记：CharacterSwitcher 按 order 排序登记。</summary>
[AddComponentMenu("ZZZ/Switch Character Member")]
public class SwitchCharacterMember : MonoBehaviour
{
    [Tooltip("成员显示名")]
    public string characterName = "AGENT";

    [Tooltip("成员强调色")]
    public Color accent = Color.white;

    [Tooltip("登记顺序：越小越靠前（0 为初始操作角色）")]
    public int order;
}
