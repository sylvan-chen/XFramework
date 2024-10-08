using System;

namespace XFramework
{
    public abstract class ObjectBase
    {
        protected ObjectBase(string name, object target, bool locked, int priority)
        {
            Name = name ?? string.Empty;
            Target = target ?? throw new ArgumentNullException(nameof(target), "Target can not be null.");
            Locked = locked;
            Priority = priority;
            LastUseUtcTime = DateTime.UtcNow;
        }

        protected ObjectBase(string name, object target, int priority) : this(name, target, false, priority)
        {
        }

        protected ObjectBase(string name, object target, bool locked) : this(name, target, locked, 0)
        {
        }

        protected ObjectBase(string name, object target) : this(name, target, false, 0)
        {
        }

        protected ObjectBase(object target) : this(null, target, false, 0)
        {
        }

        protected ObjectBase() : this(null, null, false, 0)
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

        public int Priority { get; set; }

        public DateTime LastUseUtcTime { get; internal set; }

        protected internal abstract void OnSpawn();

        protected internal abstract void OnUnspawn();

        protected internal abstract void Destroy(bool isPoolDestroyed);
    }
}
