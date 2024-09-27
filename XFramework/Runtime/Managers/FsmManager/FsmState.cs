namespace XFramework
{
    /// <summary>
    /// 状态基类
    /// </summary>
    /// <typeparam name="T">状态机所有者的类型</typeparam>
    /// <remarks>
    /// 每一个状态类型代表状态机所有者的一种状态。
    /// </remarks>
    public abstract class FsmState<T> where T : class
    {
        /// <summary>
        /// 初始化状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnInit(Fsm<T> fsm)
        {
        }

        /// <summary>
        /// 进入状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnEnter(Fsm<T> fsm)
        {
        }

        /// <summary>
        /// 退出状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnExit(Fsm<T> fsm)
        {
        }

        /// <summary>
        /// 更新状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        /// <param name="logicSeconds">逻辑时间</param>
        /// <param name="realSeconds">真实时间</param>
        public virtual void OnUpdate(Fsm<T> fsm, float logicSeconds, float realSeconds)
        {
        }

        /// <summary>
        /// 状态销毁时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnDestroy(Fsm<T> fsm)
        {
        }
    }
}