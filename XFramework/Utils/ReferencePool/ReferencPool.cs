using System;
using System.Collections.Generic;

namespace XFramework.Utils
{
    public static partial class ReferencePool
    {
        private static readonly Dictionary<Type, ReferenceCollection> _referenceCollections = new();

        /// <summary>
        /// 引用集合的数量
        /// </summary>
        public static int Count => _referenceCollections.Count;

        public static IReference Spawn(Type type)
        {
            CheckTypeCompilance(type);
            return GetReferenceCollection(type).Spawn();
        }

        public static T Spawn<T>() where T : class, IReference, new()
        {
            return GetReferenceCollection(typeof(T)).Spawn() as T;
        }

        public static void Release(IReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference), "Release failed. Reference is null.");
            }
            Type referenceType = reference.GetType();
            CheckTypeCompilance(referenceType);

            GetReferenceCollection(referenceType).Release(reference);
        }

        public static void Reserve(Type type, int count)
        {
            CheckTypeCompilance(type);
            GetReferenceCollection(type).Reserve(count);
        }

        public static void Reserve<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Reserve(count);
        }

        public static void Discard(Type type, int count)
        {
            CheckTypeCompilance(type);
            GetReferenceCollection(type).Discard(count);
        }

        public static void Discard<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Discard(count);
        }

        public static void DiscardAll(Type type)
        {
            CheckTypeCompilance(type);
            GetReferenceCollection(type).DiscardAll();
        }

        public static void DiscardAll<T>() where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).DiscardAll();
        }

        private static void CheckTypeCompilance(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type), "Check type compilance failed. Type is null.");
            }
            if (!type.IsClass || type.IsAbstract)
            {
                throw new ArgumentException("Check type compilance failed. Type must be a non-abstract class.", nameof(type));
            }
            if (!typeof(IReference).IsAssignableFrom(type))
            {
                throw new ArgumentException("Check type compilance failed. Type is not a IReference type.", nameof(type));
            }
        }

        private static ReferenceCollection GetReferenceCollection(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type), "Get ReferenceCollection failed. Type is null.");
            }

            if (!_referenceCollections.TryGetValue(type, out ReferenceCollection referenceCollection))
            {
                referenceCollection = new ReferenceCollection(type);
                _referenceCollections.Add(type, referenceCollection);
            }
            return referenceCollection;
        }
    }
}