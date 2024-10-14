using System;
using System.Collections.Generic;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 有限状态机管理器
    /// </summary>
    public sealed class FSMManager : ManagerBase
    {
        private readonly Dictionary<int, FSMBase> _fsms = new();

        private const string DefaultFSMName = "default";

        private void Update()
        {
            foreach (FSMBase fsm in _fsms.Values)
            {
                fsm.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (FSMBase fsm in _fsms.Values)
            {
                fsm.Destroy();
            }
            _fsms.Clear();
        }

        public FSM<T> CreateFSM<T>(string name, T owner, params StateBase<T>[] states) where T : class
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

        public FSM<T> CreateFSM<T>(T owner, params StateBase<T>[] states) where T : class
        {
            return CreateFSM(DefaultFSMName, owner, states);
        }

        public FSM<T> CreateFSM<T>(T owner, List<StateBase<T>> states) where T : class
        {
            return CreateFSM(DefaultFSMName, owner, states.ToArray());
        }

        public FSM<T> CreateFSM<T>(string name, T owner, List<StateBase<T>> states) where T : class
        {
            return CreateFSM(name, owner, states.ToArray());
        }

        public FSM<T> GetFSM<T>() where T : class
        {
            return GetFSM<T>(DefaultFSMName);
        }

        public FSM<T> GetFSM<T>(string name) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Get FSM failed. Name cannot be null.");
            }
            int id = GetID(typeof(T), name);
            if (_fsms.TryGetValue(id, out FSMBase fsm))
            {
                return fsm as FSM<T>;
            }
            return null;
        }

        public void DestroyFSM<T>() where T : class
        {
            DestroyFSM<T>(DefaultFSMName);
        }

        public void DestroyFSM<T>(string name) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Destroy FSM failed. Name cannot be null.");
            }
            int id = GetID(typeof(T), name);
            if (_fsms.TryGetValue(id, out FSMBase fsm))
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