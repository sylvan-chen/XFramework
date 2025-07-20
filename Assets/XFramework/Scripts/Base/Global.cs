using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 全局入口
    /// </summary>
    public static class Global
    {
        private static CachePool _cachePool;
        private static EventManager _eventManager;
        private static GameSetting _gameSetting;
        private static StateMachineManager _stateMachineManager;
        private static PoolManager _poolManager;
        private static ProcedureManager _procedureManager;
        private static AssetManager _assetManager;
        private static UIManager _uiManager;

        public static CachePool CachePool
        {
            get
            {
                if (_cachePool == null)
                {
                    _cachePool = new CachePool();
                    _cachePool.Init();
                    GameLauncher.Instance.Register(_cachePool);
#if UNITY_EDITOR
                    var cachePoolDebugger = new GameObject("CachePoolDebugger").AddComponent<CachePoolDebugger>();
                    cachePoolDebugger.Init(_cachePool);
#endif
                }
                return _cachePool.IsShutDown ? null : _cachePool;
            }
        }

        public static EventManager EventManager
        {
            get
            {
                if (_eventManager == null)
                {
                    _eventManager = new EventManager();
                    _eventManager.Init();
                    GameLauncher.Instance.Register(_eventManager);
#if UNITY_EDITOR
                    var eventManagerDebugger = new GameObject("EventManagerDebugger").AddComponent<EventManagerDebugger>();
                    eventManagerDebugger.Init(_eventManager);
#endif
                }
                return _eventManager.IsShutDown ? null : _eventManager;
            }
        }

        public static GameSetting GameSetting
        {
            get
            {
                if (_gameSetting == null)
                {
                    _gameSetting = new GameSetting();
                    _gameSetting.Init();
                    GameLauncher.Instance.Register(_gameSetting);
                }
                return _gameSetting.IsShutDown ? null : _gameSetting;
            }
        }

        public static StateMachineManager StateMachineManager
        {
            get
            {
                if (_stateMachineManager == null)
                {
                    _stateMachineManager = new StateMachineManager();
                    _stateMachineManager.Init();
                    GameLauncher.Instance.Register(_stateMachineManager);
                }
                return _stateMachineManager.IsShutDown ? null : _stateMachineManager;
            }
        }

        public static PoolManager PoolManager
        {
            get
            {
                if (_poolManager == null)
                {
                    _poolManager = new PoolManager();
                    _poolManager.Init();
                    GameLauncher.Instance.Register(_poolManager);
#if UNITY_EDITOR
                    var poolManagerDebugger = new GameObject("PoolManagerDebugger").AddComponent<PoolManagerDebugger>();
                    poolManagerDebugger.Init(_poolManager);
#endif
                }
                return _poolManager.IsShutDown ? null : _poolManager;
            }
        }

        public static ProcedureManager ProcedureManager
        {
            get
            {
                if (_procedureManager == null)
                {
                    _procedureManager = new ProcedureManager();
                    _procedureManager.Init();
                    GameLauncher.Instance.Register(_procedureManager);
#if UNITY_EDITOR
                    var procedureManagerDebugger = new GameObject("ProcedureManagerDebugger").AddComponent<ProcedureManagerDebugger>();
                    procedureManagerDebugger.Init(_procedureManager);
#endif
                }
                return _procedureManager.IsShutDown ? null : _procedureManager;
            }
        }

        public static AssetManager AssetManager
        {
            get
            {
                if (_assetManager == null)
                {
                    _assetManager = new AssetManager();
                    _assetManager.Init();
                    GameLauncher.Instance.Register(_assetManager);
                }
                return _assetManager.IsShutDown ? null : _assetManager;
            }
        }

        public static UIManager UIManager
        {
            get
            {
                if (_uiManager == null)
                {
                    _uiManager = new UIManager();
                    _uiManager.Init();
                    GameLauncher.Instance.Register(_uiManager);
                }
                return _uiManager.IsShutDown ? null : _uiManager;
            }
        }

        /// <summary>
        /// 退出游戏程序
        /// </summary>
        public static void Shutdown()
        {
            Log.Info("[XFramework] [Global] Shutdown game...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}