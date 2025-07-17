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
        await Global.UIManager.OpenPanelAsync(100001);
        await UniTask.Delay(5000);
        await Global.UIManager.OpenPanelAsync(100003);
        await UniTask.Delay(5000);
        Global.UIManager.ClosePanel(100003);
        // await Global.AssetManager.LoadSceneAsync("Background", LoadSceneMode.Additive);
        // await UniTask.Delay(6000);
        // await Global.AssetManager.LoadSceneAsync("HomeScene", LoadSceneMode.Additive, progressCallback: LoadHomeSceneProgressCallBack);
        // await UniTask.Delay(6000);
        // await Global.AssetManager.LoadSceneAsync("Popup", LoadSceneMode.Additive, progressCallback: LoadPopupProgressCallBack);
        // await UniTask.Delay(6000);
        // await Global.AssetManager.UnloadSceneAsync("Popup");
        // await UniTask.Delay(6000);
        // await Global.AssetManager.LoadSceneAsync("Popup", LoadSceneMode.Additive, progressCallback: LoadPopupProgressCallBack);
        // await UniTask.Delay(6000);
        // await Global.AssetManager.UnloadAllScenesExceptAsync("Startup", "HomeScene");
    }
}