using System;
using System.Collections.Generic;

namespace XFramework.Utils
{
    public static partial class CachePool
    {
        private class CacheCollection
        {
            private readonly Queue<ICache> _cache = new();

            public CacheCollection(Type cacheType)
            {
                CacheType = cacheType;
            }

            public Type CacheType { get; private set; }

            public int Count
            {
                get { return _cache.Count; }
            }

            /// <summary>
            /// 从池中拿出一个缓存，如果池中没有则创建一个新的缓存
            /// </summary>
            public ICache Spawn()
            {
                lock (_cache)
                {
                    if (_cache.Count > 0)
                    {
                        return _cache.Dequeue();
                    }
                }

                return Activator.CreateInstance(CacheType) as ICache;
            }

            /// <summary>
            /// 放入一个缓存
            /// </summary>
            /// <param name="cache">将要放入的缓存</param>
            public void Unspawn(ICache cache)
            {
                if (cache == null)
                {
                    return;
                }
                if (_cache.Contains(cache))
                {
                    throw new InvalidOperationException("Unspawn reference failed. Reference already unspawned.");
                }

                cache.Clear();
                lock (_cache)
                {
                    _cache.Enqueue(cache);
                }
            }

            /// <summary>
            /// 预先创建一部分缓存
            /// </summary>
            /// <param name="count">将要预留的缓存数量</param>
            public void Reserve(int count)
            {
                lock (_cache)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ICache newInstance = Activator.CreateInstance(CacheType) as ICache;
                        if (newInstance == null)
                        {
                            Log.Error($"[XFramework] [ReferencePool] Reserve reference failed. Reference type {CacheType.Name} is invalid.");
                            continue;
                        }
                        _cache.Enqueue(newInstance);
                    }
                }
            }

            /// <summary>
            /// 丢弃一部分缓存
            /// </summary>
            /// <param name="count">将要丢弃的缓存数量</param>
            public void Discard(int count)
            {
                lock (_cache)
                {
                    if (count > _cache.Count)
                    {
                        count = _cache.Count;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        _cache.Dequeue();
                    }
                }
            }

            /// <summary>
            /// 丢弃所有缓存
            /// </summary>
            public void DiscardAll()
            {
                lock (_cache)
                {
                    _cache.Clear();
                }
            }
        }
    }
}