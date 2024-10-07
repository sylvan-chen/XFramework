using XFramework;
using XFramework.Utils;

public sealed class ProcedureInitAssets : Procedure
{
    public override void OnEnter(FSM<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Global.ResourceManager.InitAsync(
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