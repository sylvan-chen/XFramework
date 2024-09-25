using System;
using System.Collections.Generic;
using UnityEngine;

namespace XFramework
{
    public class FsmManager : MonoBehaviour, IFsmManager
    {
        private readonly Dictionary<int, IFsm> _fsms = new();

        private const string DEFAULT_FSM_NAME = "default";

        private void Awake()
        {
            GlobalManager.Register<IFsmManager>(this);
        }

        private void Update()
        {
            foreach (IFsm fsm in _fsms.Values)
            {
                fsm.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            foreach (IFsm fsm in _fsms.Values)
            {
                fsm.Destroy();
            }
            _fsms.Clear();
        }

        public IFsm<T> CreateFsm<T>(T owner, IFsmState<T>[] states) where T : class
        {
            return CreateFsm(DEFAULT_FSM_NAME, owner, states);
        }

        public IFsm<T> CreateFsm<T>(string name, T owner, IFsmState<T>[] states) where T : class
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
            int id = GetId(typeof(T), name);
            if (_fsms.ContainsKey(id))
            {
                throw new InvalidOperationException($"Create FSM failed. FSM with the same name ({name}) and same owner type ({typeof(T).Name}) already exists.");
            }

            var fsm = new Fsm<T>(name, owner, states);
            _fsms.Add(id, fsm);
            return fsm;
        }

        public IFsm<T> CreateFsm<T>(T owner, List<IFsmState<T>> states) where T : class
        {
            return CreateFsm(DEFAULT_FSM_NAME, owner, states.ToArray());
        }

        public IFsm<T> CreateFsm<T>(string name, T owner, List<IFsmState<T>> states) where T : class
        {
            return CreateFsm(name, owner, states.ToArray());
        }

        public IFsm<T> GetFsm<T>() where T : class
        {
            return GetFsm<T>(DEFAULT_FSM_NAME);
        }

        public IFsm<T> GetFsm<T>(string name) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Get FSM failed. Name cannot be null.");
            }
            int id = GetId(typeof(T), name);
            if (_fsms.TryGetValue(id, out IFsm fsm))
            {
                return fsm as IFsm<T>;
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
            int id = GetId(typeof(T), name);
            if (_fsms.TryGetValue(id, out IFsm fsm))
            {
                fsm.Destroy();
                _fsms.Remove(id);
            }
        }

        private int GetId(Type type, string name)
        {
            return (type.Name + name).GetHashCode();
        }
    }
}