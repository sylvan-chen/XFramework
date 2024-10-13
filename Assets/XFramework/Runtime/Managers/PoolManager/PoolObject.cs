using System;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 池对象
    /// </summary>
    /// <remarks>
    /// 对象池不直接管理实际对象，而是管理池对象，池对象中再包含实际对象。
    /// </remarks>
    public sealed class PoolObject : IReference
    {
        internal Action OnSpawn;
        internal Action OnUnspawn;
        internal Action OnDestroy;

        /// <summary>
        /// 实际管理的对象
        /// </summary>
        public object Target { get; private set; }

        /// <summary>
        /// 是否被锁定
        /// </summary>
        /// <remarks>
        /// 锁定的对象即使引用计数为 0 也不会被任何形式的自动丢弃机制释放，而是一直保留在对象池中，直到手动解锁。
        /// </remarks>
        public bool Locked { get; internal set; }

        /// <summary>
        /// 上次使用时间
        /// </summary>
        public DateTime LastUseUtcTime { get; internal set; }

        /// <summary>
        /// 引用计数
        /// </summary>
        public int SpawnCount { get; internal set; }

        /// <summary>
        /// 是否正在使用
        /// </summary>
        public bool IsInUse
        {
            get => SpawnCount > 0;
        }

        internal static PoolObject Create(object target, bool locked = false)
        {
            PoolObject poolObject = ReferencePool.Spawn<PoolObject>();
            poolObject.Target = target ?? throw new ArgumentNullException(nameof(target), "Target can not be null.");
            poolObject.Locked = locked;
            poolObject.LastUseUtcTime = DateTime.UtcNow;
            poolObject.SpawnCount = 0;
            return poolObject;
        }

        /// <summary>
        /// 借出对象
        /// </summary>
        internal PoolObject Spawn()
        {
            SpawnCount++;
            LastUseUtcTime = DateTime.UtcNow;
            OnSpawn?.Invoke();
            return this;
        }

        /// <summary>
        /// 归还对象
        /// </summary>
        internal void Unspawn()
        {
            OnUnspawn?.Invoke();
            LastUseUtcTime = DateTime.UtcNow;
            SpawnCount--;
            if (SpawnCount < 0)
            {
                throw new InvalidOperationException("Reference count can not be negative.");
            }
        }

        internal void Destroy()
        {
            OnDestroy?.Invoke();
            ReferencePool.Unspawn(this);
        }

        public void Clear()
        {
            OnSpawn = null;
            OnUnspawn = null;
            OnDestroy = null;
            Target = null;
            Locked = false;
            LastUseUtcTime = default;
            SpawnCount = 0;
        }
    }
}
