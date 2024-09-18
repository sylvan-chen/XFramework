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
        public static void QuitGame()
        {
            XLog.Info("[XFramework] [GlobalManager] Quit game...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>
        /// 重启游戏
        /// </summary>
        public static void RestartGame()
        {
            ShutdownFramework();
            XLog.Info("[XFramework] [GlobalManager] Restarting game...");
            SceneManager.LoadScene(0);
        }

        /// <summary>
        /// 关闭框架
        /// </summary>
        /// <remarks>
        /// 清理所有管理器，并销毁框架。
        /// </remarks>
        public static void ShutdownFramework()
        {
            XLog.Info("[XFramework] [GlobalManager] Shutdown XFramework...");
            foreach (BaseManager manager in _managerDict.Values)
            {
                manager.Shutdown();
            }
            _managerDict.Clear();
        }
    }
}