using UnityEngine;

namespace XFramework.Unity
{
    /// <summary>
    /// 所有管理器的基类
    /// </summary>
    public abstract class BaseManager : MonoBehaviour
    {
        /// <summary>
        /// 在 Awake 时将自己注册到 Global 中
        /// </summary>
        protected virtual void Awake()
        {
            XLog.Debug($"Registering manager {GetType().Name} to Global");
            Global.RegisterManager(this);
        }

        /// <summary>
        /// 关闭管理器，销毁自己
        /// </summary>
        public virtual void Shutdown()
        {
            XLog.Debug($"Shutting down manager {GetType().Name}");
            Destroy(gameObject);
        }
    }
}