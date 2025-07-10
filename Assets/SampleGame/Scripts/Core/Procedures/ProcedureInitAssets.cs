using Cysharp.Threading.Tasks;
using XFramework;
using XFramework.Utils;

public sealed class ProcedureInitAssets : ProcedureBase
{
    private StateMachine<ProcedureManager> _fsm;

    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        _fsm = fsm;
        InitPackage().Forget();
    }

    private async UniTask InitPackage()
    {
        var initResult = await Global.AssetManager.InitPackageAsync();

        if (initResult.Succeed)
        {
            Log.Debug($"[ProcedureInitAssets] Init package succeed. (Version {initResult.PackageVersion})");

            _fsm.ChangeState<ProcedurePreload>();
        }
        else
        {
            Log.Error($"[ProcedureInitAssets] Init package failed: {initResult.ErrorMessage}");
        }
    }

    public override void Destroy()
    {
        base.Destroy();
    }
}