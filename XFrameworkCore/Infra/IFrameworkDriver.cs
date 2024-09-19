namespace XFramework
{
    /// <summary>
    /// 框架驱动
    /// </summary>
    /// <remarks>
    /// 负责创建和获取框架各个模块，管理各个模块的生命周期以及整个游戏程序的运行和终止，
    /// 不同的游戏引擎需要实现不同的版本，在游戏进程启动时将实例注册到 Global。
    /// </remarks>
    public interface IFrameworkDriver
    {
        public T GetModule<T>() where T : class, IModule;

        /// <summary>
        /// 更新各模块
        /// </summary>
        public void Update();

        /// <summary>
        /// 关闭游戏
        /// </summary>
        public void QuitGame();

        /// <summary>
        /// 重启游戏
        /// </summary>
        public void RestartGame();

        /// <summary>
        /// 关闭框架
        /// </summary>
        /// <remarks>
        /// 清理所有管理器，并销毁框架。
        /// </remarks>
        public void ShutdownFramework();
    }
}