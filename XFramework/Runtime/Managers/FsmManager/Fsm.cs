using System;
using System.Collections.Generic;

namespace XFramework
{
    public abstract class Fsm
    {
        internal abstract void Update(float deltaTime, float unscaleDeltaTime);
        internal abstract void Destroy();
    }

    /// <summary>
    /// 有限状态机
    /// </summary>
    /// <typeparam name="T">有限状态机的所有者类型</typeparam>
    public sealed class Fsm<T> : Fsm where T : class
    {
        private string _name;
        private T _owner;
        private readonly Dictionary<Type, FsmState<T>> _stateDict;
        private FsmState<T> _currentState = null;
        private float _currentStateTime = 0f;
        private bool _isDestroyed = false;

        public Fsm(string name, T owner, params FsmState<T>[] states)
        {
            if (states == null || states.Length == 0)
            {
                throw new ArgumentException("Construct FSM failed. Initial states cannot be null or empty.", nameof(states));
            }
            _name = name ?? throw new ArgumentNullException(nameof(name), $"Construct FSM failed. Name cannot be null.");
            _owner = owner ?? throw new ArgumentNullException(nameof(owner), $"Construct FSM failed. Owner cannot be null.");
            _stateDict = new Dictionary<Type, FsmState<T>>();
            foreach (FsmState<T> state in states)
            {
                if (state == null)
                {
                    throw new ArgumentNullException(nameof(states), $"Construct FSM failed. The state in initial states cannot be null.");
                }
                if (_stateDict.ContainsKey(state.GetType()))
                {
                    throw new ArgumentException($"Construct FSM failed. Duplicate state in initial states is not allowed, type {state.GetType().FullName} is already found.", nameof(states));
                }
                _stateDict.Add(state.GetType(), state);
                state.OnInit(this);
            }
        }

        public Fsm(string name, T owner, List<FsmState<T>> states) : this(name, owner, states.ToArray())
        {
        }

        public Fsm(T owner, params FsmState<T>[] states) : this(string.Empty, owner, states)
        {
        }

        public Fsm(T owner, List<FsmState<T>> states) : this(string.Empty, owner, states.ToArray())
        {
        }

        /// <summary>
        /// 状态机名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            private set { _name = value ?? string.Empty; }
        }

        /// <summary>
        /// 状态机所有者
        /// </summary>
        public T Owner
        {
            get { return _owner; }
            private set { _owner = value; }
        }

        /// <summary>
        /// 状态机的状态数量
        /// </summary>
        public int StateCount
        {
            get { return _stateDict.Count; }
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public FsmState<T> CurrentState
        {
            get { return _currentState; }
        }

        /// <summary>
        /// 当前状态已持续时间
        /// </summary>
        /// <remarks>
        /// 单位：秒，切换时重置为 0。
        /// </remarks>
        public float CurrentStateTime
        {
            get { return _currentStateTime; }
        }

        public bool IsDestroyed
        {
            get { return _isDestroyed; }
        }

        internal override void Update(float deltaTime, float unscaledeltaTime)
        {
            if (!CheckStarted() || _isDestroyed)
            {
                return;
            }
            _currentStateTime += unscaledeltaTime;
            _currentState.OnUpdate(this, deltaTime, unscaledeltaTime);
        }

        internal override void Destroy()
        {
            _currentState?.OnExit(this);
            foreach (FsmState<T> state in _stateDict.Values)
            {
                state.OnFsmDestroy(this);
            }
            _isDestroyed = true;
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        /// <typeparam name="TState">启动时的状态类型</typeparam>
        public void Start<TState>() where TState : FsmState<T>
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Start FSM {Name} failed. It has already been destroyed.");
            }
            if (CheckStarted())
            {
                throw new InvalidOperationException($"Start FSM {Name} failed. It has already been started, don't start it again.");
            }

            if (_stateDict.TryGetValue(typeof(TState), out FsmState<T> state))
            {
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"Launch FSM {Name} failed. State of type {typeof(TState).FullName} not found.", nameof(TState));
            }
        }

        public void Start(Type startStateType)
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Start FSM {Name} failed. It has already been destroyed.");
            }
            if (CheckStarted())
            {
                throw new InvalidOperationException($"Start FSM {Name} failed. It has already been started, don't start it again.");
            }
            if (startStateType == null)
            {
                throw new ArgumentNullException(nameof(startStateType), $"Start FSM {Name} failed. Start state type cannot be null.");
            }
            if (!typeof(FsmState<T>).IsAssignableFrom(startStateType))
            {
                throw new ArgumentException($"Start FSM {Name} failed. Start state type {startStateType.FullName} must be a subclass of {typeof(FsmState<T>).Name}.", nameof(startStateType));
            }

            if (_stateDict.TryGetValue(startStateType, out FsmState<T> state))
            {
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"Launch FSM {Name} failed. State of type {startStateType.FullName} not found.", nameof(startStateType));
            }
        }

        public TState GetState<TState>() where TState : FsmState<T>
        {
            if (_stateDict.TryGetValue(typeof(TState), out FsmState<T> state))
            {
                return (TState)state;
            }
            return null;
        }

        public bool HasState<TState>() where TState : FsmState<T>
        {
            return _stateDict.ContainsKey(typeof(TState));
        }

        public void ChangeState<TState>() where TState : FsmState<T>
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Change state of FSM {Name} failed. The FSM has already been destroyed.");
            }
            if (!CheckStarted())
            {
                throw new InvalidOperationException($"Change state of FSM {Name} failed. The FSM didn't start yet.");
            }
            if (_stateDict.TryGetValue(typeof(TState), out FsmState<T> state))
            {
                _currentState.OnExit(this);
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"Change state of FSM {Name} failed. State of type {typeof(TState).FullName} not found.", nameof(TState));
            }
        }

        public FsmState<T>[] GetAllStates()
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Get all states of FSM {Name} failed. The FSM has already been destroyed.");
            }
            if (_stateDict.Count == 0)
            {
                return new FsmState<T>[0];
            }
            var result = new FsmState<T>[_stateDict.Count];
            _stateDict.Values.CopyTo(result, 0);
            return result;
        }

        private bool CheckStarted()
        {
            return _currentState != null;
        }
    }
}