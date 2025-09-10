using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 框架组件基类
    /// </summary>
    public abstract class FrameworkComponent
    {
        public bool IsInitialized { get; private set; }
        public bool IsShutDown { get; private set; }
        public int Priority { get; internal set; }

        internal virtual void Initialize()
        {
            IsInitialized = true;
        }

        internal virtual void Dispose()
        {
            IsShutDown = true;
        }

        internal virtual void Update(float deltaTime, float unscaledDeltaTime)
        {
        }
    }

    public class FrameworkComponentDebugger : MonoBehaviour
    {
        public FrameworkComponent Component { get; private set; }

        public void Init(FrameworkComponent component)
        {
            Component = component;
            transform.SetParent(GameLauncher.Instance.transform);
        }
    }
}