namespace XFramework
{
    /// <summary>
    /// 有限状态机（泛型无关）
    /// </summary>
    public interface IFsm
    {
        /// <summary>
        /// 状态机的名字
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 更新状态机
        /// </summary>
        /// <param name="logicSeconds">逻辑时间</param>
        /// <param name="realSeconds">真实时间</param>
        public void Update(float logicSeconds, float realSeconds);

        /// <summary>
        /// 销毁状态机
        /// </summary>
        public void Destroy();
    }

    /// <summary>
    /// 有限状态机
    /// </summary>
    /// <typeparam name="T">状态拥有者的类型</typeparam>
    public interface IFsm<T> : IFsm where T : class
    {
        /// <summary>
        /// 拥有状态的对象
        /// </summary>
        public T Owner { get; }

        /// <summary>
        /// 状态数量
        /// </summary>
        public int StateCount { get; }

        /// <summary>
        /// 状态机的当前状态
        /// </summary>
        public IFsmState<T> CurrentState { get; }

        /// <summary>
        /// 状态机的当前状态的持续时间
        /// </summary>
        /// <remarks>
        /// 注意，该值不是历史总时间，每次切换状态时都会重置为 0
        /// </remarks>
        public float CurrentStateTime { get; }

        /// <summary>
        /// 启动状态机
        /// </summary>
        /// <typeparam name="TState">初始状态类型</typeparam>
        public void Start<TState>() where TState : class, IFsmState<T>;

        /// <summary>
        /// 状态是否存在
        /// </summary>
        public bool HasState<TState>() where TState : class, IFsmState<T>;

        /// <summary>
        /// 获取指定类型的状态实例
        /// </summary>
        public TState GetState<TState>() where TState : class, IFsmState<T>;

        /// <summary>
        /// 切换状态
        /// </summary>
        public void ChangeState<TState>() where TState : class, IFsmState<T>;

        /// <summary>
        /// 获取所有状态
        /// </summary>
        public IFsmState<T>[] GetAllStates();
    }
}