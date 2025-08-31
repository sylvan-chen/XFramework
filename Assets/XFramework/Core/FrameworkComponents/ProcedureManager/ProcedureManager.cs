using System;
using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 流程管理器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Procedure Manager")]
    public sealed class ProcedureManager : FrameworkComponent
    {
        private readonly string[] _availableProcedureTypeNames;
        private readonly string _startupProcedureTypeName;

        private StateMachine<ProcedureManager> _procedureStateMachine;
        private ProcedureBase _startupProcedure;

        public ProcedureBase CurrentProcedure => _procedureStateMachine?.CurrentState as ProcedureBase;
        public float CurrentProcedureTime => _procedureStateMachine?.CurrentStateTime ?? 0;

        public ProcedureManager(ProcedureManagerSetting setting)
        {
            _availableProcedureTypeNames = setting.AvailableProcedureTypeNames;
            _startupProcedureTypeName = setting.StartupProcedureTypeName;
        }

        internal override void Init()
        {
            base.Init();

            ProcedureBase[] procedures = new ProcedureBase[_availableProcedureTypeNames.Length];
            // 注册所有流程为状态
            for (int i = 0; i < _availableProcedureTypeNames.Length; i++)
            {
                string typeName = _availableProcedureTypeNames[i];
                Type type = TypeHelper.GetType(typeName) ?? throw new InvalidOperationException($"ProcedureManager init failed. Type '{typeName}' not found.");
                procedures[i] = Activator.CreateInstance(type) as ProcedureBase;
                if (typeName == _startupProcedureTypeName)
                {
                    _startupProcedure = procedures[i];
                }
            }

            if (_startupProcedure == null)
            {
                throw new InvalidOperationException($"ProcedureManager init failed. Startup procedure '{_startupProcedureTypeName}' not found or failed to initialize.");
            }

            _procedureStateMachine = M.StateMachineManager.Create(this, procedures);
        }

        internal override void Shutdown()
        {
            base.Shutdown();

            M.StateMachineManager.Destroy<ProcedureManager>();
            _procedureStateMachine = null;
            _startupProcedure = null;
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            base.Update(deltaTime, unscaledDeltaTime);
        }

        public void StartProcedure()
        {
            _procedureStateMachine.Start(_startupProcedure.GetType());
        }

        public T GetProcedure<T>() where T : ProcedureBase
        {
            return _procedureStateMachine.GetState<T>();
        }

        public bool HasProcedure<T>() where T : ProcedureBase
        {
            return _procedureStateMachine.HasState<T>();
        }
    }
}