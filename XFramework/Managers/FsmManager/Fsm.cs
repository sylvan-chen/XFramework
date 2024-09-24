using System;
using System.Collections.Generic;

namespace XFramework
{
    public sealed class Fsm<T> : IFsm<T> where T : class
    {
        private string _name;
        private T _owner;
        private readonly Dictionary<Type, IFsmState<T>> _stateDict;
        private IFsmState<T> _currentState;
        private float _currentStateTime;

        public Fsm(string name, T owner, params IFsmState<T>[] states)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("[XFramework] [Fsm] ID cannot be null or empty.", "id");
            }
            if (states == null || states.Length < 1)
            {
                throw new ArgumentException("[XFramework] [Fsm] At least one state is required.", "states");
            }

            _name = name;
            _owner = owner ?? throw new ArgumentException("[XFramework] [Fsm] Target cannot be null.", "owner");
            _stateDict = new Dictionary<Type, IFsmState<T>>();
            foreach (IFsmState<T> state in states)
            {
                if (state == null)
                {
                    throw new ArgumentException("[XFramework] [Fsm] State cannot be null.", "states");
                }
                if (_stateDict.ContainsKey(state.GetType()))
                {
                    throw new ArgumentException($"[XFramework] [Fsm] Duplicate state type {state.GetType().FullName} found in FSM({Name}).", "states");
                }
                _stateDict.Add(state.GetType(), state);
                state.OnInit(this);
            }
            _currentState = null;
            _currentStateTime = 0;
        }

        public Fsm(string name, T owner, List<IFsmState<T>> states) : this(name, owner, states.ToArray())
        {
        }

        public Fsm(T owner, params IFsmState<T>[] states) : this(string.Empty, owner, states)
        {
        }

        public Fsm(T owner, List<IFsmState<T>> states) : this(string.Empty, owner, states.ToArray())
        {
        }

        public string Name
        {
            get { return _name; }
            private set { _name = value ?? string.Empty; }
        }

        public T Owner
        {
            get { return _owner; }
            private set { _owner = value; }
        }

        public int StateCount
        {
            get { return _stateDict.Count; }
        }

        public IFsmState<T> CurrentState
        {
            get { return _currentState; }
        }

        public float CurrentStateTime
        {
            get { return _currentStateTime; }
        }

        public void Start<TState>() where TState : class, IFsmState<T>
        {
            if (CheckStarted())
            {
                throw new InvalidOperationException("[XFramework] [Fsm] FSM has already been started, don't start it again.");
            }
            if (_stateDict.TryGetValue(typeof(TState), out IFsmState<T> state))
            {
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"[XFramework] [Fsm] State {typeof(TState).FullName} not found in FSM({Name}).", "TState");
            }
        }

        public TState GetState<TState>() where TState : class, IFsmState<T>
        {
            if (_stateDict.TryGetValue(typeof(TState), out IFsmState<T> state))
            {
                return (TState)state;
            }
            return null;
        }

        public bool HasState<TState>() where TState : class, IFsmState<T>
        {
            return _stateDict.ContainsKey(typeof(TState));
        }

        public void ChangeState<TState>() where TState : class, IFsmState<T>
        {
            if (!CheckStarted())
            {
                throw new InvalidOperationException("[XFramework] [Fsm] FSM didn't start yet, cannot change state.");
            }
            if (_stateDict.TryGetValue(typeof(TState), out IFsmState<T> state))
            {
                _currentState.OnExit(this);
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"[XFramework] [Fsm] State {typeof(TState).FullName} not found in FSM({Name}).", "TState");
            }
        }

        public IFsmState<T>[] GetAllStates()
        {
            var result = new IFsmState<T>[_stateDict.Count];
            _stateDict.Values.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// 更新状态机
        /// </summary>
        /// <param name="logicSeconds">逻辑时间</param>
        /// <param name="realSeconds">真实时间</param>
        public void Update(float logicSeconds, float realSeconds)
        {
            if (!CheckStarted())
            {
                return;
            }
            _currentStateTime += realSeconds;
            _currentState.OnUpdate(this, logicSeconds, realSeconds);
        }

        /// <summary>
        /// 销毁状态机
        /// </summary>
        public void Destroy()
        {
            _currentState?.OnExit(this);
            foreach (IFsmState<T> state in _stateDict.Values)
            {
                state.OnDestroy(this);
            }
        }

        private bool CheckStarted()
        {
            return _currentState != null;
        }
    }
}