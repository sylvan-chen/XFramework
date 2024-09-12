using System;
using System.Collections.Generic;

namespace XFramework
{
    internal sealed partial class EventManger : BaseManager, IEventManager
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        /// <remarks>
        /// key 为事件 ID，value 为事件处理器调用链。实现上，禁止 Action 的多播行为，以链表的形式构建调用链。
        /// </remarks>
        private readonly Dictionary<int, XLinkedList<Action<IEventArgs>>> _eventHandlers4Id = new();

        internal override int Priority
        {
            get { return 0; }
        }

        public void ClearAll()
        {
            throw new NotImplementedException();
        }

        public int EventHandlerCount(int id)
        {
            throw new NotImplementedException();
        }

        public void Publish(int id, IEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "EventArgs cannot be null.");
            }
            _eventHandlers4Id.TryGetValue(id, out XLinkedList<Action<IEventArgs>> handlers);
        }

        public void Subscribe(int id, Action<IEventArgs> handler)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe(int id, Action<IEventArgs> handler)
        {
            throw new NotImplementedException();
        }

        internal override void Shutdown()
        {
            throw new NotImplementedException();
        }

        internal override void Update(float logicSeconds, float realSeconds)
        {
            throw new NotImplementedException();
        }
    }
}