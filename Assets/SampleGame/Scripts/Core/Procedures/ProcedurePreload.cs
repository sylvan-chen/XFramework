using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using XFramework;
using XFramework.Utils;

public sealed class ProcedurePreload : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Preload().Forget();
    }

    private async UniTask Preload()
    {
        await Global.AssetManager.LoadSceneAsync("Game01", LoadSceneMode.Single);
        await Global.UIManager.OpenPanelAsync(100001);
        await UniTask.Delay(2000);
        await Global.UIManager.OpenPanelAsync(100002);
        await UniTask.Delay(3000);
        await Global.UIManager.OpenPanelAsync(100003);
        await UniTask.Delay(5000);
        Global.UIManager.ClosePanel(100003);
    }
}