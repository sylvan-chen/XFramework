namespace XFramework
{
    /// <summary>
    /// 游戏流程管理器
    /// </summary>
    public interface IProcedureManager
    {
        public BaseProcedure CurrentProcedure { get; }

        public float CurrentProcedureTime { get; }

        public void StartProcedure<T>() where T : BaseProcedure;

        public T GetProcedure<T>() where T : BaseProcedure;

        public bool HasProcedure<T>() where T : BaseProcedure;
    }
}