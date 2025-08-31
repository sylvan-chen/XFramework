using Cysharp.Threading.Tasks;
using XGame.Core;
using XGame.Utils;

public class ProcedureStartup : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Log.Debug("Enter ProcedureStartup");

        fsm.ChangeState<ProcedureInitAsset>();
    }
}
