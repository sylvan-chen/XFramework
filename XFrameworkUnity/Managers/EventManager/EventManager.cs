using UnityEngine;
using XFramework;

namespace XFrameworkUnity
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/EventManager")]
    public sealed class EventManager : BaseManager
    {
        private readonly IEventSystem _eventSystem = XFrameworkGlobal.GetSystem<IEventSystem>();
    }
}
