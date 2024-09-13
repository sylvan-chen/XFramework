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
        /// key 为事件 ID，value 为事件委托调用链。
        /// </remarks>
        private readonly Dictionary<int, EventHandlerChain> _events = new();

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
            if (_events.TryGetValue(id, out EventHandlerChain handlers))
            {
                handlers.Fire(args);
            }
            else
            {
                XLogger.Warning($"[XFramework] [EventManager] Event (id: {id}) has been published but there are no subscribers.");
            }
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