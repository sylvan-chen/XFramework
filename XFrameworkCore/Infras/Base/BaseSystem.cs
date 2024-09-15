namespace XFramework
{
    /// <summary>
    /// 所有系统的基类，不对程序集外部暴露
    /// </summary>
    internal abstract class BaseSystem
    {
        public BaseSystem()
        {
        }

        /// <summary>
        /// 系统的轮询优先级
        /// </summary>
        /// <remarks>
        /// 只读属性。所有系统都由 `XGameCore` 集中管理，并且按照优先级轮询执行更新操作。
        /// 优先级越高（值越小）的系统在轮询中越早更新，在终止时越晚停止。
        /// </remarks>
        internal abstract int Priority { get; }

        /// <summary>
        /// 更新系统
        /// </summary>
        /// <param name="logicSeconds">逻辑流逝时间</param>
        /// <param name="realSeconds">真实流逝时间</param>
        internal abstract void Update(float logicSeconds, float realSeconds);

        /// <summary>
        /// 销毁系统
        /// </summary>
        internal abstract void Shutdown();
    }
}