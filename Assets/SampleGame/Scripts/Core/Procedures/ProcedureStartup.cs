using XFramework;

public sealed class ProcedureStartup : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        fsm.ChangeState<ProcedureSplash>();
    }
}
