using Cysharp.Threading.Tasks;
using XFramework;

public sealed class ProcedurePreload : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        LoadSceneAsync("HomeScene").Forget();
    }

    public async UniTask LoadSceneAsync(string sceneName)
    {
        await Global.AssetManager.LoadSceneAsync(sceneName);
    }
}