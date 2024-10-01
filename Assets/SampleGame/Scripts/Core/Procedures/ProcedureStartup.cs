using XFramework;

public class ProcedureStartup : Procedure
{
    public override void OnEnter(FSM<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        fsm.ChangeState<ProcedureSplash>();
    }
}
