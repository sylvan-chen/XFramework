using System;

namespace XGame.Core
{
    public sealed partial class EventManager
    {
        /// <summary>
        /// 事件委托链
        /// </summary>
        private class EventListenerChain : ICache
        {
            // 用链表实现事件委托链而不是直接用 +=
            private readonly XLinkedList<Action<IEvent>> _listeners = new();

            public static EventListenerChain Create()
            {
                return CachePool.Spawn<EventListenerChain>();
            }

            public void Destroy()
            {
                CachePool.Unspawn(this);
            }

            public int Count
            {
                get { return _listeners.Count; }
            }

            public void AddListener(Action<IEvent> listener)
            {
                _listeners.AddLast(listener);
            }

            public void RemoveListener(Action<IEvent> listener)
            {
                _listeners.Remove(listener);
            }

            public void Invoke(IEvent evt)
            {
                foreach (Action<IEvent> listener in _listeners)
                {
                    listener?.Invoke(evt);
                }
            }

            public void Clear()
            {
                _listeners.Clear();
            }
        }
    }
}