using System;
using System.Collections;
using UnityEngine;

namespace XFramework
{
    /// <summary>
    /// 流程管理器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Procedure Manager")]
    public sealed class ProcedureManager : XFrameworkComponentBase
    {
        [SerializeField]
        private string[] _availableProcedureTypeNames;

        [SerializeField]
        private string _startupProcedureTypeName;

        private FSM<ProcedureManager> _procedureFSM;
        private ProcedureBase _startupProcedure;

        public ProcedureBase CurrentProcedure
        {
            get => _procedureFSM?.CurrentState as ProcedureBase;
        }

        public float CurrentProcedureTime
        {
            get => _procedureFSM == null ? 0 : _procedureFSM.CurrentStateTime;
        }

        private void Start()
        {
            ProcedureBase[] procedures = new ProcedureBase[_availableProcedureTypeNames.Length];
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
                procedures[i] = Activator.CreateInstance(type) as ProcedureBase;
                if (typeName == _startupProcedureTypeName)
                {
                    _startupProcedure = procedures[i];
                }
            }

            if (_startupProcedure == null)
            {
                throw new InvalidOperationException("ProcedureManager init failed. Startup procedure is null.");
            }

            _procedureFSM = Global.FSMManager.CreateFSM(this, procedures);
            StartCoroutine(StartProcedureFSM());
        }

        public T GetProcedure<T>() where T : ProcedureBase
        {
            return _procedureFSM.GetState<T>();
        }

        public bool HasProcedure<T>() where T : ProcedureBase
        {
            return _procedureFSM.HasState<T>();
        }

        /// <summary>
        /// 启动流程状态机
        /// </summary>
        private IEnumerator StartProcedureFSM()
        {
            // 等到帧末，确保所有必要组件都启动完毕
            yield return new WaitForEndOfFrame();
            _procedureFSM.Start(_startupProcedure.GetType());
        }
    }
}