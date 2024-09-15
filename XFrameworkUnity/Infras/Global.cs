using System;
using System.Collections.Generic;
using UnityEngine;
using XFramework;

namespace XFrameworkUnity
{
    /// <summary>
    /// 全局管理所有其他管理器
    /// </summary>
    public sealed class Global : MonoSingleton<Global>
    {
        private readonly Dictionary<Type, BaseManager> _managerDict = new();

        private void Update()
        {
            XFrameworkGlobal.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            XFrameworkGlobal.Shutdown();
            StopAllCoroutines();
        }

        /// <summary>
        /// 获取指定类型的管理器
        /// </summary>
        /// <typeparam name="T">要获取的管理器类型</typeparam>
        /// <returns>获取到的管理器实例</returns>
        public T GetManager<T>() where T : BaseManager
        {
            Type type = typeof(T);
            if (!_managerDict.ContainsKey(type))
            {
                XLog.Error($"[XFramework] [GlobalManager] Cannot find manager of type {typeof(T).Name}");
                return null;
            }
            return _managerDict[type] as T ?? throw new NullReferenceException($"Manager with type {type.Name} is null");
        }

        /// <summary>
        /// 注册管理器
        /// </summary>
        /// <param name="manager">要注册的管理器</param>
        public void RegisterManager(BaseManager manager)
        {
            if (manager == null)
            {
                XLog.Error("[XFramework] [GlobalManager] Cannot register null manager");
                return;
            }
            // 检查这个类型的管理器是否已经注册过
            Type registeredType = manager.GetType();
            if (_managerDict.ContainsKey(registeredType))
            {
                XLog.Error($"[XFramework] [GlobalManager] Manager {manager.GetType().Name} has already been registered");
                return;
            }
            _managerDict.Add(registeredType, manager);
        }

        public void UnregisterManager(BaseManager manager)
        {
            if (manager == null)
            {
                XLog.Error("[XFramework] [GlobalManager] Cannot unregister null manager");
                return;
            }
            Type registeredType = manager.GetType();
            if (!_managerDict.ContainsKey(registeredType))
            {
                XLog.Error($"[XFramework] [GlobalManager] Manager {manager.GetType().Name} has not been registered but try to unregister");
                return;
            }
            _managerDict.Remove(registeredType);
        }

        /// <summary>
        /// 关闭游戏框架
        /// </summary>
        /// <param name="mode">关闭模式，默认为关闭游戏框架并退出游戏</param>
        public void Shutdown(ShutdownMode mode = ShutdownMode.WithGameQuit)
        {
            XLog.Info($"[XFramework] [GlobalManager] Shutdown XFramework ({mode})...");
            foreach (BaseManager manager in _managerDict.Values)
            {
                manager.Shutdown();
            }
            XFrameworkGlobal.Shutdown();
            switch (mode)
            {
                case ShutdownMode.OnlyFramework:
                    break;
                case ShutdownMode.WithGameQuit:
                    Application.Quit();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    break;
            }
        }

        public enum ShutdownMode
        {
            /// <summary>
            /// 仅关闭游戏框架，不关闭游戏
            /// </summary>
            OnlyFramework,
            /// <summary>
            /// 关闭游戏框架并退出游戏
            /// </summary>
            WithGameQuit,
        }
    }
}