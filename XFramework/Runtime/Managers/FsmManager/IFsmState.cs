namespace XFramework
{
    /// <summary>
    /// 状态机拥有者的状态
    /// </summary>
    /// <typeparam name="T">状态机拥有者的类型</typeparam>
    /// <remarks>
    /// 所有状态都必须继承此接口。
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