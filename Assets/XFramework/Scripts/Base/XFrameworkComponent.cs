using UnityEngine;

namespace XFramework
{
    /// <summary>
    /// 框架组件基类
    /// </summary>
    public abstract class XFrameworkComponent
    {
        /// <summary>
        /// 组件优先级，所有组件按优先级从小到大 Update，并按反序从大到小清理
        /// </summary>
        internal abstract int Priority { get; }

        public bool IsInitialized { get; private set; }
        public bool IsShutDown { get; private set; }

        internal virtual void Init()
        {
            IsInitialized = true;
        }

        internal virtual void Shutdown()
        {
            IsShutDown = true;
        }

        internal virtual void Update(float deltaTime, float unscaledDeltaTime)
        {
        }
    }

    public class XFrameworkComponentDebugger : MonoBehaviour
    {
        public XFrameworkComponent Component { get; private set; }

        public void Init(XFrameworkComponent component)
        {
            Component = component;
            transform.SetParent(GameLauncher.Instance.transform);
        }
    }
}