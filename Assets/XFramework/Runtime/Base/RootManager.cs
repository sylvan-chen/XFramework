using System;
using System.Collections;
using System.Collections.Generic;
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
        private readonly Dictionary<Type, ManagerBase> _managerDict = new();

        private void OnDestroy()
        {
            Log.Info("[XFramework] [RootManager] Destroy RootManager...");
            ShutdownFramework();
        }

        public void Register(ManagerBase manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager), "Register manager failed. Manager can not be null.");
            }
            Type managerType = manager.GetType();
            if (_managerDict.ContainsKey(managerType))
            {
                throw new InvalidOperationException($"Register manager failed. Manager of type {manager.GetType().Name} has already been registered.");
            }
            _managerDict.Add(managerType, manager);
        }

        public T GetManager<T>() where T : ManagerBase
        {
            if (_managerDict.TryGetValue(typeof(T), out ManagerBase manager))
            {
                return manager as T;
            }
            else
            {
                Log.Warning($"[XFramework] [RootManager] Can not find manager of type {typeof(T).Name}");
                return null;
            }
        }

        public ManagerBase GetManager(Type managerType)
        {
            if (managerType == null)
            {
                throw new ArgumentNullException(nameof(managerType), "Get manager failed. Manager type can not be null.");
            }
            if (!typeof(ManagerBase).IsAssignableFrom(managerType))
            {
                throw new ArgumentException($"Get manager failed. Type {managerType.Name} is not a subclass of {nameof(ManagerBase)}.", nameof(managerType));
            }
            if (_managerDict.TryGetValue(managerType, out ManagerBase manager))
            {
                return manager;
            }
            else
            {
                Log.Warning($"[XFramework] [RootManager] Can not find manager of type {managerType.Name}");
                return null;
            }
        }

        /// <summary>
        /// 关闭游戏
        /// </summary>
        public void ShutdownGame()
        {
            Log.Info("[XFramework] [RootManager] Shutdown game...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void ShutdownFramework()
        {
            Log.Info("[XFramework] [RootManager] Shutdown XFramework...");
            foreach (ManagerBase manager in _managerDict.Values)
            {
                DestroyImmediate(manager.gameObject);
            }
            _managerDict.Clear();
            ReferencePool.Clear();
        }
    }
}