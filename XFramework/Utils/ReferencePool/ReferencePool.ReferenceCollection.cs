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

            // public T Spawn<T>() where T : class, IReference, new()
            // {
            //     if (typeof(T) != ReferenceType)
            //     {
            //         throw new ArgumentException("Reference spawn failed. Reference type does not match the collection type.");
            //     }

            //     lock (_references)
            //     {
            //         if (_references.Count > 0)
            //         {
            //             return _references.Dequeue() as T;
            //         }
            //     }
            // }

        }
    }
}