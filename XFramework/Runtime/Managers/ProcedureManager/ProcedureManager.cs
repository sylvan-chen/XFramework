using System;
using System.Collections;
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
        private Procedure _startupProcedure;

        public Procedure CurrentProcedure
        {
            get => _procedureFsm?.CurrentState as Procedure;
        }

        public float CurrentProcedureTime
        {
            get => _procedureFsm == null ? 0 : _procedureFsm.CurrentStateTime;
        }

        private void Start()
        {
            Procedure[] procedures = new Procedure[_availableProcedureTypeNames.Length];
            // 注册所有流程为状态
            for (int i = 0; i < _availableProcedureTypeNames.Length; i++)
            {
                string typeName = _availableProcedureTypeNames[i];
                Type type = TypeHelper.GetType(typeName);
                if (type == null)
                {
                    Debug.LogError($"Can not find type {typeName}");
                    continue;
                }
                procedures[i] = Activator.CreateInstance(type) as Procedure ?? throw new InvalidOperationException($"Crate instance of procedure {typeName} failed.");
                if (typeName == _startupProcedureTypeName)
                {
                    _startupProcedure = procedures[i];
                }
            }

            if (_startupProcedure == null)
            {
                throw new InvalidOperationException("ProcedureManager init failed. Startup procedure is null.");
            }

            _procedureFsm = Global.FsmManager.CreateFsm(this, procedures);
            StartCoroutine(StartProcedureFsm());
        }

        public T GetProcedure<T>() where T : Procedure
        {
            return _procedureFsm.GetState<T>();
        }

        public bool HasProcedure<T>() where T : Procedure
        {
            return _procedureFsm.HasState<T>();
        }

        /// <summary>
        /// 启动流程状态机
        /// </summary>
        private IEnumerator StartProcedureFsm()
        {
            yield return new WaitForEndOfFrame();
            _procedureFsm.Start(_startupProcedure.GetType());
        }
    }
}