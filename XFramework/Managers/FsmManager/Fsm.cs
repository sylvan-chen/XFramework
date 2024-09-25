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
        private bool _isDestroyed;

        public Fsm(string name, T owner, params IFsmState<T>[] states)
        {
            if (states == null || states.Length < 1)
            {
                throw new ArgumentException("Construct FSM failed. At least one state is required in initial states.", nameof(states));
            }
            _name = name ?? throw new ArgumentNullException(nameof(name), $"Construct FSM failed. Name cannot be null.");
            _owner = owner ?? throw new ArgumentNullException(nameof(owner), $"Construct FSM failed. Owner cannot be null.");
            _stateDict = new Dictionary<Type, IFsmState<T>>();
            foreach (IFsmState<T> state in states)
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
            _currentState = null;
            _currentStateTime = 0;
            _isDestroyed = false;
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

        public bool IsDestroyed
        {
            get { return _isDestroyed; }
        }

        public void Start<TState>() where TState : class, IFsmState<T>
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Start FSM {Name} failed. It has already been destroyed.");
            }
            if (CheckStarted())
            {
                throw new InvalidOperationException($"Start FSM {Name} failed. It has already been started, don't start it again.");
            }
            if (_stateDict.TryGetValue(typeof(TState), out IFsmState<T> state))
            {
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"Start FSM {Name} failed. State of type {typeof(TState).FullName} not found.", nameof(TState));
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
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Change state of FSM {Name} failed. The FSM has already been destroyed.");
            }
            if (!CheckStarted())
            {
                throw new InvalidOperationException($"Change state of FSM {Name} failed. The FSM didn't start yet.");
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
                throw new ArgumentException($"Change state of FSM {Name} failed. State of type {typeof(TState).FullName} not found.", nameof(TState));
            }
        }

        public IFsmState<T>[] GetAllStates()
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Get all states of FSM {Name} failed. The FSM has already been destroyed.");
            }
            if (_stateDict.Count == 0)
            {
                return new IFsmState<T>[0];
            }
            var result = new IFsmState<T>[_stateDict.Count];
            _stateDict.Values.CopyTo(result, 0);
            return result;
        }

        public void Update(float logicSeconds, float realSeconds)
        {
            if (!CheckStarted() || _isDestroyed)
            {
                return;
            }
            _currentStateTime += realSeconds;
            _currentState.OnUpdate(this, logicSeconds, realSeconds);
        }

        public void Destroy()
        {
            _currentState?.OnExit(this);
            foreach (IFsmState<T> state in _stateDict.Values)
            {
                state.OnDestroy(this);
            }
            _isDestroyed = true;
        }

        private bool CheckStarted()
        {
            return _currentState != null;
        }
    }
}