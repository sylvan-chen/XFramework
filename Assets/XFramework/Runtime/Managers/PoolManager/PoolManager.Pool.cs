using System;
using System.Collections.Generic;

namespace XFramework
{
    public sealed partial class PoolManager
    {
        private class Pool<T> where T : PoolObjectBase, new()
        {
            private readonly Dictionary<string, List<PoolObjectBase>> _objectsDict = new();
            private readonly Dictionary<object, PoolObjectBase> _objectMap = new();
            private readonly bool _allowMultiReference;
            private float _autoSqueezeInterval;
            private float _objectTTL;
            private int _capacity;

            private float _aliveTime = 0f;
            private readonly List<T> _cachedDiscardingObjects = new();
            private readonly List<T> _cachedDiscardableObjects = new();

            public Pool(bool allowMultiReference, float autoSqueezeInterval, float objectTTL, int capacity)
            {
                _allowMultiReference = allowMultiReference;
                _autoSqueezeInterval = autoSqueezeInterval;
                _objectTTL = objectTTL;
                _capacity = capacity;
            }

            public Type ObjectType
            {
                get => typeof(T);
            }

            public int ObjectCount
            {
                get => _objectMap.Count;
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

            public bool Discard(T poolObject)
            {
                if (poolObject == null)
                {
                    throw new ArgumentNullException(nameof(poolObject), "PoolObject cannot be null.");
                }

                if (poolObject.IsInUse || poolObject.Locked)
                {
                    return false;
                }

                _objectMap.Remove(poolObject.Target);
                _objectsDict.Remove(poolObject.Name);
                poolObject.Destroy();
                return true;
            }

            public bool Discard(object target)
            {
                if (target == null)
                {
                    throw new ArgumentNullException(nameof(target), "Target cannot be null.");
                }
                // 通过 target 反向获取对应的池对象
                if (_objectMap.TryGetValue(target, out PoolObjectBase poolObject))
                {
                    return Discard(poolObject as T);
                }
                return false;
            }

            /// <summary>
            /// 释放对象池中所有未使用的对象
            /// </summary>
            public void DiscardAllUnused()
            {
                _autoSqueezeInterval = 0f;
                List<T> discardableObjects = GetDiscardableObjects();
                foreach (T obj in discardableObjects)
                {
                    Discard(obj);
                }
            }

            /// <summary>
            /// 尝试丢弃过期对象，使得池中对象数量不要超出容量限制
            /// </summary>
            public void Squeeze()
            {
                SqueezeInternal(ObjectCount - Capacity, DefaultDiscardObjectFilter);
            }

            /// <summary>
            /// 尝试丢弃过期对象，使得池中对象数量不要超出容量限制
            /// </summary>
            /// <param name="discardObjectFilter">丢弃对象过滤器</param>
            public void Squeeze(DiscardObjectFilter<T> discardObjectFilter)
            {
                SqueezeInternal(ObjectCount - Capacity, discardObjectFilter);
            }

            private void SqueezeInternal(int discardCount, DiscardObjectFilter<T> discardObjectFilter)
            {
                if (discardObjectFilter == null)
                {
                    throw new ArgumentNullException(nameof(discardObjectFilter), "DiscardObjectFilter cannot be null.");
                }
                if (discardCount <= 0)
                {
                    return;
                }

                _aliveTime = 0f;
                List<T> discardingObjects = discardObjectFilter(GetDiscardableObjects(), discardCount, _objectTTL);
                if (discardingObjects == null || discardingObjects.Count <= 0)
                {
                    return;
                }
                foreach (T obj in discardingObjects)
                {
                    Discard(obj);
                }
            }

            private List<T> GetDiscardableObjects()
            {
                _cachedDiscardableObjects.Clear();
                foreach (KeyValuePair<object, PoolObjectBase> pair in _objectMap)
                {
                    PoolObjectBase poolObject = pair.Value;
                    if (poolObject.IsInUse || poolObject.Locked)
                    {
                        continue;
                    }
                    _cachedDiscardableObjects.Add(poolObject as T);
                }
                return _cachedDiscardableObjects;
            }

            private List<T> DefaultDiscardObjectFilter(List<T> candidateObjects, int discardCount, float objectTTL)
            {
                _cachedDiscardingObjects.Clear();
                for (int i = candidateObjects.Count - 1; i >= 0; i--)
                {
                    if (candidateObjects[i].LastUseUtcTime.AddSeconds(objectTTL) < DateTime.UtcNow)
                    {
                        _cachedDiscardingObjects.Add(candidateObjects[i]);
                        candidateObjects.RemoveAt(i);
                    }
                }
                discardCount -= _cachedDiscardingObjects.Count;
                candidateObjects.Sort((a, b) => a.LastUseUtcTime.CompareTo(b.LastUseUtcTime));
                foreach (T obj in candidateObjects)
                {
                    _cachedDiscardingObjects.Add(obj);
                    discardCount--;
                    if (discardCount <= 0)
                    {
                        break;
                    }
                }

                return _cachedDiscardingObjects;
            }
        }
    }
}