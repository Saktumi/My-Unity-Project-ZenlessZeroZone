using UnityEngine;

/// <summary>单例基类：提供静态 instance 访问。</summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T m_instance;

    public static T instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = Object.FindFirstObjectByType<T>();
            }
            return m_instance;
        }
    }
}
