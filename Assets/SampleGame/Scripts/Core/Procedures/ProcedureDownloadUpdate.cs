using XFramework;
using XFramework.Utils;

public sealed class ProcedureDownloadUpdate : Procedure
{
    public override void OnEnter(FSM<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Global.AssetManager.DwonloadUpdateAsync(
            onSucceed: () =>
            {
                fsm.ChangeState<ProcedurePreload>();
            },
            onDownloading: (totalDownloadCount, currentDownloadCount, totalDownloadBytes, currentDownloadBytes) =>
            {
                Log.Info($"[XFramework] [ProcedureDownloadUpdate] Downloading update: {currentDownloadCount}/{totalDownloadCount}, {currentDownloadBytes}/{totalDownloadBytes}");
            },
            onFail: (error) =>
            {
                Log.Error($"[XFramework] [ProcedureDownloadUpdate] ProcedureDownloadUpdate failed: {error}");
            }
        );
    }
}