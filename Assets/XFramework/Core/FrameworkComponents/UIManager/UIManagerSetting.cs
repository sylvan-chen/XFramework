using UnityEngine;

namespace XGame.Core
{
    [CreateAssetMenu(fileName = "UIManagerSetting", menuName = "XFramework/UIManagerSetting")]
    public class UIManagerSetting : ScriptableObject
    {
        public string UIRootName = "[UIRoot]";
        public Camera UICamera;
    }
}