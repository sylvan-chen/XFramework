using System;
using XFramework.Utils;

namespace XFramework
{
    public sealed partial class EventManager
    {
        /// <summary>
        /// 延迟事件包装类
        /// </summary>
        private class DelayEventWrapper : IReference
        {

            public IEventArgs Args;
            public EventHandlerChain HandlerChain;
            public int DelayFrame;

            public static DelayEventWrapper Create(IEventArgs args, EventHandlerChain handlerChain, int delayFrame)
            {
                var wrapper = ReferencePool.Spawn<DelayEventWrapper>();
                wrapper.Args = args ?? throw new ArgumentNullException(nameof(args), "Spawn DelayEventWrapper failed. Args is null.");
                wrapper.HandlerChain = handlerChain ?? throw new ArgumentNullException(nameof(handlerChain), "Spawn DelayEventWrapper failed. HandlerChain is null.");
                wrapper.DelayFrame = delayFrame;
                return wrapper;
            }

            public void Destroy()
            {
                ReferencePool.Release(this);
            }

            public void Clear()
            {
                Args = null;
                HandlerChain = null;
                DelayFrame = 0;
            }
        }
    }
}