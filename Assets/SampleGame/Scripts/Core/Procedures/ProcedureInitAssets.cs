using Cysharp.Threading.Tasks;
using XFramework;

public sealed class ProcedureInitAssets : ProcedureBase
{
    private StateMachine<ProcedureManager> _fsm;

    public override async void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        _fsm = fsm;

        await Global.AssetManager.InitPackageAsync();

        _fsm.ChangeState<ProcedurePreload>();
    }

    public override void Destroy()
    {
        base.Destroy();
    }
}