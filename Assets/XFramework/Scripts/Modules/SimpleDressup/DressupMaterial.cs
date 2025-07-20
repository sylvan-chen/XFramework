using UnityEngine;
using System;

namespace SimpleDressup
{
    /// <summary>
    /// 简化的材质数据类 - Unity Material 的封装
    /// 定义了哪些纹理通道需要参与图集合并
    /// </summary>
    [CreateAssetMenu(fileName = "New Dressup Material", menuName = "SimpleDressup/Dressup Material")]
    public class DressupMaterial : ScriptableObject
    {
        [Header("基础设置")]
        public Material SourceMaterial;  // 原始Unity材质

        [Header("纹理通道配置")]
        public TextureChannel[] TextureChannels = new TextureChannel[0];

        /// <summary>
        /// 检查材质是否有效
        /// </summary>
        public bool IsValid()
        {
            return SourceMaterial != null && TextureChannels.Length > 0;
        }

        /// <summary>
        /// 获取指定属性的纹理
        /// </summary>
        public Texture GetTexture(string propertyName)
        {
            if (SourceMaterial == null || !SourceMaterial.HasProperty(propertyName))
                return null;

            return SourceMaterial.GetTexture(propertyName);
        }

        public enum TextureType
        {
            Diffuse,    // 漫反射
            Normal,     // 法线
            Mask        // 遮罩
        }

        /// <summary>
        /// 纹理通道 - 定义需要合并的纹理属性
        /// </summary>
        [Serializable]
        public struct TextureChannel
        {
            [Tooltip("纹理属性名称，如_BaseMap, _BumpMap等")]
            public string PropertyName;

            [Tooltip("是否参与图集合并")]
            public bool EnableAtlas;

            [Tooltip("纹理类型")]
            public TextureType TextureType;
        }
    }
}
