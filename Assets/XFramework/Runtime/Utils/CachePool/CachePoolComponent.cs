using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Cache Pool")]
    public sealed class CachePoolComponent : XFrameworkComponent
    {
        public override void Clear()
        {
            base.Clear();

            CachePool.Clear();
        }
    }
}