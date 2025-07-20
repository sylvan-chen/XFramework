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
        await Global.UIManager.LoadPanelAsync(100001);
        await Global.UIManager.LoadPanelAsync(100002);
        await Global.UIManager.LoadPanelAsync(100003);

        await Global.AssetManager.LoadSceneAsync("Game01", LoadSceneMode.Single);
        Global.UIManager.OpenPanel(100001);
        await UniTask.Delay(2000);
        Global.UIManager.OpenPanel(100002);
        await UniTask.Delay(3000);
        Global.UIManager.OpenPanel(100003);
        await UniTask.Delay(5000);
        Global.UIManager.ClosePanel(100003);
    }
}