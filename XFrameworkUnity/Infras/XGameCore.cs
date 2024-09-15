using System;
using UnityEngine;
using XFramework;

namespace XFrameworkUnity
{
    /// <summary>
    /// 游戏核心组件，所有的管理器入口
    /// </summary>
    public static class XGameCore
    {
        private static readonly XLinkedList<BaseManager> _managers = new();

        /// <summary>
        /// 获取指定类型的管理器
        /// </summary>
        /// <typeparam name="T">要获取的管理器类型</typeparam>
        /// <returns>获取到的管理器实例</returns>
        public static T GetManager<T>() where T : BaseManager
        {
            foreach (BaseManager manager in _managers)
            {
                if (manager.GetType() is T t)
                {
                    return t;
                }
            }
            Debug.LogWarning($"Cannot find manager of type {typeof(T).Name}");
            return null;
        }

        /// <summary>
        /// 获取指定名称的管理器
        /// </summary>
        /// <param name="name">要获取的管理器名称</param>
        /// <returns>获取到的管理器实例</returns>
        public static BaseManager GetManager(string name)
        {
            foreach (BaseManager manager in _managers)
            {
                Type type = manager.GetType();
                if (type.Name == name || type.FullName == name)
                {
                    return manager;
                }
            }
            Debug.LogWarning($"Cannot find manager of name {name}");
            return null;
        }

        public static void RegisterManager(BaseManager manager)
        {
            _managers.AddLast(manager);
        }

        public static void UnRegisterManager(BaseManager manager)
        {
            _managers.Remove(manager);
        }

        public static void Shutdown(ShutdownMode mode)
        {

        }
    }

    public enum ShutdownMode
    {
        /// <summary>
        /// 仅关闭游戏框架，不关闭游戏
        /// </summary>
        None,
        /// <summary>
        /// 关闭游戏框架并重启游戏
        /// </summary>
        WithGameRestart,
        /// <summary>
        /// 关闭游戏框架并退出游戏
        /// </summary>
        WithGameQuit,
    }
}