using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using XFramework;

public sealed class ProcedureEnterScene : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        EnterScene().Forget();
    }

    private async UniTaskVoid EnterScene()
    {
        await Global.AssetManager.LoadSceneAsync("Game02", LoadSceneMode.Single);
    }
}
