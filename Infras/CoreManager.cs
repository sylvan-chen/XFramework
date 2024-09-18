using UnityEngine;

namespace XFramework.Unity
{
    /// <summary>
    /// 核心管理器
    /// </summary>
    /// <remarks>
    /// 维护整个框架和游戏系统的生命周期。
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/CoreManager")]
    public sealed class CoreManager : BaseManager
    {
        protected override void OnShutdown()
        {
            XLog.Debug("CoreManager OnShutdown");
            StopAllCoroutines();
        }
    }
}