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
                if (_cachePool == null && GameLauncher.Instance != null)
                {
                    _cachePool = GameLauncher.Instance.FindComponent<CachePool>();
                }
                return _cachePool;
            }
        }

        public static EventManager EventManager
        {
            get
            {
                if (_eventManager == null && GameLauncher.Instance != null)
                {
                    _eventManager = GameLauncher.Instance.FindComponent<EventManager>();
                }
                return _eventManager;
            }
        }

        public static GameSetting GameSetting
        {
            get
            {
                if (_gameSetting == null && GameLauncher.Instance != null)
                {
                    _gameSetting = GameLauncher.Instance.FindComponent<GameSetting>();
                }
                return _gameSetting;
            }
        }

        public static StateMachineManager StateMachineManager
        {
            get
            {
                if (_stateMachineManager == null && GameLauncher.Instance != null)
                {
                    _stateMachineManager = GameLauncher.Instance.FindComponent<StateMachineManager>();
                }
                return _stateMachineManager;
            }
        }

        public static PoolManager PoolManager
        {
            get
            {
                if (_poolManager == null && GameLauncher.Instance != null)
                {
                    _poolManager = GameLauncher.Instance.FindComponent<PoolManager>();
                }
                return _poolManager;
            }
        }

        public static ProcedureManager ProcedureManager
        {
            get
            {
                if (_procedureManager == null && GameLauncher.Instance != null)
                {
                    _procedureManager = GameLauncher.Instance.FindComponent<ProcedureManager>();
                }
                return _procedureManager;
            }
        }

        public static AssetManager AssetManager
        {
            get
            {
                if (_assetManager == null && GameLauncher.Instance != null)
                {
                    _assetManager = GameLauncher.Instance.FindComponent<AssetManager>();
                }
                return _assetManager;
            }
        }

        public static UIManager UIManager
        {
            get
            {
                if (_uiManager == null && GameLauncher.Instance != null)
                {
                    _uiManager = GameLauncher.Instance.FindComponent<UIManager>();
                }
                return _uiManager;
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