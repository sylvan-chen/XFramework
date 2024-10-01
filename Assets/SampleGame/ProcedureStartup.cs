using XFramework;
using XFramework.Utils;

public class ProcedureStartup : Procedure
{
    private float _runningTime = 0f;

    public override void OnUpdate(FSM<ProcedureManager> fsm, float deltaTime, float unscaledDeltaTime)
    {
        base.OnUpdate(fsm, deltaTime, unscaledDeltaTime);

        _runningTime += unscaledDeltaTime;
        if (_runningTime > 5)
        {
            fsm.ChangeState<ProcedureSplash>();
        }
    }
}
