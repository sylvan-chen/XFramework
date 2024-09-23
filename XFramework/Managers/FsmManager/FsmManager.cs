using System.Collections.Generic;
using UnityEngine;

namespace XFramework
{
    public class FsmManager : MonoBehaviour, IFsmManager
    {
        private readonly Dictionary<string, IFsm> _fsms = new();

        private const string DEFAULT_FSM_ID = "default";

        public IFsm<T> CreateFsm<T>(T owner, IFsmState<T> states) where T : class
        {
            return CreateFsm(DEFAULT_FSM_ID, owner, states);
        }

        public IFsm<T> CreateFsm<T>(string id, T owner, IFsmState<T> states) where T : class
        {
            string creatingId = typeof(T).Name + "_" + id;
            if (_fsms.ContainsKey(creatingId))
            {
                XLog.Error("FSM with id " + creatingId + " already exists!");
                return null;
            }

            var fsm = new Fsm<T>(creatingId, owner, states);
            _fsms.Add(creatingId, fsm);
            return fsm;
        }


        public IFsm<T> GetFsm<T>() where T : class
        {
            throw new System.NotImplementedException();
        }

        public IFsm<T> GetFsm<T>(string id) where T : class
        {
            throw new System.NotImplementedException();
        }

        public void DestroyFsm<T>() where T : class
        {
            throw new System.NotImplementedException();
        }

        public void DestroyFsm<T>(string id) where T : class
        {
            throw new System.NotImplementedException();
        }
    }
}