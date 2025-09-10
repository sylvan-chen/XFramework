using System;

namespace XGame.Core
{
    public sealed partial class EventManager
    {
        /// <summary>
        /// 延迟事件包装类
        /// </summary>
        private class DelayedEvent : ICache
        {
            public IEvent Event;
            public EventListenerChain ListenerChain;
            public float DelaySeconds;

            public static DelayedEvent Create(IEvent evt, EventListenerChain handlerChain, float delaySeconds)
            {
                var wrapper = CachePool.Spawn<DelayedEvent>();
                wrapper.Event = evt ?? throw new ArgumentNullException(nameof(evt), "Spawn DelayEventWrapper failed. Args is null.");
                wrapper.ListenerChain = handlerChain ?? throw new ArgumentNullException(nameof(handlerChain), "Spawn DelayEventWrapper failed. HandlerChain is null.");
                wrapper.DelaySeconds = delaySeconds;
                return wrapper;
            }

            public void Destroy()
            {
                CachePool.Unspawn(this);
            }

            public void Clear()
            {
                Event.Destroy();
                Event = null;
                ListenerChain = null;
                DelaySeconds = 0;
            }
        }
    }
}