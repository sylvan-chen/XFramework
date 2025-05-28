using XFramework;
using XFramework.Utils;

public sealed class ProcedureCheckUpdate : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
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