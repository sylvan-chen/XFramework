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
        private static ResourceManager _assetManager;

        public static EventManager EventManager
        {
            get
            {
                if (_eventManager == null)
                {
                    _eventManager = RootManager.Instance.GetManager<EventManager>();
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
                    _gameSettingManager = RootManager.Instance.GetManager<GameSettingManager>();
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
                    _fsmManager = RootManager.Instance.GetManager<FSMManager>();
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
                    _procedureManager = RootManager.Instance.GetManager<ProcedureManager>();
                }
                return _procedureManager;
            }
        }

        public static ResourceManager AssetManager
        {
            get
            {
                if (_assetManager == null)
                {
                    _assetManager = RootManager.Instance.GetManager<ResourceManager>();
                }
                return _assetManager;
            }
        }

        public static void Shutdown()
        {
            RootManager.Instance.ShutdownGame();
        }
    }
}