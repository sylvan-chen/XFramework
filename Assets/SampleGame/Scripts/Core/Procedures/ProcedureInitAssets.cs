using XFramework;
using XFramework.Utils;

public sealed class ProcedureInitAssets : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Global.AssetManager.InitPackageAsync(() =>
        {
            fsm.ChangeState<ProcedurePreload>();
        });
    }
}