using System;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 根管理器
    /// </summary>
    /// <remarks>
    /// 管理各个管理器，并提供安全关闭游戏的方法。
    /// </remarks>
    internal sealed class RootManager : MonoSingletonPersistent<RootManager>
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            Log.Info("[XFramework] [RootManager] Force quit game!");
            ShutdownFramework();
        }

        public T GetManager<T>() where T : Manager
        {
            T manager = GetComponentInChildren<T>() ?? throw new InvalidOperationException($"Get manager {typeof(T).Name} failed.");
            Log.Debug($"[XFramework] [RootManager] Get manager: {manager.GetType().Name}");
            return manager;
        }

        /// <summary>
        /// 关闭游戏
        /// </summary>
        public void ShutdownGame()
        {
            Log.Info("[XFramework] [RootManager] Quit game...");
            ShutdownFramework();
            ReferencePool.Clear();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void ShutdownFramework()
        {
            Log.Info("[XFramework] [RootManager] Shutdown XFramework...");
            Destroy(gameObject);
        }
    }
}