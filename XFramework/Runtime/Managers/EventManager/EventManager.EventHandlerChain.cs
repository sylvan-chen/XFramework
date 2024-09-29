using System;
using XFramework.Utils;

namespace XFramework
{
    public sealed partial class EventManager
    {
        /// <summary>
        /// 事件委托链
        /// </summary>
        private class EventHandlerChain
        {
            // 用链表实现事件委托链而不是直接用 +=
            private readonly XLinkedList<Action<IEventArgs>> _handlers = new();

            public int Count
            {
                get { return _handlers.Count; }
            }

            public void AddHandler(Action<IEventArgs> handler)
            {
                _handlers.AddLast(handler);
            }

            public void RemoveHandler(Action<IEventArgs> handler)
            {
                _handlers.Remove(handler);
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