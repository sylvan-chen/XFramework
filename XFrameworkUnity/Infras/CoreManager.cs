using UnityEngine;

namespace XFramework.Unity
{
    /// <summary>
    /// 核心管理器
    /// </summary>
    /// <remarks>
    /// 维护整个框架和游戏系统的运转和关闭。
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/CoreManager")]
    public sealed class CoreManager : BaseManager
    {
        private void Update()
        {
            XFrameworkCore.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        protected override void OnDestroy()
        {
            XLog.Debug("CoreManager OnShutdown");
            XFrameworkCore.Shutdown();
            StopAllCoroutines();
        }
    }
}