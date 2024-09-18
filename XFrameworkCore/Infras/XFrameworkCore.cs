using System;
using System.Collections.Generic;
using System.Linq;
using XFramework.Unity;

namespace XFramework
{
    /// <summary>
    /// 框架模块全局管理类，外部通过此类来与各个模块进行交互
    /// </summary>
    public static class XFrameworkCore
    {
        private static readonly XLinkedList<BaseModule> _modules = new();

        /// <summary>
        /// 轮询更新所有模块
        /// </summary>
        /// <param name="logicSeconds">逻辑流逝时间</param>
        /// <param name="realSeconds">真实流逝时间</param>
        public static void Update(float logicSeconds, float realSeconds)
        {
            foreach (BaseModule module in _modules)
            {
                module.Update(logicSeconds, realSeconds);
            }
        }

        /// <summary>
        /// 关闭游戏框架，清理所有模块
        /// </summary>
        public static void Shutdown()
        {
            // 按优先级倒序遍历，先清理高优先级的模块
            foreach (BaseModule module in _modules.Reverse())
            {
                module.Shutdown();
            }
            _modules.ClearEntirely();
        }

        /// <summary>
        /// 通过接口类型获取指定的模块
        /// </summary>
        /// <typeparam name="T">要获取的模块接口类型</typeparam>
        /// <returns>模块实例</returns>
        /// <remarks>如果模块不存在，则自动创建该模块并返回</remarks>
        public static T GetModule<T>() where T : class, IModule
        {
            Type interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("To get a system, generic type T must be an interface.");
            }
            // 接口实现类的类名为接口名去掉 'I'
            string moduleTypeFullName = $"{interfaceType.Namespace}.{interfaceType.Name.Substring(1)}";
            Type moduleType = Type.GetType(moduleTypeFullName) ?? throw new InvalidOperationException($"Cannot find system type {moduleTypeFullName}.");
            return AcquireModule(moduleType) as T ?? throw new InvalidOperationException($"Cannot get system of type {moduleTypeFullName}.");
        }

        /// <summary>
        /// 获取指定类型的模块，如果模块不存在，则自动创建该模块并返回
        /// </summary>
        /// <param name="type">要获取的模块类型</param>
        /// <returns>模块实例</returns>
        private static BaseModule AcquireModule(Type type)
        {
            foreach (BaseModule module in _modules)
            {
                if (module.GetType() == type)
                {
                    return module;
                }
            }
            return CreateModule(type);
        }

        /// <summary>
        /// 创建新的模块实例，并按优先级插入到链表适当的位置
        /// </summary>
        /// <param name="moduleType">要注册的模块类型</param>
        /// <returns>新的模块实例</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static BaseModule CreateModule(Type moduleType)
        {
            BaseModule module = Activator.CreateInstance(moduleType) as BaseModule ?? throw new InvalidOperationException($"Cannot create system instance of type {moduleType.FullName}.");
            // 找到模块插入链表的位置（优先级从高到低，值从小到大）
            LinkedListNode<BaseModule> targetNode = _modules.First;
            while (targetNode is not null && module.Priority > targetNode.Value.Priority)
            {
                targetNode = targetNode.Next;
            }

            if (targetNode is null)
            {
                _modules.AddLast(module);
            }
            else
            {
                _modules.AddBefore(targetNode, module);
            }
            return module;
        }
    }
}