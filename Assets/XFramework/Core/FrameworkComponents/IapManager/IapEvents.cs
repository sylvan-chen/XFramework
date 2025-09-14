using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace XGame.Core
{
    public class EventIapGrantReward : IEvent
    {
        public static EventIapGrantReward Create()
        {
            return CachePool.Spawn<EventIapGrantReward>();
        }

        public void Destroy()
        {
            CachePool.Unspawn(this);
        }

        public void Clear()
        {
        }
    }


    public delegate void IapOnGrantRewardEvent(string productId, List<PayoutDefinition> payouts);

    public delegate void IapOnPurchaseFinishedEvent(string productId, bool success);
}