using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 框架组件基类
    /// </summary>
    public abstract class XFrameworkComponent : MonoBehaviour
    {
        /// <summary>
        /// 组件优先级，所有组件按优先级从小到大初始化和 Update，并按反序从大到小清理
        /// </summary>
        internal virtual int Priority { get => 0; }

        /// <summary>
        /// 组件在 Awake 时注册到驱动器
        /// </summary>
        protected virtual void Awake()
        {
            Log.Debug($"[XFramework] [XFrameworkComponent] Register component {GetType().Name}.");
            GameLauncher.Instance.Register(this);
            // 确保组件挂载在驱动器节点上作为子节点，以保证框架的正常运行
            if (transform.parent != GameLauncher.Instance.transform)
            {
                transform.SetParent(GameLauncher.Instance.transform);
            }
        }

        /// <summary>
        /// 组件需要在 Init 方法中实现初始化逻辑
        /// </summary>
        internal virtual void Init()
        {
            Log.Debug($"[XFramework] [XFrameworkComponent] Init component {GetType().Name} (Priority: {Priority}).");
        }

        /// <summary>
        /// 组件需要重载实现 Clear 方法以在关闭框架时清理数据和资源
        /// </summary>
        internal virtual void Clear()
        {
            Log.Debug($"[XFramework] [XFrameworkComponent] Clear component {GetType().Name} (Priority: {Priority}).");
        }
    }
}