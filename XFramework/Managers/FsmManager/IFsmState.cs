namespace XFramework
{
    /// <summary>
    /// 用于状态机的目标对象类型的状态
    /// </summary>
    /// <typeparam name="T">目标对象类型</typeparam>
    /// <remarks>
    /// 所有状态都必须继承自此基类，可自由选择实现哪些方法。
    /// </remarks>
    public interface IFsmState<T> where T : class
    {
        /// <summary>
        /// 初始化状态时
        /// </summary>
        public void OnInit(IFsm<T> fsm);

        /// <summary>
        /// 进入状态时
        /// </summary>
        public void OnEnter(IFsm<T> fsm);

        /// <summary>
        /// 退出状态时
        /// </summary>
        public void OnExit(IFsm<T> fsm);

        /// <summary>
        /// 更新状态时
        /// </summary>
        public void OnUpdate(IFsm<T> fsm, float logicSeconds, float realSeconds);

        /// <summary>
        /// 状态销毁时
        /// </summary>
        public void OnDestroy(IFsm<T> fsm);
    }
}