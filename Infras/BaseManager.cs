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
        public void Shutdown()
        {
            XLog.Debug($"Shutting down manager {GetType().Name}");
            OnShutdown();
            Destroy(gameObject);
        }

        /// <summary>
        /// 管理器关闭时的收尾工作
        /// </summary>
        protected abstract void OnShutdown();
    }
}