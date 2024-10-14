using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    public abstract class ManagerBase : MonoBehaviour
    {
        protected virtual void Awake()
        {
            Log.Debug($"[XFramework] [ManagerBase] Register {GetType().Name}.");
            RootManager.Instance.Register(this);
        }

        protected virtual void OnDestroy()
        {
            Log.Debug($"[XFramework] [ManagerBase] Destory {GetType().Name}.");
        }
    }
}