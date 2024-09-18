using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XFramework.Unity
{
    /// <summary>
    /// 全局管理
    /// </summary>
    public static class Global
    {
        private static readonly Dictionary<Type, BaseManager> _managerDict = new();

        /// <summary>
        /// 获取指定类型的管理器
        /// </summary>
        /// <typeparam name="T">要获取的管理器类型</typeparam>
        /// <returns>获取到的管理器实例</returns>
        public static T GetManager<T>() where T : BaseManager
        {
            if (_managerDict.TryGetValue(typeof(T), out BaseManager manager))
            {
                return manager as T;
            }
            XLog.Error($"[XFramework] [GlobalManager] Cannot find manager of type {typeof(T).Name}");
            return null;
        }

        /// <summary>
        /// 注册管理器
        /// </summary>
        /// <param name="manager">要注册的管理器</param>
        public static void RegisterManager(BaseManager manager)
        {
            if (manager == null)
            {
                XLog.Error("[XFramework] [GlobalManager] Cannot register null manager");
                return;
            }
            // 同类型的管理器不允许重复注册
            Type registeredType = manager.GetType();
            if (_managerDict.ContainsKey(registeredType))
            {
                throw new Exception($"Manager {registeredType.Name} registered multiple times, check if there are duplicate managers in your project");
            }
            _managerDict.Add(registeredType, manager);
        }

        /// <summary>
        /// 关闭游戏
        /// </summary>
        /// <param name="mode">关闭模式，默认为关闭游戏框架并退出游戏</param>
        /// <remarks>
        /// 通过该方法安全关闭游戏而非直接调用 Application.Quit()，避免因管理器未正常关闭导致的异常。
        /// 除了直接关闭游戏外，还提供了其他模式，如重启游戏、仅关闭游戏框架等。
        /// </remarks>
        public static void Shutdown(ShutdownMode mode = ShutdownMode.Default)
        {
            XLog.Info($"[XFramework] [GlobalManager] Shutdown XFramework ({mode})...");
            foreach (BaseManager manager in _managerDict.Values)
            {
                manager.Shutdown();
            }
            _managerDict.Clear();
            switch (mode)
            {
                case ShutdownMode.Default:
                    Application.Quit();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    break;
                case ShutdownMode.Restart:
                    XLog.Info("[XFramework] [GlobalManager] Restarting game...");
                    SceneManager.LoadScene(0);
                    break;
                case ShutdownMode.OnlyFramework:
                    break;
            }
        }

        public enum ShutdownMode
        {
            /// <summary>
            /// 关闭游戏框架并退出游戏
            /// </summary>
            Default,
            /// <summary>
            /// 重启游戏
            /// </summary>
            Restart,
            /// <summary>
            /// 仅关闭游戏框架，不关闭游戏
            /// </summary>
            OnlyFramework,
        }
    }
}