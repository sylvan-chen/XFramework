using System;

namespace XFramework
{
    /// <summary>
    /// 全局管理类，框架入口
    /// </summary>
    public static class Global
    {
        private static ICoreManager _coreManager;
        private static IEventManager _eventManager;
        private static IGameSettingManager _gameSettingManager;

        public static ICoreManager CoreManager
        {
            get { return _coreManager ?? throw new NullReferenceException("ICoreManager not registered."); }
        }

        public static IEventManager EventManager
        {
            get { return _eventManager ?? throw new NullReferenceException("IEventManager not registered."); }
        }

        public static IGameSettingManager GameSettingManager
        {
            get { return _gameSettingManager ?? throw new NullReferenceException("IGameSettingManager not registered."); }
        }

        /// <summary>
        /// 注册管理器到 Global
        /// </summary>
        /// <typeparam name="T">要注册的管理器接口类型</typeparam>
        /// <param name="manger">管理器实例</param>
        public static void RegisterManager<T>(T manger) where T : class, IManager
        {
            if (!typeof(T).IsInterface)
            {
                XLog.Error($"[XFramework] [Global] Register {manger.GetType().Name} of generic type {typeof(T).Name} failed. The generic type T must be an interface.");
                return;
            }
            if (manger == null)
            {
                XLog.Error($"[XFramework] [Global] Register manager of generic type {typeof(T).Name} failed. The manager is null.");
                return;
            }
            // XLog.Debug($"[XFramework] [Global] Register {manger.GetType().Name} of generic type {typeof(T).Name}.");
            switch (typeof(T).Name)
            {
                case "ICoreManager":
                    _coreManager = manger as ICoreManager;
                    break;
                case "IEventManager":
                    _eventManager = manger as IEventManager;
                    break;
                case "IGameSettingManager":
                    _gameSettingManager = manger as IGameSettingManager;
                    break;
                default:
                    XLog.Error($"[XFramework] [Global] Register {manger.GetType().Name} of generic type {typeof(T).Name} failed. {typeof(T).Name} is not supported bt XFramework yet.");
                    break;
            }
        }
    }
}