using Cysharp.Threading.Tasks;
using XFramework;

public sealed class ProcedurePreload : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Preload(fsm).Forget();
    }

    private async UniTaskVoid Preload(StateMachine<ProcedureManager> fsm)
    {
        // Preload resources here
        await Game.UIManager.LoadPanelAsync(100001);
        await Game.UIManager.LoadPanelAsync(100002);
        await Game.UIManager.LoadPanelAsync(100003);

        fsm.ChangeState<ProcedureEnterScene>();
    }
}