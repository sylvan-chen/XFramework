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
            Global.Instance.RegisterManager(this);
        }

        /// <summary>
        /// 关闭 Manager，注销自己
        /// </summary>
        public virtual void Shutdown()
        {
            Global.Instance.UnregisterManager(this);
            Destroy(gameObject);
        }
    }
}