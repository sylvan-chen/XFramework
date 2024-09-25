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
            // _fsmManager = GlobalManager.
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