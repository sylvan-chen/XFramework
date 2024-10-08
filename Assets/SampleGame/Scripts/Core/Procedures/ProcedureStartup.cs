using XFramework;

public sealed class ProcedureStartup : ProcedureBase
{
    public override void OnEnter(FSM<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        fsm.ChangeState<ProcedureSplash>();
    }
}
