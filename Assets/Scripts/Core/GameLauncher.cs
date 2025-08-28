using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XGame.Utils;

namespace XGame.Core
{
    /// <summary>
    /// 总启动器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Game Launcher")]
    internal sealed class GameLauncher : MonoSingletonPersistent<GameLauncher>
    {
        [Header("XFramework Component Settings")]
        [SerializeField] private TableManagerSetting _tableManagerSetting = null;
        [SerializeField] private AssetManagerSetting _assetManagerSetting = null;

        private readonly List<FrameworkComponent> _cachedComponents = new();
        private readonly Dictionary<Type, FrameworkComponent> _componentMap = new();

        public bool IsInitialized { get; private set; } = false;

        protected override void Awake()
        {
            base.Awake();

            gameObject.name ??= "[GameLauncher]";
        }

        private void Start()
        {
            // 加载所有管理器组件
            // 分四层 Base -> Core -> System -> Game
            // 上层依赖下层，下层不可依赖上层
            LoadBaseComponents();
            LoadCoreComponents();
            LoadSystemComponents();
            LoadGameComponents();

            InitComponents().Forget();
            EnterGame().Forget();
        }

        private async UniTaskVoid InitComponents()
        {
            foreach (FrameworkComponent component in _cachedComponents)
            {
                component.Init();
                await UniTask.NextFrame(); // 等待一帧让组件完成初始化
            }

            IsInitialized = true;
        }

        private async UniTaskVoid EnterGame()
        {
            await UniTask.WaitUntil(() => IsInitialized);

            var procedureManager = GetFrameworkComponent<ProcedureManager>();
            procedureManager.StartProcedure();
        }

        private void Update()
        {
            foreach (FrameworkComponent component in _cachedComponents)
            {
                component.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            ShutdownFramework();
        }

        public T GetFrameworkComponent<T>() where T : FrameworkComponent
        {
            _componentMap.TryGetValue(typeof(T), out var component);
            return component as T;
        }

        public FrameworkComponent GetFrameworkComponent(Type type)
        {
            _componentMap.TryGetValue(type, out var component);
            return component;
        }

        /// <summary>
        /// 加载Base层组件
        /// </summary>
        private void LoadBaseComponents()
        {
            var cachePool = new CachePool();
            CacheComponentInstance(typeof(CachePool), cachePool);

            var gameSetting = new GameSetting();
            CacheComponentInstance(typeof(GameSetting), gameSetting);
        }

        /// <summary>
        /// 加载Core层组件
        /// </summary>
        private void LoadCoreComponents()
        {
            var poolManager = new PoolManager();
            CacheComponentInstance(typeof(PoolManager), poolManager);

            var configManager = new TableManager(_tableManagerSetting);
            CacheComponentInstance(typeof(TableManager), configManager);

            var assetManager = new AssetManager(_assetManagerSetting);
            CacheComponentInstance(typeof(AssetManager), assetManager);

            var eventManager = new EventManager();
            CacheComponentInstance(typeof(EventManager), eventManager);

            var stateMachineManager = new StateMachineManager();
            CacheComponentInstance(typeof(StateMachineManager), stateMachineManager);
        }

        /// <summary>
        /// 加载System层组件
        /// </summary>
        private void LoadSystemComponents()
        {
            var uiManager = new UIManager();
            CacheComponentInstance(typeof(UIManager), uiManager);
        }

        /// <summary>
        /// 加载Game层组件
        /// </summary>
        private void LoadGameComponents()
        {
            var procedureManager = new ProcedureManager();
            CacheComponentInstance(typeof(ProcedureManager), procedureManager);
        }

        private void CacheComponentInstance(Type componentType, FrameworkComponent instance)
        {
            if (_cachedComponents.Contains(instance) || _componentMap.ContainsKey(componentType))
            {
                Log.Warning($"[GameLauncher] Duplicate component cache attempted: {componentType.Name}");
                return;
            }

            _cachedComponents.Add(instance);
            _componentMap[componentType] = instance;
        }

        /// <summary>
        /// 关闭并清理框架
        /// </summary>
        private void ShutdownFramework()
        {
            Log.Debug("[GameLauncher] Shutdown XFramework...");
            _cachedComponents.Reverse();
            foreach (FrameworkComponent component in _cachedComponents)
            {
                component.Shutdown();
            }
            _cachedComponents.Clear();
        }
    }
}