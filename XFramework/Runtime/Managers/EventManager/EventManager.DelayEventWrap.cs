namespace XFramework
{
    public sealed partial class EventManager
    {
        /// <summary>
        /// 延迟事件包装类
        /// </summary>
        private class DelayEventWrapper
        {

            public IEventArgs Args;
            public EventHandlerChain HandlerChain;
            public int DelayFrame;

            public DelayEventWrapper(IEventArgs args, EventHandlerChain handlerChain, int delayFrame)
            {
                Args = args;
                HandlerChain = handlerChain;
                DelayFrame = delayFrame;
            }
        }
    }
}