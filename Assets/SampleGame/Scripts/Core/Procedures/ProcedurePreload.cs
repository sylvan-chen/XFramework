using Cysharp.Threading.Tasks;
using XFramework;

public sealed class ProcedurePreload : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Preload().Forget();
    }

    private async UniTask Preload()
    {
        await Global.SceneManager.LoadSceneAsync("Background");
        await UniTask.Delay(10000);
        await Global.SceneManager.LoadAdditiveSceneAsync("HomeScene");
        await UniTask.Delay(10000);
        await Global.SceneManager.LoadAdditiveSceneAsync("Popup");
        await UniTask.Delay(10000);
        await Global.SceneManager.UnloadSceneAsync("Popup");
        await UniTask.Delay(10000);
        await Global.SceneManager.LoadAdditiveSceneAsync("Popup");
        await UniTask.Delay(10000);
        await Global.SceneManager.UnloadAllScenesAsync("HomeScene");
    }
}