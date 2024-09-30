using System;
using System.Collections.Generic;

namespace XFramework.Utils
{
    public static partial class ReferencePool
    {
        private class ReferenceCollection
        {
            private readonly Queue<IReference> _references = new();

            public ReferenceCollection(Type referenceType)
            {
                ReferenceType = referenceType;
            }

            public Type ReferenceType { get; private set; }

            public int UnusedReferenceCount
            {
                get { return _references.Count; }
            }

            /// <summary>
            /// 孵化一个可用的引用
            /// </summary>
            public IReference Spawn()
            {
                lock (_references)
                {
                    if (_references.Count > 0)
                    {
                        return _references.Dequeue();
                    }
                }

                return Activator.CreateInstance(ReferenceType) as IReference ?? throw new InvalidOperationException($"Spawn reference of type {ReferenceType} failed.");
            }

            /// <summary>
            /// 释放一个引用
            /// </summary>
            /// <param name="reference">将要释放的引用</param>
            public void Release(IReference reference)
            {
                if (reference == null)
                {
                    return;
                }
                if (_references.Contains(reference))
                {
                    throw new InvalidOperationException("Release reference failed. Reference already released.");
                }

                reference.Clear();
                lock (_references)
                {
                    _references.Enqueue(reference);
                }
            }

            /// <summary>
            /// 预先缓存一部分引用
            /// </summary>
            /// <param name="count">将要预留的引用数量</param>
            public void Reserve(int count)
            {
                lock (_references)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (Activator.CreateInstance(ReferenceType) is not IReference newInstance)
                        {
                            Log.Error("[XFramework] [ReferencePool] Reserve reference failed. Reference type is invalid.");
                            continue;
                        }
                        _references.Enqueue(newInstance);
                    }
                }
            }

            /// <summary>
            /// 丢弃一部分缓存的引用
            /// </summary>
            /// <param name="count">将要丢弃的引用数量</param>
            public void Discard(int count)
            {
                lock (_references)
                {
                    if (count > _references.Count)
                    {
                        count = _references.Count;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        _references.Dequeue();
                    }
                }
            }

            /// <summary>
            /// 丢弃所有缓存的引用
            /// </summary>
            public void DiscardAll()
            {
                lock (_references)
                {
                    _references.Clear();
                }
            }
        }
    }
}