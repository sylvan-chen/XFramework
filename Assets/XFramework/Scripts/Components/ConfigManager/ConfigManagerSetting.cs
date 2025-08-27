using UnityEngine;

namespace XFramework
{
    /// <summary>
    /// 配置表管理器组件数据
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigManagerSetting", menuName = "XFramework/ConfigManagerSetting")]
    public class ConfigManagerSetting : ScriptableObject
    {
        [Tooltip("配置表文件夹名")]
        public string TableFolderName = "Schemes";

        [Tooltip("自动预加载配置表")]
        public bool AutoPreload = true;
    }
}
