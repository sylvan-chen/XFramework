using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 框架组件基类
    /// </summary>
    public abstract class XFrameworkComponent : MonoBehaviour
    {
        protected virtual void Awake()
        {
            Log.Debug($"[XFramework] [XFrameworkComponent] Register {GetType().Name}.");
            XFrameworkDriver.Instance.Register(this);
        }

        protected virtual void OnDestroy()
        {
            Log.Debug($"[XFramework] [XFrameworkComponent] Destory {GetType().Name}.");
        }

        public virtual void Clear()
        {
            Log.Debug($"[XFramework] [XFrameworkComponent] Clear component {GetType().Name}.");
        }
    }
}