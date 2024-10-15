using System;
using XFramework.Utils;

namespace XFramework
{
    public sealed partial class EventManager
    {
        /// <summary>
        /// 延迟事件包装类
        /// </summary>
        private class DelayEventWrapper : ICache
        {
            public IEvent Event;
            public EventHandlerChain HandlerChain;
            public int DelayFrame;

            public static DelayEventWrapper Create(IEvent evt, EventHandlerChain handlerChain, int delayFrame)
            {
                var wrapper = CachePool.Spawn<DelayEventWrapper>();
                wrapper.Event = evt ?? throw new ArgumentNullException(nameof(evt), "Spawn DelayEventWrapper failed. Args is null.");
                wrapper.HandlerChain = handlerChain ?? throw new ArgumentNullException(nameof(handlerChain), "Spawn DelayEventWrapper failed. HandlerChain is null.");
                wrapper.DelayFrame = delayFrame;
                return wrapper;
            }

            public void Destroy()
            {
                CachePool.Unspawn(this);
            }

            public void Clear()
            {
                Event = null;
                HandlerChain = null;
                DelayFrame = 0;
            }
        }
    }
}