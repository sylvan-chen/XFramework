using System;
using System.Collections.Generic;
using System.Linq;

namespace XFramework
{
    /// <summary>
    /// 主要对外入口，外部游戏引擎通过此静态类来与各个管理器进行交互
    /// </summary>
    public static class XFrameworkCore
    {
        private static readonly XLinkedList<BaseManager> _systems = new();

        /// <summary>
        /// 轮询更新所有管理器
        /// </summary>
        /// <param name="logicSeconds">逻辑流逝时间</param>
        /// <param name="realSeconds">真实流逝时间</param>
        public static void Update(float logicSeconds, float realSeconds)
        {
            foreach (BaseManager system in _systems)
            {
                system.Update(logicSeconds, realSeconds);
            }
        }

        /// <summary>
        /// 终止并清理所有管理器
        /// </summary>
        public static void Shutdown()
        {
            // 按优先级倒序遍历，确保先销毁高优先级的管理器
            foreach (BaseManager system in _systems.Reverse())
            {
                system.Shutdown();
            }
            _systems.Clear();
        }

        /// <summary>
        /// 通过接口类型获取指定的管理器
        /// </summary>
        /// <typeparam name="T">要获取的管理器接口类型</typeparam>
        /// <returns>管理器实例</returns>
        /// <remarks>如果管理器不存在，则自动创建该管理器并返回</remarks>
        public static T GetManager<T>() where T : class, IManager
        {
            Type interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("To get a system, generic type T must be an interface.");
            }
            // 接口实现类的类名为接口名去掉 'I'
            string systemTypeFullName = $"{interfaceType.Namespace}.{interfaceType.Name.Substring(1)}";
            Type systemType = Type.GetType(systemTypeFullName) ?? throw new InvalidOperationException($"Cannot find system type {systemTypeFullName}.");
            return AcquireManager(systemType) as T ?? throw new InvalidOperationException($"Cannot get system of type {systemTypeFullName}.");
        }

        /// <summary>
        /// 获取指定类型的管理器，如果管理器不存在，则自动创建该管理器并返回
        /// </summary>
        /// <param name="type">要获取的管理器类型</param>
        /// <returns>管理器实例</returns>
        private static BaseManager AcquireManager(Type type)
        {
            foreach (BaseManager system in _systems)
            {
                if (system.GetType() == type)
                {
                    return system;
                }
            }
            return RegisterManager(type);
        }

        /// <summary>
        /// 注册新的管理器实例，并按优先级插入到链表适当的位置
        /// </summary>
        /// <param name="systemType">要注册的管理器类型</param>
        /// <returns>新的管理器实例</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static BaseManager RegisterManager(Type systemType)
        {
            BaseManager system = Activator.CreateInstance(systemType) as BaseManager ?? throw new InvalidOperationException($"Cannot create system instance of type {systemType.FullName}.");
            // 找到管理器插入链表的位置（优先级从高到低，值从小到大）
            LinkedListNode<BaseManager> targetNode = _systems.First;
            while (targetNode is not null && system.Priority > targetNode.Value.Priority)
            {
                targetNode = targetNode.Next;
            }

            if (targetNode is null)
            {
                _systems.AddLast(system);
            }
            else
            {
                _systems.AddBefore(targetNode, system);
            }
            return system;
        }
    }
}