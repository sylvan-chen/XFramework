using System;
using System.Collections.Generic;
using XFramework.Utils;

namespace XFramework
{
    public abstract class PoolBase
    {
        internal abstract void Update(float deltaTime, float unscaledDeltaTime);
        internal abstract void Destroy();

        public abstract void Squeeze();
        public abstract void DiscardAllUnused();
    }

    public sealed class Pool<T> : PoolBase where T : class
    {
        private readonly bool _allowMultiReference;        // 是否允许多引用
        private float _autoSqueezeInterval;                // 自动收缩间隔
        private int _capacity;                             // 容量
        private float _poolObjectSurvivalTime;             // 池对象可存活时间（秒）
        private float _poolObjectSurvivalDuration = 0f;    // 池对象已存活时间（秒）
        private readonly Dictionary<T, PoolObject> _poolObjectDict = new();
        private readonly List<PoolObject> _cachedDiscardingPoolObjects = new();
        private readonly List<PoolObject> _cachedDiscardablePoolObjects = new();

        public Pool(bool allowMultiReference, float autoSqueezeInterval, float poolObjectSurvivalTime, int capacity)
        {
            _allowMultiReference = allowMultiReference;
            _autoSqueezeInterval = autoSqueezeInterval;
            _poolObjectSurvivalTime = poolObjectSurvivalTime;
            _capacity = capacity;
        }

        public float AutoSqueezeInterval
        {
            get => _autoSqueezeInterval;
            set
            {
                if (value < 0f)
                {
                    throw new ArgumentException("Set AutoSqueezeInterval failed. AutoSqueezeInterval must be greater than or equal to 0.", nameof(value));
                }
                _autoSqueezeInterval = value;
            }
        }

        public float PoolObjectSurvivalTime
        {
            get => _poolObjectSurvivalTime;
            set
            {
                if (value < 0f)
                {
                    throw new ArgumentException("Set PoolObjectSurvivalTime failed. PoolObjectSurvivalTime must be greater than or equal to 0.", nameof(value));
                }
                _poolObjectSurvivalTime = value;
            }
        }

        public int Count
        {
            get => _poolObjectDict.Count;
        }

        public int Capacity
        {
            get => _capacity;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Set Capacity failed. Capacity must be greater than or equal to 0.", nameof(value));
                }
                _capacity = value;
                Squeeze();
            }
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            _poolObjectSurvivalDuration += unscaledDeltaTime;
            if (_poolObjectSurvivalDuration >= _autoSqueezeInterval)
            {
                _poolObjectSurvivalDuration = 0f;
                Squeeze();
            }
        }

        internal override void Destroy()
        {
            Log.Debug($"[XFramework] [Pool<{typeof(T).Name}>] Destroy pool.");
            foreach (PoolObject poolObject in _poolObjectDict.Values)
            {
                poolObject.Destroy();
            }
        }

        /// <summary>
        /// 注册一个对象到池中
        /// </summary>
        public void Register(T target, Action onSpawn = null, Action onUnspawn = null, Action onDestroy = null)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "TargetObject cannot be null.");
            }
            PoolObject poolObject = PoolObject.Create(target);
            poolObject.OnSpawn = onSpawn;
            poolObject.OnUnspawn = onUnspawn;
            poolObject.OnDestroy = onDestroy;
            poolObject.SpawnCount = 1;
            _poolObjectDict.Add(target, poolObject);
        }

        public T Spawn()
        {
            foreach (PoolObject poolObject in _poolObjectDict.Values)
            {
                if (_allowMultiReference || !poolObject.IsInUse)
                {
                    return poolObject.Spawn() as T;
                }
            }
            return null;
        }

        public void Unspawn(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }

            if (_poolObjectDict.TryGetValue(target, out PoolObject poolObject))
            {
                poolObject.Unspawn();
                if (Count > Capacity && poolObject.SpawnCount <= 0)
                {
                    Squeeze();
                }
            }
            else
            {
                Log.Error($"[XFramework] [Pool<{typeof(T).Name}>] Unspawn failed. Target not found in pool.");
            }
        }

        public void Lock(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }
            if (_poolObjectDict.TryGetValue(target, out PoolObject poolObject))
            {
                poolObject.Locked = true;
            }
            else
            {
                Log.Error($"[XFramework] [Pool<{typeof(T).Name}>] Lock failed. Target not found in pool.");
            }
        }

        public void Unlock(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }
            if (_poolObjectDict.TryGetValue(target, out PoolObject poolObject))
            {
                poolObject.Locked = false;
            }
            else
            {
                Log.Error($"[XFramework] [Pool<{typeof(T).Name}>] UnLock failed. Target not found in pool.");
            }
        }

        public bool Discard(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }
            // 通过 target 反向获取对应的池对象
            if (_poolObjectDict.TryGetValue(target, out PoolObject poolObject))
            {
                return Discard(poolObject);
            }
            return false;
        }


        internal bool Discard(PoolObject poolObject)
        {
            if (poolObject == null)
            {
                throw new ArgumentNullException(nameof(poolObject), "PoolObject cannot be null.");
            }

            if (poolObject.IsInUse || poolObject.Locked)
            {
                return false;
            }

            _poolObjectDict.Remove(poolObject.Target as T);
            poolObject.Destroy();
            return true;
        }

        /// <summary>
        /// 释放对象池中所有未使用的对象
        /// </summary>
        public override void DiscardAllUnused()
        {
            _autoSqueezeInterval = 0f;
            List<PoolObject> discardablePoolObjects = GetDiscardablePoolObjects();
            foreach (PoolObject poolObject in discardablePoolObjects)
            {
                Discard(poolObject);
            }
        }

        /// <summary>
        /// 尝试丢弃过期对象，使得池中对象数量不要超出容量限制
        /// </summary>
        public override void Squeeze()
        {
            SqueezeInternal(Count - Capacity, DefaultDiscardObjectFilter);
        }

        /// <summary>
        /// 尝试丢弃过期对象，使得池中对象数量不要超出容量限制
        /// </summary>
        /// <param name="discardablePoolObjectFilter">自定义丢弃对象过滤器</param>
        public void Squeeze(DiscardablePoolObjectFilter discardablePoolObjectFilter)
        {
            SqueezeInternal(Count - Capacity, discardablePoolObjectFilter);
        }

        private void SqueezeInternal(int discardCount, DiscardablePoolObjectFilter discardablePoolObjectFilter)
        {
            if (discardablePoolObjectFilter == null)
            {
                throw new ArgumentNullException(nameof(discardablePoolObjectFilter), "DiscardObjectFilter cannot be null.");
            }
            if (discardCount <= 0)
            {
                return;
            }

            List<PoolObject> discardingObjects = discardablePoolObjectFilter(GetDiscardablePoolObjects(), discardCount, _poolObjectSurvivalTime);
            if (discardingObjects == null || discardingObjects.Count <= 0)
            {
                return;
            }
            foreach (PoolObject obj in discardingObjects)
            {
                Discard(obj);
            }
        }

        private List<PoolObject> GetDiscardablePoolObjects()
        {
            _cachedDiscardablePoolObjects.Clear();
            foreach (PoolObject poolObject in _poolObjectDict.Values)
            {
                if (poolObject.IsInUse || poolObject.Locked)
                {
                    continue;
                }
                _cachedDiscardablePoolObjects.Add(poolObject);
            }
            return _cachedDiscardablePoolObjects;
        }

        private List<PoolObject> DefaultDiscardObjectFilter(List<PoolObject> candidatePoolObjects, int discardCount, float objectTTL)
        {
            _cachedDiscardingPoolObjects.Clear();
            for (int i = candidatePoolObjects.Count - 1; i >= 0; i--)
            {
                if (candidatePoolObjects[i].LastUseUtcTime.AddSeconds(objectTTL) < DateTime.UtcNow)
                {
                    _cachedDiscardingPoolObjects.Add(candidatePoolObjects[i]);
                    candidatePoolObjects.RemoveAt(i);
                }
            }
            discardCount -= _cachedDiscardingPoolObjects.Count;
            candidatePoolObjects.Sort((a, b) => a.LastUseUtcTime.CompareTo(b.LastUseUtcTime));
            foreach (PoolObject obj in candidatePoolObjects)
            {
                _cachedDiscardingPoolObjects.Add(obj);
                discardCount--;
                if (discardCount <= 0)
                {
                    break;
                }
            }
            return _cachedDiscardingPoolObjects;
        }
    }
}