using UnityEngine;

/// <summary>实体基类：玩家 / 敌人共用的控制器、状态机与朝向封装。</summary>
public class Entity<T> : EntityBase where T : Entity<T>
{
    public EntityStateManager<T> states { get; protected set; }

    protected virtual void Awake()
    {
        InitializeController();
        InitializeStateManager();
    }

    /// <summary>初始化 CharacterController，已有则复用。</summary>
    protected virtual void InitializeController()
    {
        controller = GetComponent<CharacterController>();

        if (!controller)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        controller.skinWidth = 0.005f;
        controller.minMoveDistance = 0;
        originalHeight = controller.height;
    }

    /// <summary>获取同物体上的状态管理器。</summary>
    protected virtual void InitializeStateManager() => states = GetComponent<EntityStateManager<T>>();

    /// <summary>以指定角速度平滑转向目标方向。</summary>
    public virtual void FaceDirection(Vector3 direction, float degreesPerSecond)
    {
        if (direction != Vector3.zero)
        {
            var rotation = transform.rotation;
            var rotationDelta = degreesPerSecond * Time.deltaTime;
            var target = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(rotation, target, rotationDelta);
        }
    }

    /// <summary>每帧步进状态机。</summary>
    protected virtual void HandleStates() => states.Step();

    protected virtual void Update()
    {
        HandleStates();
    }
}

