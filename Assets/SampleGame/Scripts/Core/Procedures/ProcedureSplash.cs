using XFramework;

public sealed class ProcedureSplash : Procedure
{
    public override void OnEnter(FSM<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        // TODO: 这里播放闪屏动画
        // ...

        fsm.ChangeState<ProcedureInitAssets>();
    }
}
