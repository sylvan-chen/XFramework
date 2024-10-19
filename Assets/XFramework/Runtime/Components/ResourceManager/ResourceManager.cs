using UnityEngine;
using XFramework.Resource;

namespace XFramework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Resource Manager")]
    public sealed class ResourceManager : XFrameworkComponent
    {
        [SerializeField]
        private ResourceMode resourceMode;

        private IResourceHelper _resourceHelper;

        internal override int Priority
        {
            get => Global.PriorityValue.ResourceManager;
        }

        private void Start()
        {
            if (resourceMode == ResourceMode.Editor)
            {
                // _resourceHelper =
            }
        }
    }
}