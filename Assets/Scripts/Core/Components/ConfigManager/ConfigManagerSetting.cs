using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 配置表管理器组件数据
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigManagerSetting", menuName = "XFramework/ConfigManagerSetting")]
    public class ConfigManagerSetting : ScriptableObject
    {
        public bool AutoPreload = true;
        public string PreloadTableNameSpace = "XGame.Table";
        public string[] PreloadTableFolderNames = new[] { "Schemes" };
    }
}
