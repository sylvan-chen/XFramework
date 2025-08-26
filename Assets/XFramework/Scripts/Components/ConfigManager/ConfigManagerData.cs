using System.IO;
using UnityEngine;

namespace XFramework
{
    /// <summary>
    /// 配置表管理器组件数据
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigManagerData", menuName = "XFramework/Components/ConfigManagerData")]
    public class ConfigManagerData : ScriptableObject
    {
        [Header("配置表相对路径（基于StreamingAssets）")]
        public string TablePath = "Schemes";

        [Header("自动预加载配置表")]
        public bool AutoPreload = true;
    }
}
