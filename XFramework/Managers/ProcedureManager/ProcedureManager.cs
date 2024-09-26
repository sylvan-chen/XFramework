using System;
using UnityEngine;

namespace XFramework
{
    public class ProcedureManager : MonoBehaviour, IProcedureManager
    {
        [SerializeField]
        private string[] _procedureTypeNames;

        [SerializeField]
        private string _startupProcedureTypeName;

        private IFsm<ProcedureManager> _procedureFsm;

        public BaseProcedure CurrentProcedure
        {
            get => _procedureFsm.CurrentState as BaseProcedure;
        }

        public float CurrentProcedureTime => throw new System.NotImplementedException();

        private void Start()
        {
            // 注册所有流程为状态
            foreach (string typeName in _procedureTypeNames)
            {
                Type type = Type.GetType(typeName);
            }
            // _fsmManager = GlobalManager.Fsm.CreateFsm<ProcedureManager>(this, );
        }

        public T GetProcedure<T>() where T : BaseProcedure
        {
            throw new System.NotImplementedException();
        }

        public bool HasProcedure<T>() where T : BaseProcedure
        {
            throw new System.NotImplementedException();
        }

        public void StartProcedure<T>() where T : BaseProcedure
        {
            throw new System.NotImplementedException();
        }
    }
}