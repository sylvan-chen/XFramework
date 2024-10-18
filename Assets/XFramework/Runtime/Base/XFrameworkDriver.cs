using System;
using System.Collections.Generic;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 驱动框架的根节点
    /// </summary>
    /// <remarks>
    /// 管理框架的各个组件，并保证框架的安全关闭
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/XFramework Driver")]
    internal sealed class XFrameworkDriver : MonoSingletonPersistent<XFrameworkDriver>
    {
        private readonly Dictionary<Type, XFrameworkComponentBase> _componentDict = new();

        private void OnDestroy()
        {
            Log.Info("[XFramework] [XFrameworkDriver] Destroy XFrameworkDriver...");
            ShutdownFramework();
        }

        public void Register(XFrameworkComponentBase component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component), "Register component failed. Component can not be null.");
            }
            Type componentType = component.GetType();
            if (_componentDict.ContainsKey(componentType))
            {
                throw new InvalidOperationException($"Register component failed. Component of type {component.GetType().Name} has already been registered.");
            }
            _componentDict.Add(componentType, component);
        }

        public T FindComponent<T>() where T : XFrameworkComponentBase
        {
            if (_componentDict.TryGetValue(typeof(T), out XFrameworkComponentBase component))
            {
                return component as T;
            }
            else
            {
                Log.Warning($"[XFramework] [XFrameworkDriver] Can not find component of type {typeof(T).Name}");
                return null;
            }
        }

        public XFrameworkComponentBase FindComponent(Type componentType)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType), "Find component failed. Component type can not be null.");
            }
            if (!typeof(XFrameworkComponentBase).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"Find component failed. Type {componentType.Name} is not a subclass of {nameof(XFrameworkComponentBase)}.", nameof(componentType));
            }
            if (_componentDict.TryGetValue(componentType, out XFrameworkComponentBase component))
            {
                return component;
            }
            else
            {
                Log.Warning($"[XFramework] [XFrameworkDriver] Can not find component of type {componentType.Name}");
                return null;
            }
        }

        /// <summary>
        /// 关闭并清理框架
        /// </summary>
        private void ShutdownFramework()
        {
            Log.Info("[XFramework] [XFrameworkDriver] Shutdown XFramework...");
            foreach (XFrameworkComponentBase manager in _componentDict.Values)
            {
                DestroyImmediate(manager.gameObject);
            }
            _componentDict.Clear();
            CachePool.Clear();
        }
    }
}