using UnityEngine;

namespace XFrameworkUnity
{
    /// <summary>
    /// 所有管理器的基类
    /// </summary>
    /// <remarks>
    /// 所有管理器都继承自此类，并自动在 Awake 时注册到 XGameCore 中
    /// </remarks>
    public abstract class BaseManager : MonoBehaviour
    {
        public virtual void Awake()
        {
            XGameCore.RegisterManager(this);
        }
    }
}