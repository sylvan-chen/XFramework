using System;

namespace XFramework
{
    /// <summary>
    /// 全局入口
    /// </summary>
    public static class Global
    {
        private static IEventManager _eventManager;
        private static IGameSettingManager _gameSettingManager;
        private static IFsmManager _fsmManager;

        public static IEventManager EventManager
        {
            get
            {
                if (_eventManager == null)
                {
                    _eventManager = RootManager.Instance.GetManager<IEventManager>();
                }
                return _eventManager;
            }
        }

        public static IGameSettingManager GameSettingManager
        {
            get
            {
                if (_gameSettingManager == null)
                {
                    _gameSettingManager = RootManager.Instance.GetManager<IGameSettingManager>();
                }
                return _gameSettingManager;
            }
        }

        public static IFsmManager FsmManager
        {
            get
            {
                if (_fsmManager == null)
                {
                    _fsmManager = RootManager.Instance.GetManager<IFsmManager>();
                }
                return _fsmManager;
            }
        }

        public static void Shutdown()
        {
            RootManager.Instance.ShutdownGame();
        }
    }
}