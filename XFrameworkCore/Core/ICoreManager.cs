namespace XFramework
{
    /// <summary>
    /// 核心框架
    /// </summary>
    /// <remarks>
    /// 游戏程序的起点，第一个实例化的对象，负责管理框架和整个游戏的生命周期。
    /// </remarks>
    public interface ICoreManager : IManager
    {
        /// <summary>
        /// 关闭框架
        /// </summary>
        public void ShutdownFramework();

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame();

        /// <summary>
        /// 重启游戏
        /// </summary>
        public void RestartGame();
    }
}