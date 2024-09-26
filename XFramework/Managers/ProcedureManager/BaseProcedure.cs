namespace XFramework
{
    /// <summary>
    /// 所有流程的基类
    /// </summary>
    /// <remarks>
    /// 所有流程都需要继承自此类，它其实就是 ProcedureManager 的一个状态。
    /// </remarks>
    public abstract class BaseProcedure : IFsmState<ProcedureManager>
    {
        public virtual void OnDestroy(IFsm<ProcedureManager> fsm)
        {
        }

        public virtual void OnEnter(IFsm<ProcedureManager> fsm)
        {
        }

        public virtual void OnExit(IFsm<ProcedureManager> fsm)
        {
        }

        public virtual void OnInit(IFsm<ProcedureManager> fsm)
        {
        }

        public virtual void OnUpdate(IFsm<ProcedureManager> fsm, float logicSeconds, float realSeconds)
        {
        }
    }
}