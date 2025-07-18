using System;
using System.Collections.Generic;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Pool Manager")]
    public sealed class PoolManager : XFrameworkComponent
    {
        public const int MaxCapacity = int.MaxValue;
        public const float InfinityObjectExpiredTime = float.MaxValue;
        public const float InfinityAutoClearInterval = float.MaxValue;

        private readonly Dictionary<Type, PoolBase> _poolDict = new();

        public int Count
        {
            get => _poolDict.Count;
        }

        internal override int Priority
        {
            get => Consts.XFrameworkConsts.ComponentPriority.PoolManager;
        }

        private void Update()
        {
            foreach (PoolBase pool in _poolDict.Values)
            {
                pool.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        internal override void Clear()
        {
            base.Clear();

            foreach (PoolBase pool in _poolDict.Values)
            {
                pool.Destroy();
            }
            _poolDict.Clear();
        }

        public PoolBase[] GetAllPools()
        {
            PoolBase[] pools = new PoolBase[_poolDict.Count];
            int index = 0;
            foreach (PoolBase pool in _poolDict.Values)
            {
                pools[index++] = pool;
            }
            return pools;
        }

        public Pool<T> GetPool<T>() where T : class
        {
            return GetPool(typeof(T)) as Pool<T>;
        }

        public PoolBase GetPool(Type objectType)
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
                pool.DicardAllUnused();
            }
        }

        public Pool<T> CreatePool<T>() where T : class
        {
            return CreatePoolInternal<T>(false, MaxCapacity, InfinityObjectExpiredTime, InfinityAutoClearInterval);
        }

        public Pool<T> CreatePool<T>(int capacity) where T : class
        {
            return CreatePoolInternal<T>(false, capacity, InfinityObjectExpiredTime, InfinityAutoClearInterval);
        }

        public Pool<T> CreatePool<T>(float objectExpiredTime, float autoClearInterval) where T : class
        {
            return CreatePoolInternal<T>(false, MaxCapacity, objectExpiredTime, autoClearInterval);
        }

        public Pool<T> CreatePool<T>(int capacity, float objectExpiredTime, float autoClearInterval) where T : class
        {
            return CreatePoolInternal<T>(false, capacity, objectExpiredTime, autoClearInterval);
        }

        public PoolBase CreatePool(Type objectType)
        {
            return CreatePoolInternal(objectType, false, MaxCapacity, InfinityObjectExpiredTime, InfinityAutoClearInterval);
        }

        public PoolBase CreatePool(Type objectType, int capacity)
        {
            return CreatePoolInternal(objectType, false, capacity, InfinityObjectExpiredTime, InfinityAutoClearInterval);
        }

        public PoolBase CreatePool(Type objectType, float objectExpiredTime, float autoClearInterval)
        {
            return CreatePoolInternal(objectType, false, MaxCapacity, objectExpiredTime, autoClearInterval);
        }

        public PoolBase CreatePool(Type objectType, int capacity, float objectExpiredTime, float autoClearInterval)
        {
            return CreatePoolInternal(objectType, false, capacity, objectExpiredTime, autoClearInterval);
        }

        public Pool<T> CreateMultiReferencePool<T>() where T : class
        {
            return CreatePoolInternal<T>(true, MaxCapacity, InfinityObjectExpiredTime, InfinityAutoClearInterval);
        }

        public Pool<T> CreateMultiReferencePool<T>(int capacity) where T : class
        {
            return CreatePoolInternal<T>(true, capacity, InfinityObjectExpiredTime, InfinityAutoClearInterval);
        }

        public Pool<T> CreateMultiReferencePool<T>(float objectExpiredTime, float autoClearInterval) where T : class
        {
            return CreatePoolInternal<T>(true, MaxCapacity, objectExpiredTime, autoClearInterval);
        }

        public Pool<T> CreateMultiReferencePool<T>(int capacity, float objectExpiredTime, float autoClearInterval) where T : class
        {
            return CreatePoolInternal<T>(true, capacity, objectExpiredTime, autoClearInterval);
        }

        public PoolBase CreateMultiReferencePool(Type objectType)
        {
            return CreatePoolInternal(objectType, true, MaxCapacity, InfinityObjectExpiredTime, InfinityAutoClearInterval);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, int capacity)
        {
            return CreatePoolInternal(objectType, true, capacity, InfinityObjectExpiredTime, InfinityAutoClearInterval);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, float objectExpiredTime, float autoClearInterval)
        {
            return CreatePoolInternal(objectType, true, MaxCapacity, objectExpiredTime, autoClearInterval);
        }

        public PoolBase CreateMultiReferencePool(Type objectType, int capacity, float objectExpiredTime, float autoClearInterval)
        {
            return CreatePoolInternal(objectType, true, capacity, objectExpiredTime, autoClearInterval);
        }

        private PoolBase CreatePoolInternal(Type objectType, bool allowMultiReference, int capacity, float objectExpiredTime, float autoClearInterval)
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
            PoolBase pool = Activator.CreateInstance(poolType, allowMultiReference, capacity, objectExpiredTime, autoClearInterval) as PoolBase;
            _poolDict.Add(objectType, pool);
            return pool;
        }

        private Pool<T> CreatePoolInternal<T>(bool allowMultiReference, int capacity, float objectExpiredTime, float autoClearInterval) where T : class
        {
            if (_poolDict.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Create pool failed, pool of type {typeof(T)} already exists.");
            }

            Pool<T> pool = new(allowMultiReference, capacity, objectExpiredTime, autoClearInterval);
            _poolDict.Add(typeof(T), pool);
            return pool;
        }
    }
}