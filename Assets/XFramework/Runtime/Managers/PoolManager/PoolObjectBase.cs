using System;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 池对象基类
    /// </summary>
    /// <remarks>
    /// 对象池中实际管理的是该类对象实例，该对象再包含真正需要管理的对象。
    /// </remarks>
    public abstract class PoolObjectBase : IReference
    {
        private int _referenceCount = 0;

        protected PoolObjectBase(string name, object target, bool locked)
        {
            Name = name ?? string.Empty;
            Target = target ?? throw new ArgumentNullException(nameof(target), "Target can not be null.");
            Locked = locked;
            LastUseUtcTime = DateTime.UtcNow;
        }

        protected PoolObjectBase(string name, object target) : this(name, target, false)
        {
        }

        protected PoolObjectBase(object target) : this(null, target, false)
        {
        }

        /// <summary>
        /// 对象名称
        /// </summary>
        /// <remarks>
        /// 同样的 ObjectBase 可能实际引用不同类型的对象，可以用名称来区分。
        /// </remarks>
        public string Name { get; private set; }

        /// <summary>
        /// 实际管理的对象
        /// </summary>
        public object Target { get; private set; }

        /// <summary>
        /// 是否被锁定
        /// </summary>
        /// <remarks>
        /// 锁定后，即使引用计数为 0，该对象也不会被自动回收。
        /// </remarks>
        public bool Locked { get; set; }

        /// <summary>
        /// 上次使用时间
        /// </summary>
        public DateTime LastUseUtcTime { get; internal set; }

        /// <summary>
        /// 引用计数
        /// </summary>
        public int ReferenceCount
        {
            get => _referenceCount;
        }

        /// <summary>
        /// 是否正在使用
        /// </summary>
        public bool IsInUse
        {
            get => _referenceCount > 0;
        }

        /// <summary>
        /// 借出对象
        /// </summary>
        public PoolObjectBase Spawn()
        {
            _referenceCount++;
            LastUseUtcTime = DateTime.UtcNow;
            OnSpawn();
            return this;
        }

        /// <summary>
        /// 归还对象
        /// </summary>
        public void Unspawn()
        {
            OnUnspawn();
            LastUseUtcTime = DateTime.UtcNow;
            _referenceCount--;
            if (_referenceCount < 0)
            {
                throw new InvalidOperationException("Reference count can not be negative.");
            }
        }

        protected internal abstract void OnSpawn();

        protected internal abstract void OnUnspawn();

        protected internal abstract void Destroy(bool isPoolDestroyed = false);

        public virtual void Clear()
        {
            Name = null;
            Target = null;
            Locked = false;
            LastUseUtcTime = default;
            _referenceCount = 0;
        }
    }
}
