using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
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
        private readonly List<XFrameworkComponent> _cachedComponents = new();
        private Coroutine _initCoroutine;

        protected override void Awake()
        {
            base.Awake();

            gameObject.name ??= "[GameLauncher]";
        }

        private void Start()
        {
            UniTask initTask = InitGameAsync();
            _initCoroutine = StartCoroutine(initTask.ToCoroutine());
        }

        private void Update()
        {
            foreach (XFrameworkComponent component in _cachedComponents)
            {
                component.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            ShutdownFramework();
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        private async UniTask InitGameAsync()
        {
            // 预加载配置表
            await PreloadConfigTablesAsync();
            // 开始游戏流程
            Global.ProcedureManager.StartProcedure();
            _initCoroutine = null;
        }

        private async UniTask PreloadConfigTablesAsync()
        {
            Log.Info("[XFramework] [GameLauncher] Preload Config Tables...");
            var fieldInfos = typeof(Consts.ConfigConsts).GetFields(BindingFlags.Public | BindingFlags.Static);
            if (fieldInfos == null || fieldInfos.Length == 0)
            {
                Log.Warning("[XFramework] [GameLauncher] No config constants found to preload.");
                return;
            }

            foreach (var fieldInfo in fieldInfos)
            {
                // 筛选字符串常量字段
                if (fieldInfo != null && fieldInfo.FieldType == typeof(string) && fieldInfo.IsLiteral)
                {
                    Type configType = TypeHelper.GetType(fieldInfo.Name, "XFramework");
                    if (configType == null)
                    {
                        Log.Error($"[XFramework] [GameLauncher] Config type {fieldInfo.Name} not found.");
                        continue;
                    }
                    await ConfigLoader.LoadConfigAsync(fieldInfo.GetValue(null) as string, configType);
                }
            }
        }

        internal void Register(XFrameworkComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component), "Register component failed. Component can not be null.");
            }
            if (_cachedComponents.Contains(component))
            {
                throw new InvalidOperationException($"Register component failed. Component of type {component.GetType().Name} has already been registered.");
            }
            _cachedComponents.Add(component);
            _cachedComponents.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        /// <summary>
        /// 关闭并清理框架
        /// </summary>
        private void ShutdownFramework()
        {
            if (_initCoroutine != null)
            {
                StopCoroutine(_initCoroutine);
                _initCoroutine = null;
            }
            Log.Info("[XFramework] [GameLauncher] Shutdown XFramework...");
            _cachedComponents.Reverse();
            foreach (XFrameworkComponent manager in _cachedComponents)
            {
                manager.Shutdown();
            }
            _cachedComponents.Clear();
        }
    }
}