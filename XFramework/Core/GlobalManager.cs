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
            get => _eventManager;
        }

        public static IGameSettingManager GameSetting
        {
            get => _gameSettingManager;
        }

        public static IFsmManager Fsm
        {
            get => _fsmManager;
        }

        /// <summary>
        /// 注册管理器到 Global
        /// </summary>
        /// <typeparam name="T">要注册的管理器接口类型</typeparam>
        /// <param name="manager">管理器实例</param>
        public static void Register<T>(T manager) where T : class, IManager
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager), $"Register manager failed. The manager is null.");
            }
            if (!typeof(T).IsInterface)
            {
                throw new ArgumentException($"Register manager failed. The registered type {typeof(T).Name} must be an interface.", nameof(T));
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
                case "IFsmManager":
                    _fsmManager = manager as IFsmManager;
                    break;
                default:
                    throw new NotSupportedException($"Register manager failed. The registered type {typeof(T).Name} is not supported by XFramework.");
            }
        }
    }
}