using XFramework;

public sealed class ProcedureSplash : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        // TODO: 这里播放闪屏动画
        // ...

        // fsm.ChangeState<ProcedureInitAssets>();
    }
}
