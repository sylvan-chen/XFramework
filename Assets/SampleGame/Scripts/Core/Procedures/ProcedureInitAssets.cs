using XFramework;
using XFramework.Utils;

public sealed class ProcedureInitAssets : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Global.AssetManager.InitAsync(
            onSucceed: () =>
            {
                fsm.ChangeState<ProcedureCheckUpdate>();
            },
            onFail: (error) =>
            {
                Log.Error($"[XFramework] [ProcedureInitAssets] Init Resource failed. {error}");
            }
        );
    }
}