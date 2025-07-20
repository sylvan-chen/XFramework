using UnityEngine;
using System;
using XFramework.Utils;

namespace SimpleDressup
{
    /// <summary>
    /// 纹理片段数据类 - 代表一个纹理在图集中的区域和相关信息
    /// 这是纹理合并的基本单元
    /// </summary>
    [Serializable]
    public class TextureFragment
    {
        [Header("基础信息")]
        public string FragmentName;
        public DressupSlotType SlotType;

        [Header("纹理数据")]
        public Texture2D[] Textures;  // 多个纹理通道的纹理
        public Color TintColor = Color.white;  // 染色

        [Header("图集信息")]
        public Rect AtlasRegion;  // 在图集中的UV区域
        public bool IsAtlasReady;  // 是否已分配图集区域

        [Header("源信息")]
        public DressupMaterial SourceMaterial;  // 来源材质
        public Vector2 OriginalSize;  // 原始纹理尺寸

        /// <summary>
        /// 获取纹理的像素数量 - 用于装箱算法排序
        /// </summary>
        public int PixelCount
        {
            get
            {
                if (Textures == null || Textures.Length == 0 || Textures[0] == null)
                    return 0;

                return Textures[0].width * Textures[0].height;
            }
        }


        /// <summary>
        /// 构造函数
        /// </summary>
        public TextureFragment(string name, DressupSlotType slotType)
        {
            FragmentName = name;
            SlotType = slotType;
            AtlasRegion = new Rect(0, 0, 0, 0);
            IsAtlasReady = false;
        }

        /// <summary>
        /// 从材质中提取纹理数据
        /// </summary>
        public void ExtractFromMaterial(DressupMaterial material)
        {
            if (material == null || !material.IsValid())
            {
                Log.Error("TextureFragment: DressupMaterial is invalid");
                return;
            }

            SourceMaterial = material;
            var channels = material.TextureChannels;
            Textures = new Texture2D[channels.Length];

            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i].EnableAtlas)
                {
                    var texture = material.GetTexture(channels[i].PropertyName) as Texture2D;
                    Textures[i] = texture;

                    // 记录第一个有效纹理的尺寸作为基准
                    if (i == 0 && texture != null)
                    {
                        OriginalSize = new Vector2(texture.width, texture.height);
                    }
                }
            }
        }

        /// <summary>
        /// 设置在图集中的区域
        /// </summary>
        public void SetAtlasRegion(Rect region)
        {
            AtlasRegion = region;
            IsAtlasReady = true;
        }

        /// <summary>
        /// 检查片段是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(FragmentName) &&
                   Textures != null &&
                   Textures.Length > 0 &&
                   Textures[0] != null;
        }

        /// <summary>
        /// 获取主纹理（通常是第一个通道的纹理）
        /// </summary>
        public Texture2D MainTexture => Textures != null && Textures.Length > 0 ? Textures[0] : null;
    }
}
