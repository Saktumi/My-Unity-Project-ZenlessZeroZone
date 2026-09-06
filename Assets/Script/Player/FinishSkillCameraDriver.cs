using System.Collections;
using UnityEngine;

/// <summary>终结技镜头驱动器：负责特写相机切换、推进与恢复。</summary>
public class FinishSkillCameraDriver
{
    private readonly Player m_player;

    private Camera m_originalCamera;
    private bool m_prepared;
    private Vector3 m_camera2BaseLocalPos;
    private bool m_camera2BasePosValid;
    private Coroutine m_dollyRoutine;

    public FinishSkillCameraDriver(Player player)
    {
        m_player = player;
    }

    /// <summary>开局禁用特写相机，避免抢占主视角。</summary>
    public void DisableCamerasAtStart()
    {
        if (m_player.finishSkillCamera1 != null)
        {
            m_player.finishSkillCamera1.enabled = false;
        }
        if (m_player.finishSkillCamera2 != null)
        {
            m_player.finishSkillCamera2.enabled = false;
        }
    }

    /// <summary>动画事件回调：1 → 相机1，2 → 相机2并推进，其它值 → 恢复原相机。</summary>
    public void OnEvent(int index)
    {
        switch (index)
        {
            case 1:
                SetActiveCamera(m_player.finishSkillCamera1);
                break;
            case 2:
                SetActiveCamera(m_player.finishSkillCamera2);
                StartDolly();
                break;
            default:
                Restore();
                break;
        }
    }

    /// <summary>记录当前相机，作为结束后要恢复的相机。</summary>
    public void Prepare()
    {
        if (m_player.finishSkillCamera1 == null && m_player.finishSkillCamera2 == null)
        {
            return;
        }

        m_originalCamera = FindActiveCamera();
        m_prepared = true;
        if (m_player.finishSkillCamera2 != null)
        {
        // 记录相对位置，结束后相机 2 复位
            m_camera2BaseLocalPos = m_player.finishSkillCamera2.transform.localPosition;
            m_camera2BasePosValid = true;
        }
    }

    /// <summary>恢复原相机，并把相机2拉回初始相对位置。</summary>
    public void Restore()
    {
        if (!m_prepared)
        {
            return;
        }

        StopDolly();
        if (m_camera2BasePosValid && m_player.finishSkillCamera2 != null)
        {
            m_player.finishSkillCamera2.transform.localPosition = m_camera2BaseLocalPos;
        }
        m_camera2BasePosValid = false;

        SetActiveCamera(m_originalCamera);
        m_prepared = false;
    }

    private void StartDolly()
    {
        var cam2 = m_player.finishSkillCamera2;
        if (cam2 == null
            || !m_camera2BasePosValid
            || Mathf.Approximately(m_player.finishSkillCamera2ZDolly, 0f))
        {
            return;
        }

        StopDolly();
        m_dollyRoutine = m_player.StartCoroutine(DollyCamera2());
    }

    private void StopDolly()
    {
        if (m_dollyRoutine != null)
        {
            m_player.StopCoroutine(m_dollyRoutine);
            m_dollyRoutine = null;
        }
    }

    private IEnumerator DollyCamera2()
    {
        var camTransform = m_player.finishSkillCamera2.transform;
        // 全程使用局部坐标：父物体移动时相机跟随，结束后能回到初始相对位置
        Vector3 original = camTransform.localPosition;
        Vector3 localForward = camTransform.localRotation * Vector3.forward;
        Vector3 target = original + localForward * m_player.finishSkillCamera2ZDolly;

        float elapsed = 0f;
        float duration = Mathf.Max(0.001f, m_player.finishSkillCamera2DollyTime);

        // 先向前推进
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            camTransform.localPosition = Vector3.Lerp(original, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        camTransform.localPosition = target;

        // 再退回原位
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            camTransform.localPosition = Vector3.Lerp(target, original, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        camTransform.localPosition = original;
        m_dollyRoutine = null;
    }

    private void SetActiveCamera(Camera target)
    {
        if (target == null)
        {
            return;
        }

        if (m_originalCamera != null && m_originalCamera != target)
        {
            m_originalCamera.enabled = false;
        }
        if (m_player.finishSkillCamera1 != null && m_player.finishSkillCamera1 != target)
        {
            m_player.finishSkillCamera1.enabled = false;
        }
        if (m_player.finishSkillCamera2 != null && m_player.finishSkillCamera2 != target)
        {
            m_player.finishSkillCamera2.enabled = false;
        }

        target.enabled = true;
    }

    private Camera FindActiveCamera()
    {
        if (Camera.main != null && Camera.main.enabled)
        {
            return Camera.main;
        }

        // 回退：取场景中第一个启用的相机
        foreach (var cam in Camera.allCameras)
        {
            if (cam.enabled)
            {
                return cam;
            }
        }
        return null;
    }
}
