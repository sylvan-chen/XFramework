namespace XFramework
{
    /// <summary>
    /// 核心管理器
    /// </summary>
    /// <remarks>
    /// 负责管理框架和整个游戏的生命周期。
    /// </remarks>
    public interface ICoreManager : IManager
    {
        /// <summary>
        /// 关闭游戏
        /// </summary>
        public void QuitGame();

        /// <summary>
        /// 重启游戏
        /// </summary>
        public void RestartGame();

        /// <summary>
        /// 启动框架
        /// </summary>
        /// <remarks>
        /// 实例化所有管理器，加载驱动等。
        /// </remarks>
        public void BootFramework();

        /// <summary>
        /// 关闭框架
        /// </summary>
        /// <remarks>
        /// 清理所有管理器，并销毁框架。
        /// </remarks>
        public void ShutdownFramework();
    }
}