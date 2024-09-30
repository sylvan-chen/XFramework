namespace XFramework
{
    /// <summary>
    /// 全局入口
    /// </summary>
    public static class Global
    {
        private static EventManager _eventManager;
        private static GameSettingManager _gameSettingManager;
        private static FsmManager _fsmManager;
        private static ProcedureManager _procedureManager;

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

        public static FsmManager FsmManager
        {
            get
            {
                if (_fsmManager == null)
                {
                    _fsmManager = RootManager.Instance.GetManager<FsmManager>();
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

        public static void Shutdown()
        {
            RootManager.Instance.ShutdownGame();
        }
    }
}