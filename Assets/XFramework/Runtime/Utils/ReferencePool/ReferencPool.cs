using System;
using System.Collections.Generic;

namespace XFramework.Utils
{
    public static partial class ReferencePool
    {
        private static readonly Dictionary<Type, ReferenceCollection> _referenceCollections = new();

        /// <summary>
        /// 引用集合的数量（引用池中的类型数量）
        /// </summary>
        public static int Count => _referenceCollections.Count;

        /// <summary>
        /// 清空所有引用
        /// </summary>
        public static void Clear()
        {
            foreach (ReferenceCollection referenceCollection in _referenceCollections.Values)
            {
                referenceCollection.DiscardAll();
            }
            _referenceCollections.Clear();
        }

        /// <summary>
        /// 孵化一个指定类型的引用
        /// </summary>
        /// <param name="type">要孵化的引用类型</param>
        /// <returns>孵化出的引用</returns>
        public static IReference Spawn(Type type)
        {
            CheckTypeCompilance(type);
            return GetReferenceCollection(type).Spawn();
        }

        /// <summary>
        /// 孵化一个指定类型的引用
        /// </summary>
        /// <typeparam name="T">要孵化的引用类型</typeparam>
        /// <returns>孵化出的引用</returns>
        public static T Spawn<T>() where T : class, IReference, new()
        {
            return GetReferenceCollection(typeof(T)).Spawn() as T;
        }

        /// <summary>
        /// 回收一个引用
        /// </summary>
        /// <param name="reference">要回收的引用</param>
        public static void Unspawn(IReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference), "Release failed. Reference is null.");
            }
            Type referenceType = reference.GetType();
            CheckTypeCompilance(referenceType);

            GetReferenceCollection(referenceType).Unspawn(reference);
        }

        /// <summary>
        /// 预留指定类型的指定数量的引用
        /// </summary>
        /// <param name="type">要预留的引用类型</param>
        /// <param name="count">要预留的数量</param>
        public static void Reserve(Type type, int count)
        {
            CheckTypeCompilance(type);
            GetReferenceCollection(type).Reserve(count);
        }

        /// <summary>
        /// 预留指定类型的指定数量的引用
        /// </summary>
        /// <typeparam name="T">要预留的引用类型</typeparam>
        /// <param name="count">要预留的数量</param>
        public static void Reserve<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Reserve(count);
        }

        /// <summary>
        /// 丢弃指定类型的指定数量的引用
        /// </summary>
        /// <param name="type">要丢弃的引用类型</param>
        /// <param name="count">要丢弃的数量</param>
        public static void Discard(Type type, int count)
        {
            CheckTypeCompilance(type);
            GetReferenceCollection(type).Discard(count);
        }

        /// <summary>
        /// 丢弃指定类型的指定数量的引用
        /// </summary>
        /// <typeparam name="T">要丢弃的引用类型</typeparam>
        /// <param name="count">要丢弃的数量</param>
        public static void Discard<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Discard(count);
        }

        /// <summary>
        /// 丢弃指定类型的所有引用
        /// </summary>
        /// <param name="type">要丢弃的引用类型</param>
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
#endif
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