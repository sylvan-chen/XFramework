using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 全局入口
    /// </summary>
    public static class Global
    {
        private static EventManager _eventManager;
        private static GameSettingManager _gameSettingManager;
        private static FSMManager _fsmManager;
        private static ProcedureManager _procedureManager;
        private static AssetManager _assetManager;
        private static PoolManager _poolManager;

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

        public static GameSettingManager GameSettingManager
        {
            get
            {
                if (_gameSettingManager == null)
                {
                    _gameSettingManager = XFrameworkDriver.Instance.FindComponent<GameSettingManager>();
                }
                return _gameSettingManager;
            }
        }

        public static FSMManager FSMManager
        {
            get
            {
                if (_fsmManager == null)
                {
                    _fsmManager = XFrameworkDriver.Instance.FindComponent<FSMManager>();
                }
                return _fsmManager;
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