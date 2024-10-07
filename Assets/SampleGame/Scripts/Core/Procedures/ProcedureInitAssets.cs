using XFramework;
using XFramework.Utils;

public sealed class ProcedureInitAssets : Procedure
{
    public override void OnEnter(FSM<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Global.AssetManager.InitAsync(
        onSuccess: () =>
        {
            Log.Debug("AssetManager Init Success");
        },
        onFailed: () =>
        {
            Log.Error("AssetManager Init Failed");
        });
    }
}