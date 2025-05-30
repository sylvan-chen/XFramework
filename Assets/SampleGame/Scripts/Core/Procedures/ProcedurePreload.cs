using UnityEngine.SceneManagement;
using XFramework;

public sealed class ProcedurePreload : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        // 加载游戏主场景
        SceneManager.LoadSceneAsync("Home", LoadSceneMode.Additive);
    }
}