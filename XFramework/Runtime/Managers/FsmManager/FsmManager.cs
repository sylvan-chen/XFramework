using System;
using System.Collections.Generic;
using UnityEngine;

namespace XFramework
{
    /// <summary>
    /// 有限状态机管理器
    /// </summary>
    public sealed class FsmManager : Manager
    {
        private readonly Dictionary<int, Fsm> _fsms = new();

        private const string DEFAULT_FSM_NAME = "default";

        private void Update()
        {
            foreach (Fsm fsm in _fsms.Values)
            {
                fsm.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            foreach (Fsm fsm in _fsms.Values)
            {
                fsm.Destroy();
            }
            _fsms.Clear();
        }

        public Fsm<T> CreateFsm<T>(string name, T owner, params FsmState<T>[] states) where T : class
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

            var fsm = Fsm<T>.Spawn(name, owner, states);
            _fsms.Add(id, fsm);
            return fsm;
        }

        public Fsm<T> CreateFsm<T>(T owner, params FsmState<T>[] states) where T : class
        {
            return CreateFsm(DEFAULT_FSM_NAME, owner, states);
        }

        public Fsm<T> CreateFsm<T>(T owner, List<FsmState<T>> states) where T : class
        {
            return CreateFsm(DEFAULT_FSM_NAME, owner, states.ToArray());
        }

        public Fsm<T> CreateFsm<T>(string name, T owner, List<FsmState<T>> states) where T : class
        {
            return CreateFsm(name, owner, states.ToArray());
        }

        public Fsm<T> GetFsm<T>() where T : class
        {
            return GetFsm<T>(DEFAULT_FSM_NAME);
        }

        public Fsm<T> GetFsm<T>(string name) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Get FSM failed. Name cannot be null.");
            }
            int id = GetID(typeof(T), name);
            if (_fsms.TryGetValue(id, out Fsm fsm))
            {
                return fsm as Fsm<T>;
            }
            return null;
        }

        public void DestroyFsm<T>() where T : class
        {
            DestroyFsm<T>(DEFAULT_FSM_NAME);
        }

        public void DestroyFsm<T>(string name) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Destroy FSM failed. Name cannot be null.");
            }
            int id = GetID(typeof(T), name);
            if (_fsms.TryGetValue(id, out Fsm fsm))
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