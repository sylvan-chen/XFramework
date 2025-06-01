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
        await Global.SceneManager.ChangeSceneAsync("Background");
        await Global.SceneManager.AddSceneAsync("HomeScene");
        await Global.SceneManager.AddSceneAsync("Popup");
        await UniTask.Delay(2000);
        await Global.SceneManager.RemoveSceneAsync("Popup");
        await UniTask.Delay(2000);
        await Global.SceneManager.AddSceneAsync("Popup");
        await UniTask.Delay(2000);
        await Global.SceneManager.RemoveAllScenesAsync("HomeScene");
    }
}