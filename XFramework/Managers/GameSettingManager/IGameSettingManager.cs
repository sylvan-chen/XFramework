namespace XFramework
{
    public interface IGameSettingManager : IManager
    {
        /// <summary>
        /// 帧率
        /// </summary>
        public int FrameRate { get; set; }

        /// <summary>
        /// 游戏速度
        /// </summary>
        public float GameSpeed { get; set; }

        /// <summary>
        /// 允许后台运行
        /// </summary>
        public bool AllowRunInBackground { get; set; }

        /// <summary>
        /// 保持屏幕常亮
        /// </summary>
        public bool NeverSleep { get; set; }

        /// <summary>
        /// 游戏是否暂停
        /// </summary>
        public bool IsGamePaused { get; }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame();

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame();

        /// <summary>
        /// 重置游戏速度
        /// </summary>
        public void ResetGameSpeed();
    }
}