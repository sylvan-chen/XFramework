using System;

namespace XFramework
{
    internal sealed partial class EventManger
    {
        /// <summary>
        /// 事件委托链
        /// </summary>
        private class EventHandlerChain
        {
            // 约束上最好禁止单个 Action 的多播行为。
            private readonly XLinkedList<Action<IEventArgs>> _handlers = new();

            public void AddHandler(Action<IEventArgs> handler)
            {
            }

            public void RemoveHandler(Action<IEventArgs> handler)
            {
            }

            public void Fire(IEventArgs args)
            {
                foreach (Action<IEventArgs> handler in _handlers)
                {
                    handler?.Invoke(args);
                }
            }
        }
    }
}