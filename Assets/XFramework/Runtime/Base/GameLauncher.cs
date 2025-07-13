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
    [AddComponentMenu("XFramework/Game Launcher")]
    internal sealed class GameLauncher : MonoSingletonPersistent<GameLauncher>
    {
        private readonly Dictionary<Type, XFrameworkComponent> _componentDict = new();
        private readonly List<XFrameworkComponent> _cachedComponents = new();

        protected override void Awake()
        {
            base.Awake();

            Init();
        }

        private void OnDestroy()
        {
            ShutdownFramework();
        }

        private void Init()
        {
            Log.Info("[XFramework] [XFrameworkDriver] Init All XFramework Components...");
            _cachedComponents.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            foreach (XFrameworkComponent component in _cachedComponents)
            {
                component.Init();
            }
        }

        internal void Register(XFrameworkComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component), "Register component failed. Component can not be null.");
            }
            Type componentType = component.GetType();
            if (_componentDict.ContainsKey(componentType) || _cachedComponents.Contains(component))
            {
                throw new InvalidOperationException($"Register component failed. Component of type {component.GetType().Name} has already been registered.");
            }
            _componentDict.Add(componentType, component);
            _cachedComponents.Add(component);
        }

        internal T FindComponent<T>() where T : XFrameworkComponent
        {
            if (_componentDict.TryGetValue(typeof(T), out XFrameworkComponent component))
            {
                return component as T;
            }
            else
            {
                Log.Warning($"[XFramework] [XFrameworkDriver] Can not find component of type {typeof(T).Name}");
                return null;
            }
        }

        internal XFrameworkComponent FindComponent(Type componentType)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType), "Find component failed. Component type can not be null.");
            }
            if (!typeof(XFrameworkComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"Find component failed. Type {componentType.Name} is not a subclass of {nameof(XFrameworkComponent)}.", nameof(componentType));
            }
            if (_componentDict.TryGetValue(componentType, out XFrameworkComponent component))
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
            _cachedComponents.Reverse();
            foreach (XFrameworkComponent manager in _cachedComponents)
            {
                manager.Clear();
            }
            _componentDict.Clear();
            _cachedComponents.Clear();
        }
    }
}