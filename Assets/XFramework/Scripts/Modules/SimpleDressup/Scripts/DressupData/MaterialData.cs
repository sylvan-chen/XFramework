using UnityEngine;

namespace XFramework.SimpleDressup
{
    internal enum TextureType
    {
        Base,
        Normal,
        Metallic,
        Occlusion,
        Emission
    }

    /// <summary>
    /// 材质数据
    /// </summary>
    public class DressupMaterialData
    {
        public DressupItem SourceItem;
        public Shader Shader;
        public Texture2D BaseMap;
        public Texture2D NormalMap;
        public Texture2D MetallicMap;
        public Texture2D OcclusionMap;
        public Texture2D EmissionMap;
        public Rect AtlasRect = new(0, 0, 1, 1);

        internal Texture2D GetTexture(TextureType type)
        {
            return type switch
            {
                TextureType.Base => BaseMap,
                TextureType.Normal => NormalMap,
                TextureType.Metallic => MetallicMap,
                TextureType.Occlusion => OcclusionMap,
                TextureType.Emission => EmissionMap,
                _ => null
            };
        }
    }
}
