using UnityEngine;

/// <summary>实体最底层基类：提供位置、地面检测与控制器访问。</summary>
public abstract class EntityBase : MonoBehaviour
{
    public Vector3 unsizedPosition => transform.position;
    public bool isGrounded { get; protected set; } = true;

    public CharacterController controller { get; protected set; }
    public float originalHeight { get; protected set; }

    /// <summary>是否处于坡面上，子类可覆写检测逻辑。</summary>
    public virtual bool OnSlopingGround()
    {
        return false;
    }
}
