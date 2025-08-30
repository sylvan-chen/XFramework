using Cysharp.Threading.Tasks;
using XGame.Core;
using XGame.Utils;

public class ProcedureEnterGame : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Log.Debug("[ProcedureEnterGame] Enter Game");

        M.AssetManager.LoadSceneAsync("Test").Forget();
    }
}
