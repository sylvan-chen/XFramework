using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 框架组件基类
    /// </summary>
    public abstract class XFrameworkComponentBase : MonoBehaviour
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
    }
}