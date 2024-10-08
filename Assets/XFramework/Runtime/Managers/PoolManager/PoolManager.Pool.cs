using System;
using System.Collections.Generic;

namespace XFramework
{
    public sealed partial class PoolManager
    {
        private class Pool<T> where T : ObjectBase, new()
        {
            private readonly Dictionary<string, List<ObjectBase>> _objectsDict = new();
            private readonly Dictionary<object, ObjectBase> _objectBaseDict = new();
            private readonly bool _allowMultiReference;
            private float _autoDicardInterval;
            private float _expireTime;
            private int _capacity;

            private float _aliveTime = 0f;
            private List<T> _cachedDiscardingObjects = new();

            public Pool(bool allowMultiReference, float autoDicardInterval, float expireTime, int capacity)
            {
                _allowMultiReference = allowMultiReference;
                _autoDicardInterval = autoDicardInterval;
                _expireTime = expireTime;
                _capacity = capacity;
            }

            public Type ObjectType
            {
                get => typeof(T);
            }

            public int ObjectCount
            {
                get => _objectBaseDict.Count;
            }

            public int Capacity
            {
                get => _capacity;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("Capacity must be greater than or equal to 0.", nameof(value));
                    }
                    _capacity = value;
                    Squeeze();
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
                List<T> discardObjects = discardObjectFilter(GetDiscardableObjects(), discardCount, _expireTime);
                if (discardObjects == null || discardObjects.Count <= 0)
                {
                    return;
                }
                foreach (T obj in discardObjects)
                {
                    // TODO 丢弃对象
                }
            }

            private List<T> GetDiscardableObjects()
            {
                List<T> discardableObjects = new();
                foreach (KeyValuePair<object, ObjectBase> pair in _objectBaseDict)
                {
                    ObjectBase obj = pair.Value;
                    // if (obj.)
                }
                return discardableObjects;
            }

            private List<T> DefaultDiscardObjectFilter(List<T> candidateObjects, int discardCount, float expireTime)
            {
                _cachedDiscardingObjects.Clear();
                for (int i = candidateObjects.Count - 1; i >= 0; i--)
                {
                    if (candidateObjects[i].LastUseUtcTime.AddSeconds(expireTime) < DateTime.UtcNow)
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