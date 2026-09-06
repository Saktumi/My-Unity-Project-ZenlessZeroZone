using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>战斗选择窗口：选择怪物进入对应战斗。</summary>
public class BattlePanel : MonoBehaviour
{
    /// <summary>场景按钮绑定：进入第 index 个怪物的战斗。</summary>
    public void EnterBattle(int index)
    {
        BattleContext.Select(index);
        GameLog.Log($"[BattlePanel] 选择怪物 [{BattleContext.selected.name}]，进入 SampleScene");
        SceneManager.LoadScene("SampleScene");
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
