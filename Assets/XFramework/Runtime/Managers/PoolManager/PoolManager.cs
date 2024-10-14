using System;
using System.Collections.Generic;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    public sealed class PoolManager : ManagerBase
    {
        public const int DefaultCapacity = int.MaxValue;
        public const float DefaultObjectSurvivalTime = float.MaxValue;

        private readonly Dictionary<Type, PoolBase> _poolDict = new();

        public int Count
        {
            get => _poolDict.Count;
        }

        private void Update()
        {
            foreach (PoolBase pool in _poolDict.Values)
            {
                pool.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Log.Debug($"[XFramework] [PoolManager] Destroy PoolManager.");
            foreach (PoolBase pool in _poolDict.Values)
            {
                pool.Destroy();
            }
            _poolDict.Clear();
        }

        private Pool<T> GetPool<T>() where T : class
        {
            return GetPool(typeof(T)) as Pool<T>;
        }

        private PoolBase GetPool(Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType), "GetPool failed. ObjectType cannot be null.");
            }
            if (_poolDict.TryGetValue(objectType, out PoolBase pool))
            {
                return pool;
            }
            return null;
        }

        public Pool<T> CreatePool<T>() where T : class
        {
            return CreatePoolInternal<T>(false, DefaultObjectSurvivalTime, DefaultObjectSurvivalTime, DefaultCapacity);
        }

        public PoolBase CreatePool(Type objectType)
        {
            return CreatePoolInternal(objectType, false, DefaultObjectSurvivalTime, DefaultObjectSurvivalTime, DefaultCapacity);
        }

        public Pool<T> CreatePool<T>(float objectSurvivalTime) where T : class
        {
            return CreatePoolInternal<T>(false, objectSurvivalTime, objectSurvivalTime, DefaultCapacity);
        }

        public PoolBase CreatePool(Type objectType, float objectSurvivalTime)
        {
            return CreatePoolInternal(objectType, false, objectSurvivalTime, objectSurvivalTime, DefaultCapacity);
        }

        public Pool<T> CreatePool<T>(int capacity) where T : class
        {
            return CreatePoolInternal<T>(false, DefaultObjectSurvivalTime, DefaultObjectSurvivalTime, capacity);
        }

        public PoolBase CreatePool(Type objectType, int capacity)
        {
            return CreatePoolInternal(objectType, false, DefaultObjectSurvivalTime, DefaultObjectSurvivalTime, capacity);
        }

        public Pool<T> CreatePool<T>(float objectSurvivalTime, int capacity) where T : class
        {
            return CreatePoolInternal<T>(false, objectSurvivalTime, objectSurvivalTime, capacity);
        }

        public PoolBase CreatePool(Type objectType, float objectSurvivalTime, int capacity)
        {
            return CreatePoolInternal(objectType, false, objectSurvivalTime, objectSurvivalTime, capacity);
        }

        public Pool<T> CreatePool<T>(float autoSqueezeInterval, float poolObjectSurvivalTime) where T : class
        {
            return CreatePoolInternal<T>(false, autoSqueezeInterval, poolObjectSurvivalTime, DefaultCapacity);
        }

        public PoolBase CreatePool(Type objectType, float autoSqueezeInterval, float poolObjectSurvivalTime)
        {
            return CreatePoolInternal(objectType, false, autoSqueezeInterval, poolObjectSurvivalTime, DefaultCapacity);
        }

        public Pool<T> CreatePool<T>(float autoSqueezeInterval, float poolObjectSurvivalTime, int capacity) where T : class
        {
            return CreatePoolInternal<T>(false, autoSqueezeInterval, poolObjectSurvivalTime, capacity);
        }

        public PoolBase CreatePool(Type objectType, float autoSqueezeInterval, float poolObjectSurvivalTime, int capacity)
        {
            return CreatePoolInternal(objectType, false, autoSqueezeInterval, poolObjectSurvivalTime, capacity);
        }

        public Pool<T> CreateMultiReferencePool<T>() where T : class
        {
            return CreatePoolInternal<T>(true, DefaultObjectSurvivalTime, DefaultObjectSurvivalTime, DefaultCapacity);
        }

        public PoolBase CreateMultiReferencePool(Type objectType)
        {
            return CreatePoolInternal(objectType, true, DefaultObjectSurvivalTime, DefaultObjectSurvivalTime, DefaultCapacity);
        }

        public Pool<T> CreateMultiReferencePool<T>(float objectSurvivalTime) where T : class
        {
            return CreatePoolInternal<T>(true, objectSurvivalTime, objectSurvivalTime, DefaultCapacity);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, float objectSurvivalTime)
        {
            return CreatePoolInternal(objectType, true, objectSurvivalTime, objectSurvivalTime, DefaultCapacity);
        }

        public Pool<T> CreateMultiReferencePool<T>(int capacity) where T : class
        {
            return CreatePoolInternal<T>(true, DefaultObjectSurvivalTime, DefaultObjectSurvivalTime, capacity);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, int capacity)
        {
            return CreatePoolInternal(objectType, true, DefaultObjectSurvivalTime, DefaultObjectSurvivalTime, capacity);
        }

        public Pool<T> CreateMultiReferencePool<T>(float objectSurvivalTime, int capacity) where T : class
        {
            return CreatePoolInternal<T>(true, objectSurvivalTime, objectSurvivalTime, capacity);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, float objectSurvivalTime, int capacity)
        {
            return CreatePoolInternal(objectType, true, objectSurvivalTime, objectSurvivalTime, capacity);
        }

        public Pool<T> CreateMultiReferencePool<T>(float autoSqueezeInterval, float poolObjectSurvivalTime) where T : class
        {
            return CreatePoolInternal<T>(true, autoSqueezeInterval, poolObjectSurvivalTime, DefaultCapacity);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, float autoSqueezeInterval, float poolObjectSurvivalTime)
        {
            return CreatePoolInternal(objectType, true, autoSqueezeInterval, poolObjectSurvivalTime, DefaultCapacity);
        }

        public Pool<T> CreateMultiReferencePool<T>(float autoSqueezeInterval, float poolObjectSurvivalTime, int capacity) where T : class
        {
            return CreatePoolInternal<T>(true, autoSqueezeInterval, poolObjectSurvivalTime, capacity);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, float autoSqueezeInterval, float poolObjectSurvivalTime, int capacity)
        {
            return CreatePoolInternal(objectType, true, autoSqueezeInterval, poolObjectSurvivalTime, capacity);
        }

        public bool DestroyPool<T>() where T : class
        {
            return DestroyPool(typeof(T));
        }

        public bool DestroyPool(Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType), "DestroyPool failed. Type cannot be null.");
            }
            if (_poolDict.TryGetValue(objectType, out PoolBase pool))
            {
                pool.Destroy();
                _poolDict.Remove(objectType);
                return true;
            }
            return false;
        }

        public void Squeeze()
        {
            List<PoolBase> pools = new(_poolDict.Values);
            foreach (PoolBase pool in pools)
            {
                pool.Squeeze();
            }
        }

        public void DiscardAllUnused()
        {
            List<PoolBase> pools = new(_poolDict.Values);
            foreach (PoolBase pool in pools)
            {
                pool.DiscardAllUnused();
            }
        }

        private PoolBase CreatePoolInternal(Type objectType, bool allowMultiReference, float autoSqueezeInterval, float poolObjectSurvivalTime, int capacity)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType), "CreatePool failed. Type cannot be null.");
            }
            if (!objectType.IsClass || objectType.IsAbstract)
            {
                throw new ArgumentException($"CreatePool failed. Type {objectType.Name} is not a valid class type.");
            }
            if (_poolDict.ContainsKey(objectType))
            {
                throw new InvalidOperationException($"CreatePool failed. Pool of type {objectType.Name} already exists.");
            }
            Type poolType = typeof(Pool<>).MakeGenericType(objectType);
            PoolBase pool = Activator.CreateInstance(poolType, allowMultiReference, autoSqueezeInterval, poolObjectSurvivalTime, capacity) as PoolBase;
            _poolDict.Add(objectType, pool);
            return pool;
        }

        private Pool<T> CreatePoolInternal<T>(bool allowMultiReference, float autoSqueezeInterval, float poolObjectSurvivalTime, int capacity) where T : class
        {
            if (_poolDict.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Create pool failed, pool of type {typeof(T)} already exists.");
            }

            Pool<T> pool = new(allowMultiReference, autoSqueezeInterval, poolObjectSurvivalTime, capacity);
            _poolDict.Add(typeof(T), pool);
            return pool;
        }
    }
}