using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace XGame.Core.Iap
{
    public delegate void IapOnGrantRewardEvent(string productId, List<PayoutDefinition> payouts);

    public delegate void IapOnPurchaseFinishedEvent(string productId, bool success);
}