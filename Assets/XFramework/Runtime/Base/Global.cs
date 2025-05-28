using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 全局入口
    /// </summary>
    public static partial class Global
    {
        private static CachePool _cachePool;
        private static EventManager _eventManager;
        private static GameSetting _gameSetting;
        private static StateMachineManager _stateMachineManager;
        private static PoolManager _poolManager;
        private static ProcedureManager _procedureManager;
        private static AssetManager _assetManager;

        public static CachePool CachePool
        {
            get
            {
                if (_cachePool == null)
                {
                    _cachePool = XFrameworkDriver.Instance.FindComponent<CachePool>();
                }
                return _cachePool;
            }
        }

        public static EventManager EventManager
        {
            get
            {
                if (_eventManager == null)
                {
                    _eventManager = XFrameworkDriver.Instance.FindComponent<EventManager>();
                }
                return _eventManager;
            }
        }

        public static GameSetting GameSetting
        {
            get
            {
                if (_gameSetting == null)
                {
                    _gameSetting = XFrameworkDriver.Instance.FindComponent<GameSetting>();
                }
                return _gameSetting;
            }
        }

        public static StateMachineManager StateMachineManager
        {
            get
            {
                if (_stateMachineManager == null)
                {
                    _stateMachineManager = XFrameworkDriver.Instance.FindComponent<StateMachineManager>();
                }
                return _stateMachineManager;
            }
        }

        public static PoolManager PoolManager
        {
            get
            {
                if (_poolManager == null)
                {
                    _poolManager = XFrameworkDriver.Instance.FindComponent<PoolManager>();
                }
                return _poolManager;
            }
        }

        public static ProcedureManager ProcedureManager
        {
            get
            {
                if (_procedureManager == null)
                {
                    _procedureManager = XFrameworkDriver.Instance.FindComponent<ProcedureManager>();
                }
                return _procedureManager;
            }
        }

        public static AssetManager AssetManager
        {
            get
            {
                if (_assetManager == null)
                {
                    _assetManager = XFrameworkDriver.Instance.FindComponent<AssetManager>();
                }
                return _assetManager;
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