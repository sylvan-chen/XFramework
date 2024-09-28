using System;
using UnityEngine;

namespace XFramework
{
    /// <summary>
    /// 流程管理器
    /// </summary>
    public class ProcedureManager : Manager
    {
        [SerializeField]
        private string[] _availableProcedureTypeNames;

        [SerializeField]
        private string _startupProcedureTypeName;

        private Fsm<ProcedureManager> _procedureFsm;

        public Procedure CurrentProcedure
        {
            get => _procedureFsm.CurrentState as Procedure;
        }

        public float CurrentProcedureTime => throw new System.NotImplementedException();

        private void Start()
        {
            // 注册所有流程为状态
            foreach (string typeName in _availableProcedureTypeNames)
            {
                Type type = Type.GetType(typeName);
            }
            // _fsmManager = GlobalManager.Fsm.CreateFsm<ProcedureManager>(this, );
        }

        public T GetProcedure<T>() where T : Procedure
        {
            throw new System.NotImplementedException();
        }

        public bool HasProcedure<T>() where T : Procedure
        {
            throw new System.NotImplementedException();
        }

        public void StartProcedure<T>() where T : Procedure
        {
            throw new System.NotImplementedException();
        }
    }
}