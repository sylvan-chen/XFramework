namespace XFramework
{
    /// <summary>
    /// 全局管理类，框架入口
    /// </summary>
    public static class Global
    {
        private static IFrameworkDriver _driver;

        /// <summary>
        /// 获取指定类型的管理器
        /// </summary>
        /// <typeparam name="T">要获取的管理器类型</typeparam>
        /// <returns>获取到的管理器实例</returns>
        public static T GetManager<T>() where T : class, IModule
        {
            return _driver.GetModule<T>();
        }

        /// <summary>
        /// 注册框架驱动到 Global
        /// </summary>
        /// <param name="supoort">框架驱动实例</param>
        public static void RegisterSupport(IFrameworkDriver supoort)
        {
            XLog.Debug($"[XFramework] [Global] Register {supoort.GetType().Name}.");
            _driver = supoort;
        }

        /// <summary>
        /// 关闭游戏
        /// </summary>
        public static void QuitGame()
        {
            if (CheckDriverRegistered())
            {
                _driver.QuitGame();
            }
        }

        /// <summary>
        /// 重启游戏
        /// </summary>
        public static void RestartGame()
        {
            if (CheckDriverRegistered())
            {
                _driver.RestartGame();
            }
        }

        /// <summary>
        /// 关闭框架
        /// </summary>
        /// <remarks>
        /// 清理所有管理器，并销毁框架。
        /// </remarks>
        public static void ShutdownFramework()
        {
            if (CheckDriverRegistered())
            {
                _driver.ShutdownFramework();
            }
            _driver = null;
        }

        private static bool CheckDriverRegistered()
        {
            if (_driver == null)
            {
                XLog.Fatal("[XFramework] [Global] No FrameworkAdapter registered.");
                return false;
            }
            return true;
        }
    }
}