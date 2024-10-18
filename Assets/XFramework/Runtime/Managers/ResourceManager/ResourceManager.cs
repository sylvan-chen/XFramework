using UnityEngine;

namespace XFramework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Resource Manager")]
    public sealed class ResourceManager : XFrameworkComponent
    {
        [SerializeField]
        private ResourceMode resourceMode;

        private void Start()
        {

        }
    }
}