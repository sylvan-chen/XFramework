using System;
using System.Collections.Generic;
using UnityEngine;

namespace XFramework
{
    public class ProcedureManager : MonoBehaviour, IProcedureManager
    {
        [SerializeField]
        private string[] _procedureTypeNames;

        [SerializeField]
        private string _startupProcedureTypeName;

        private IFsmManager _fsmManager;

        public BaseProcedure CurrentProcedure
        {
            get;
        }

        public float CurrentProcedureTime => throw new System.NotImplementedException();

        private void Start()
        {
            foreach (string typeName in _procedureTypeNames)
            {

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