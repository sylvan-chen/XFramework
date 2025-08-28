using UnityEngine;
using XGame.Utils;

namespace XGame.Core
{
    /// <summary>
    /// 全局入口
    /// </summary>
    public static class Game
    {
        private static CachePool _cachePool;
        private static EventManager _eventManager;
        private static GameSetting _gameSetting;
        private static StateMachineManager _stateMachineManager;
        private static PoolManager _poolManager;
        private static ProcedureManager _procedureManager;
        private static AssetManager _assetManager;
        private static UIManager _uiManager;
        private static TableManager _tableManager;

        public static CachePool CachePool
        {
            get
            {
                _cachePool ??= GameLauncher.Instance.GetFrameworkComponent<CachePool>();
                if (_cachePool.IsShutDown)
                {
                    Log.Error("[Global] CachePool is already shut down but you still try to access it.");
                }
                return _cachePool;
            }
        }

        public static EventManager EventManager
        {
            get
            {
                _eventManager ??= GameLauncher.Instance.GetFrameworkComponent<EventManager>();
                if (_eventManager.IsShutDown)
                {
                    Log.Error("[Global] EventManager is already shut down but you still try to access it.");
                }
                return _eventManager;
            }
        }

        public static GameSetting GameSetting
        {
            get
            {
                _gameSetting ??= GameLauncher.Instance.GetFrameworkComponent<GameSetting>();
                if (_gameSetting.IsShutDown)
                {
                    Log.Error("[Global] GameSetting is already shut down but you still try to access it.");
                }
                return _gameSetting;
            }
        }

        public static StateMachineManager StateMachineManager
        {
            get
            {
                _stateMachineManager ??= GameLauncher.Instance.GetFrameworkComponent<StateMachineManager>();
                if (_stateMachineManager.IsShutDown)
                {
                    Log.Error("[Global] StateMachineManager is already shut down but you still try to access it.");
                }
                return _stateMachineManager;
            }
        }

        public static PoolManager PoolManager
        {
            get
            {
                _poolManager ??= GameLauncher.Instance.GetFrameworkComponent<PoolManager>();
                if (_poolManager.IsShutDown)
                {
                    Log.Error("[Global] PoolManager is already shut down but you still try to access it.");
                }
                return _poolManager;
            }
        }

        public static ProcedureManager ProcedureManager
        {
            get
            {
                _procedureManager ??= GameLauncher.Instance.GetFrameworkComponent<ProcedureManager>();
                if (_procedureManager.IsShutDown)
                {
                    Log.Error("[Global] ProcedureManager is already shut down but you still try to access it.");
                }
                return _procedureManager;
            }
        }

        public static AssetManager AssetManager
        {
            get
            {
                _assetManager ??= GameLauncher.Instance.GetFrameworkComponent<AssetManager>();
                if (_assetManager.IsShutDown)
                {
                    Log.Error("[Global] AssetManager is already shut down but you still try to access it.");
                }
                return _assetManager;
            }
        }

        public static UIManager UIManager
        {
            get
            {
                _uiManager ??= GameLauncher.Instance.GetFrameworkComponent<UIManager>();
                if (_uiManager.IsShutDown)
                {
                    Log.Error("[Global] UIManager is already shut down but you still try to access it.");
                }
                return _uiManager;
            }
        }

        public static TableManager TableManager
        {
            get
            {
                _tableManager ??= GameLauncher.Instance.GetFrameworkComponent<TableManager>();
                if (_tableManager.IsShutDown)
                {
                    Log.Error("[Global] ConfigManager is already shut down but you still try to access it.");
                }
                return _tableManager;
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