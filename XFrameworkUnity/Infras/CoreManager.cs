using UnityEngine;

namespace XFramework.Unity
{
    /// <summary>
    /// 核心管理器
    /// </summary>
    /// <remarks>
    /// 维护整个框架的运行，包括 XFreamworkCore 各个模块的运转。
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/CoreManager")]
    public sealed class CoreManager : BaseManager
    {
        private void Update()
        {
            XFrameworkCore.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            XFrameworkCore.Shutdown();
        }

        private void OnApplicationQuit()
        {
            StopAllCoroutines();
        }
    }
}