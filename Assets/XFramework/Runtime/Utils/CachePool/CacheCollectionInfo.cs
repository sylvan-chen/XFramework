using System;

namespace XFramework
{
    public readonly struct CacheCollectionInfo
    {
        private readonly Type _cacheType;
        private readonly int _unusedCount;
        private readonly int _usingCount;
        private readonly int _spawnedCount;
        private readonly int _unspawnedCount;
        private readonly int _createdCount;
        private readonly int _discardedCount;

        public CacheCollectionInfo(Type cacheType, int unusedCount, int usingCount, int spawnedCount, int unspawnedCount, int createdCount, int discardedCount)
        {
            _cacheType = cacheType;
            _unusedCount = unusedCount;
            _usingCount = usingCount;
            _spawnedCount = spawnedCount;
            _unspawnedCount = unspawnedCount;
            _createdCount = createdCount;
            _discardedCount = discardedCount;
        }

        /// <summary>
        /// 缓存类型
        /// </summary>
        public Type CacheType => _cacheType;

        /// <summary>
        /// 未使用缓存数量
        /// </summary>
        public int UnusedCount => _unusedCount;

        /// <summary>
        /// 使用中的缓存数量
        /// </summary>
        public int UsingCount => _usingCount;

        /// <summary>
        /// 借出缓存次数
        /// </summary>
        public int SpawnedCount => _spawnedCount;

        /// <summary>
        /// 归还缓存次数
        /// </summary>
        public int UnspawnedCount => _unspawnedCount;

        /// <summary>
        /// 创建缓存次数
        /// </summary>
        public int CreatedCount => _createdCount;

        /// <summary>
        /// 销毁缓存次数
        /// </summary>
        public int DiscardedCount => _discardedCount;
    }
}