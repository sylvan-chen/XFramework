using XFramework;
using XFramework.Utils;

public sealed class ProcedureInitAssets : ProcedureBase
{
    private StateMachine<ProcedureManager> _fsm;

    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        _fsm = fsm;

        Global.AssetManager.OnInitializeSucceedEvent += OnAssetManagerInitSucceed;
        Global.AssetManager.InitPackageAsync();
    }

    private void OnAssetManagerInitSucceed()
    {
        _fsm.ChangeState<ProcedurePreload>();
    }

    public override void Destroy()
    {
        base.Destroy();

        Global.AssetManager.OnInitializeSucceedEvent -= OnAssetManagerInitSucceed;
    }
}