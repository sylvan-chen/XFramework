using XFramework;
using XFramework.Utils;

public sealed class ProcedureCheckUpdate : Procedure
{
    public override void OnEnter(FSM<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Global.AssetManager.CheckUpdateAsync(
            onSucceed: (needUpdate) =>
            {
                if (!needUpdate)
                {
                    fsm.ChangeState<ProcedurePreload>();
                }
                else
                {
                    fsm.ChangeState<ProcedureDownloadUpdate>();
                }
            },
            onFail: (error) =>
            {
                Log.Error($"[XFramework] [ProcedureCheckUpdate] Check update failed: {error}");
            }
        );
    }
}