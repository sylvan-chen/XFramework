using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using XFramework.Utils;

namespace XFramework.SimpleDressup
{
    public class MaterialCombiner
    {
        #region 常量定义

        /// <summary>
        /// 每帧处理的像素数量
        /// </summary>
        public const int PIXEL_PROCESS_COUNT_PER_FRAME = 1000;

        #endregion

        #region 数据结构

        public enum TextureType
        {
            Base,
            Normal,
            Metallic,
            Occlusion,
            Emission
        }

        public struct MaterialCombineResult
        {
            public bool Success;
            public Material CombinedMaterial;
            public Texture2D BaseAtlas;
            public Texture2D NormalAtlas;
            public Texture2D MetallicAtlas;
            public Texture2D OcclusionAtlas;
            public Texture2D EmissionAtlas;
            public Dictionary<string, Rect> UvRectMap;
        }

        public class TextureUnit
        {
            public Texture2D BaseMap;
            public Texture2D NormalMap;
            public Texture2D MetallicMap;
            public Texture2D OcclusionMap;
            public Texture2D EmissionMap;
            public Rect UvRect;
        }

        #endregion

        #region 公共接口

        private int _atlasSize;
        private Shader _shader;

        public async UniTask<MaterialCombineResult> CombineMaterialsAsync(List<DressupItem> dressupItems, int atlasSize, Shader shader)
        {
            _atlasSize = atlasSize;
            _shader = shader;

            var result = new MaterialCombineResult { Success = false };

            if (dressupItems == null || dressupItems.Count == 0)
            {
                Log.Warning("[MaterialCombiner] No items to combine.");
                return result;
            }

            // TODO: 进行材质合并操作
            // ...

            return result;
        }

        /// <summary>
        /// 批量从外观物品中提取纹理单元
        /// </summary>
        /// <param name="dressupItems">外观物品列表</param>
        /// <returns>纹理单元数组</returns>
        public TextureUnit[] ExtractTextureUnits(IReadOnlyList<DressupItem> dressupItems)
        {
            var textureUnits = new List<TextureUnit>();

            foreach (var item in dressupItems)
            {
                if (item == null || item.Materials == null || item.Materials.Length == 0)
                {
                    Log.Warning($"[MaterialCombiner] Dressup item '{item.Mesh.name}' has no materials.");
                    continue;
                }

                foreach (var material in item.Materials)
                {
                    if (material == null) continue;

                    var unit = new TextureUnit
                    {
                        BaseMap = ExtractTexture(material, TextureType.Base),
                        NormalMap = ExtractTexture(material, TextureType.Normal),
                        MetallicMap = ExtractTexture(material, TextureType.Metallic),
                        OcclusionMap = ExtractTexture(material, TextureType.Occlusion),
                        EmissionMap = ExtractTexture(material, TextureType.Emission)
                    };

                    // 计算UV矩形
                    // unit.UvRect = CalculateUvRect(unit);

                    textureUnits.Add(unit);
                }
            }

            return textureUnits.ToArray();
        }

        /// <summary>
        /// 根据纹理单元构建合并材质
        /// </summary>
        public async UniTask<MaterialCombineResult> BuildMaterial(TextureUnit[] textureUnits, int atlasSize, Shader shader)
        {
            var result = new MaterialCombineResult() { Success = false };

            if (textureUnits == null || textureUnits.Length == 0)
            {
                Log.Warning("[MaterialCombiner] No texture units to build material.");
                return result;
            }

            // 合并纹理为图集
            var combinedAtlas = new Texture2D(atlasSize, atlasSize);

            var baseTextures = new List<Texture2D>();
            var normalTextures = new List<Texture2D>();
            var metallicTextures = new List<Texture2D>();
            var occlusionTextures = new List<Texture2D>();
            var emissionTextures = new List<Texture2D>();


            foreach (var unit in textureUnits)
            {
                if (unit.BaseMap != null) baseTextures.Add(unit.BaseMap);
                if (unit.NormalMap != null) normalTextures.Add(unit.NormalMap);
                if (unit.MetallicMap != null) metallicTextures.Add(unit.MetallicMap);
                if (unit.OcclusionMap != null) occlusionTextures.Add(unit.OcclusionMap);
                if (unit.EmissionMap != null) emissionTextures.Add(unit.EmissionMap);
            }

            var (baseAtlas, uvRects) = await PackBaseTexturesAsync(baseTextures, atlasSize);

            // TODO: 处理其他纹理图集

            return result;
        }

        #endregion

        #region 私有方法

        private Texture2D ExtractTexture(Material material, TextureType type)
        {
            Texture2D map;

            switch (type)
            {
                case TextureType.Base:
                    map = material.GetTexture("_MainTex") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_BaseMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_BaseColorMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_Albedo") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_Diffuse") as Texture2D;
                    break;
                case TextureType.Normal:
                    map = material.GetTexture("_BumpMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_NormalMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_DetailNormalMap") as Texture2D;
                    break;
                case TextureType.Metallic:
                    map = material.GetTexture("_MetallicGlossMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_MetallicMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_SpecGlossMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_MaskMap") as Texture2D;
                    break;
                case TextureType.Occlusion:
                    map = material.GetTexture("_OcclusionMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_AOMap") as Texture2D;
                    break;
                case TextureType.Emission:
                    map = material.GetTexture("_EmissionMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_EmissiveMap") as Texture2D;
                    if (map == null)
                        map = material.GetTexture("_EmissionColorMap") as Texture2D;
                    break;
                default:
                    map = null;
                    break;
            }

            return map;
        }

        private async UniTask<(Texture2D atlas, Rect[] uvRects)> PackBaseTexturesAsync(List<Texture2D> textures, int atlasSize)
        {
            var atlas = new Texture2D(atlasSize, atlasSize, GetTextureDefaultFormat(TextureType.Base), false)
            {
                name = $"CombinedAtlas_{atlasSize}"
            };

            await UniTask.NextFrame();

            var uvRects = atlas.PackTextures(textures.ToArray(), 2, atlasSize);

            return (atlas, uvRects);
        }

        private async UniTask<Texture2D> PackTexturesWithLayoutAsync(List<Texture2D> textures, int atlasSize, Rect[] uvRects, TextureType textureType)
        {
            var atlas = new Texture2D(atlasSize, atlasSize, GetTextureDefaultFormat(textureType), false)
            {
                name = $"CombinedAtlas_{atlasSize}_{textureType}"
            };

            var backgroundColor = GetTextureDefaultColor(textureType);
            var pixels = new Color32[atlasSize * atlasSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;

                if (i % PIXEL_PROCESS_COUNT_PER_FRAME == 0) await UniTask.NextFrame();
            }

            atlas.SetPixels32(pixels);

            for (int i = 0; i < textures.Count && i < uvRects.Length; i++)
            {
                // TODO
            }

            atlas.Apply();

            return atlas;
        }

        private Color GetTextureDefaultColor(TextureType type)
        {
            return type switch
            {
                TextureType.Base => Color.white,
                TextureType.Normal => new Color(0.5f, 0.5f, 1f),
                TextureType.Metallic => Color.black,
                TextureType.Occlusion => Color.white,
                TextureType.Emission => Color.black,
                _ => Color.white
            };
        }

        private TextureFormat GetTextureDefaultFormat(TextureType type)
        {
            return type switch
            {
                TextureType.Base => TextureFormat.RGBA32,
                TextureType.Normal => TextureFormat.DXT5,
                TextureType.Metallic => TextureFormat.DXT1,
                TextureType.Occlusion => TextureFormat.DXT1,
                TextureType.Emission => TextureFormat.DXT1,
                _ => TextureFormat.RGBA32
            };
        }


        #endregion
    }
}
