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
        var uiPanelConfigTable = await ConfigHelper.LoadConfigAsync<UIPanelConfigTable>("uipanel");
        var uiPanelConfig = uiPanelConfigTable.GetConfigById(1);
        // Global.UIManager
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

    void LoadHomeSceneProgressCallBack(float progress)
    {
        Log.Debug($"Load HomeScene Progress: {progress * 100}%");
    }

    void LoadPopupProgressCallBack(float progress)
    {
        Log.Debug($"Load Popup Progress: {progress * 100}%");
    }
}