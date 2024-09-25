using System;

namespace XFramework
{
    /// <summary>
    /// 全局管理类，框架入口
    /// </summary>
    public static class GlobalManager
    {
        private static IEventManager _eventManager;
        private static IGameSettingManager _gameSettingManager;
        private static IFsmManager _fsmManager;

        public static IEventManager Event
        {
            get => _eventManager ?? throw new NullReferenceException("[XFramework] [Global] IEventManager not registered.");
        }

        public static IGameSettingManager GameSetting
        {
            get => _gameSettingManager ?? throw new NullReferenceException("[XFramework] [Global] IGameSettingManager not registered.");
        }

        public static IFsmManager Fsm
        {
            get => _fsmManager ?? throw new NullReferenceException("[XFramework] [Global] IFsmManager not registered.");
        }

        /// <summary>
        /// 注册管理器到 Global
        /// </summary>
        /// <typeparam name="T">要注册的管理器接口类型</typeparam>
        /// <param name="manager">管理器实例</param>
        public static void Register<T>(T manager) where T : class, IManager
        {
            if (!typeof(T).IsInterface)
            {
                throw new ArgumentException($"[XFramework] [Global] Register {manager.GetType().Name} of generic type {typeof(T).Name} failed. The generic type T must be an interface.");
            }
            if (manager == null)
            {
                throw new NullReferenceException($"[XFramework] [Global] Register manager of generic type {typeof(T).Name} failed. The manager is null.");
            }
            XLog.Debug($"[XFramework] [Global] {typeof(T).Name} implemented by {manager.GetType().Name} registered.");
            switch (typeof(T).Name)
            {
                case "IEventManager":
                    _eventManager = manager as IEventManager;
                    break;
                case "IGameSettingManager":
                    _gameSettingManager = manager as IGameSettingManager;
                    break;
                default:
                    throw new NotSupportedException($"[XFramework] [Global] Register {manager.GetType().Name} implementing {typeof(T).Name} failed. {typeof(T).Name} is not supported bt XFramework yet.");
            }
        }
    }
}