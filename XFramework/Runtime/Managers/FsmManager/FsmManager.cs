using System;
using System.Collections.Generic;
using UnityEngine;

namespace XFramework
{
    /// <summary>
    /// 有限状态机管理器
    /// </summary>
    public sealed class FSMManager : Manager
    {
        private readonly Dictionary<int, FSM> _fsms = new();

        private const string DEFAULT_FSM_NAME = "default";

        private void Update()
        {
            foreach (FSM fsm in _fsms.Values)
            {
                fsm.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            foreach (FSM fsm in _fsms.Values)
            {
                fsm.Destroy();
            }
            _fsms.Clear();
        }

        public FSM<T> CreateFSM<T>(string name, T owner, params FSMState<T>[] states) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Create FSM failed. Name cannot be null.");
            }
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner), "Create FSM failed. Owner cannot be null.");
            }
            if (states == null || states.Length == 0)
            {
                throw new ArgumentNullException(nameof(states), "Create FSM failed. Initial states cannot be null or empty.");
            }
            int id = GetID(typeof(T), name);
            if (_fsms.ContainsKey(id))
            {
                throw new InvalidOperationException($"Create FSM failed. FSM with the same name ({name}) and same owner type ({typeof(T).Name}) already exists.");
            }

            var fsm = FSM<T>.Create(name, owner, states);
            _fsms.Add(id, fsm);
            return fsm;
        }

        public FSM<T> CreateFSM<T>(T owner, params FSMState<T>[] states) where T : class
        {
            return CreateFSM(DEFAULT_FSM_NAME, owner, states);
        }

        public FSM<T> CreateFSM<T>(T owner, List<FSMState<T>> states) where T : class
        {
            return CreateFSM(DEFAULT_FSM_NAME, owner, states.ToArray());
        }

        public FSM<T> CreateFSM<T>(string name, T owner, List<FSMState<T>> states) where T : class
        {
            return CreateFSM(name, owner, states.ToArray());
        }

        public FSM<T> GetFSM<T>() where T : class
        {
            return GetFSM<T>(DEFAULT_FSM_NAME);
        }

        public FSM<T> GetFSM<T>(string name) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Get FSM failed. Name cannot be null.");
            }
            int id = GetID(typeof(T), name);
            if (_fsms.TryGetValue(id, out FSM fsm))
            {
                return fsm as FSM<T>;
            }
            return null;
        }

        public void DestroyFSM<T>() where T : class
        {
            DestroyFSM<T>(DEFAULT_FSM_NAME);
        }

        public void DestroyFSM<T>(string name) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Destroy FSM failed. Name cannot be null.");
            }
            int id = GetID(typeof(T), name);
            if (_fsms.TryGetValue(id, out FSM fsm))
            {
                fsm.Destroy();
                _fsms.Remove(id);
            }
        }

        private int GetID(Type type, string name)
        {
            return (type.Name + name).GetHashCode();
        }
    }
}