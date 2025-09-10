using UnityEngine;

namespace XGame.Core
{
    public enum IapProviderType
    {
        None,
        GooglePlay,
        AppleAppStore,
        AmazonAppStore,
        HuaweiAppGallery
    }

    [CreateAssetMenu(fileName = "IapManagerSetting", menuName = "XFramework/IapManagerSetting")]
    public class IapManagerSetting : ScriptableObject
    {
        public IapProviderType ProviderType = IapProviderType.None;
    }
}