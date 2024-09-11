using System;
using System.Collections.Generic;

namespace XFramework
{
    public static partial class XReferencePool
    {
        /// <summary>
        /// 单种类别引用对象的集合
        /// </summary>
        private sealed class ReferenceCollection
        {
            private readonly Queue<IReference> _pool;
            private readonly Type _referenceType;
            private int _usingReferenceCount;
            private int _acquiredReferenceCount;
            private int _releasedReferenceCount;
            private int _addedReferenceCount;
            private int _removedReferenceCount;

            public ReferenceCollection(Type referenceType)
            {
                if (referenceType is null)
                {
                    throw new ArgumentNullException(nameof(referenceType), "Reference type cannot be null.");
                }
            }
        }
    }
}
