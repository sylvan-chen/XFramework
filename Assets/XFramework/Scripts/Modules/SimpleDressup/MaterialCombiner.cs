using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
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
        }

        public struct TextureUnit
        {
            public Texture2D BaseMap;
            public Texture2D NormalMap;
            public Texture2D MetallicMap;
            public Texture2D OcclusionMap;
            public Texture2D EmissionMap;
            public Rect UvRect;

            public readonly Texture2D GetTexture(TextureType type)
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

        #endregion

        #region 公共接口

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

                    textureUnits.Add(unit);
                }
            }

            return textureUnits.ToArray();
        }

        /// <summary>
        /// 根据纹理单元构建合并材质
        /// </summary>
        public async UniTask<MaterialCombineResult> BuildCombinedMaterialAsync(TextureUnit[] textureUnits, int atlasSize, Shader shader)
        {
            var result = new MaterialCombineResult() { Success = false };

            if (textureUnits == null || textureUnits.Length == 0)
            {
                Log.Warning("[MaterialCombiner] No texture units to build material.");
                return result;
            }

            // 先打包所有BaseMap用于确定统一的UV布局
            var baseTextures = new List<Texture2D>();
            foreach (var unit in textureUnits)
            {
                if (unit.BaseMap != null)
                    baseTextures.Add(unit.BaseMap);
                else
                    baseTextures.Add(Texture2D.whiteTexture); // 使用白色纹理填充空位，确保布局正确
            }

            try
            {
                var (baseAtlas, uvRects) = await PackBaseTexturesAsync(baseTextures, atlasSize);
                result.BaseAtlas = baseAtlas;

                for (int i = 0; i < textureUnits.Length; i++)
                {
                    textureUnits[i].UvRect = uvRects[i];
                }

                // 并行打包其他纹理
                var normalPackTask = PackOtherTexturesAsync(textureUnits, atlasSize, TextureType.Normal);
                var metallicPackTask = PackOtherTexturesAsync(textureUnits, atlasSize, TextureType.Metallic);
                var occlusionPackTask = PackOtherTexturesAsync(textureUnits, atlasSize, TextureType.Occlusion);
                var emissionPackTask = PackOtherTexturesAsync(textureUnits, atlasSize, TextureType.Emission);


                var (normalTexture, metallicTexture, occlusionTexture, emissionTexture) =
                    await UniTask.WhenAll(normalPackTask, metallicPackTask, occlusionPackTask, emissionPackTask);

                result.NormalAtlas = normalTexture;
                result.MetallicAtlas = metallicTexture;
                result.OcclusionAtlas = occlusionTexture;
                result.EmissionAtlas = emissionTexture;

                // 创建合并材质
                result.CombinedMaterial = CreateMaterial(
                    baseAtlas,
                    normalTexture,
                    metallicTexture,
                    occlusionTexture,
                    emissionTexture,
                    shader
                );
                result.Success = true;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[MaterialCombiner] Failed to pack textures: {ex.Message}");
                result.Success = false;
                ClearResult(result);
                return result;
            }

            return result;
        }

        #endregion

        #region 私有方法

        private Texture2D ExtractTexture(Material material, TextureType type)
        {
            Texture2D map = null;

            switch (type)
            {
                case TextureType.Base:
                    if (material.HasTexture("_BaseMap"))
                    {
                        map = material.GetTexture("_BaseMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_MainTex"))
                    {
                        map = material.GetTexture("_MainTex") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_BaseColorMap"))
                    {
                        map = material.GetTexture("_BaseColorMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_Albedo"))
                    {
                        map = material.GetTexture("_Albedo") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_Diffuse"))
                    {
                        map = material.GetTexture("_Diffuse") as Texture2D;
                        break;
                    }
                    break;
                case TextureType.Normal:
                    if (material.HasTexture("_BumpMap"))
                    {
                        map = material.GetTexture("_BumpMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_NormalMap"))
                    {
                        map = material.GetTexture("_NormalMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_DetailNormalMap"))
                    {
                        map = material.GetTexture("_DetailNormalMap") as Texture2D;
                        break;
                    }
                    break;
                case TextureType.Metallic:
                    if (material.HasTexture("_MetallicGlossMap"))
                    {
                        map = material.GetTexture("_MetallicGlossMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_MetallicMap"))
                    {
                        map = material.GetTexture("_MetallicMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_SpecGlossMap"))
                    {
                        map = material.GetTexture("_SpecGlossMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_MaskMap"))
                    {
                        map = material.GetTexture("_MaskMap") as Texture2D;
                        break;
                    }
                    break;
                case TextureType.Occlusion:
                    if (material.HasTexture("_OcclusionMap"))
                    {
                        map = material.GetTexture("_OcclusionMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_AOMap"))
                    {
                        map = material.GetTexture("_AOMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_AmbientOcclusionMap"))
                    {
                        map = material.GetTexture("_AmbientOcclusionMap") as Texture2D;
                        break;
                    }
                    break;
                case TextureType.Emission:
                    if (material.HasTexture("_EmissionMap"))
                    {
                        map = material.GetTexture("_EmissionMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_EmissiveMap"))
                    {
                        map = material.GetTexture("_EmissiveMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_EmissionColorMap"))
                    {
                        map = material.GetTexture("_EmissionColorMap") as Texture2D;
                        break;
                    }
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

        private async UniTask<Texture2D> PackOtherTexturesAsync(TextureUnit[] textureUnits, int atlasSize, TextureType textureType)
        {
            var atlas = new Texture2D(atlasSize, atlasSize, GetTextureDefaultFormat(textureType), false)
            {
                name = $"CombinedAtlas_{atlasSize}_{textureType}"
            };

            // 填充默认颜色
            var backgroundColor = GetTextureDefaultColor(textureType);
            var pixels = new Color32[atlasSize * atlasSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;

                if (i > 0 && i % PIXEL_PROCESS_COUNT_PER_FRAME == 0) await UniTask.NextFrame();
            }

            atlas.SetPixels32(pixels);

            // 将存在的贴图绘制到图集的对应位置
            for (int i = 0; i < textureUnits.Length; i++)
            {
                var unit = textureUnits[i];
                var sourceTexture = unit.GetTexture(textureType);

                if (sourceTexture == null) continue;

                var uvRect = unit.UvRect;
                await CopyTextureToAtlasAsync(sourceTexture, atlas, uvRect);
            }

            atlas.Apply(true, false);

            return atlas;
        }

        private async UniTask CopyTextureToAtlasAsync(Texture2D texture, Texture2D atlas, Rect uvRect)
        {
            if (!texture.isReadable)
            {
                Log.Error($"[MaterialCombiner] Pack textures failed. Source texture '{texture.name}' is not readable.");
                return;
            }

            int targetX = Mathf.FloorToInt(uvRect.x * atlas.width);
            int targetY = Mathf.FloorToInt(uvRect.y * atlas.height);
            int targetWidth = Mathf.FloorToInt(uvRect.width * atlas.width);
            int targetHeight = Mathf.FloorToInt(uvRect.height * atlas.height);

            // 简单双线性插值缩放
            for (int h = 0; h < targetHeight; h++)
            {
                for (int w = 0; w < targetWidth; w++)
                {
                    float u = (float)w / targetWidth;
                    float v = (float)h / targetHeight;

                    int sourceX = Mathf.FloorToInt(u * texture.width);
                    int sourceY = Mathf.FloorToInt(v * texture.height);

                    if (sourceX < 0 || sourceX >= texture.width || sourceY < 0 || sourceY >= texture.height)
                        continue;

                    Color pixelColor = texture.GetPixel(sourceX, sourceY);
                    atlas.SetPixel(targetX + w, targetY + h, pixelColor);

                    if (h > 0 && (h * targetWidth + w) % PIXEL_PROCESS_COUNT_PER_FRAME == 0) await UniTask.NextFrame();
                }
            }
        }

        private Material CreateMaterial(Texture2D baseAtlas, Texture2D normalAtlas, Texture2D metallicAtlas, Texture2D occlusionAtlas, Texture2D emissionAtlas, Shader shader)
        {
            var material = new Material(shader)
            {
                name = "CombinedMaterial"
            };

            if (baseAtlas != null)
                material.SetTexture("_BaseMap", baseAtlas);

            if (normalAtlas != null)
                material.SetTexture("_BumpMap", normalAtlas);

            if (metallicAtlas != null)
                material.SetTexture("_MetallicGlossMap", metallicAtlas);

            if (occlusionAtlas != null)
                material.SetTexture("_OcclusionMap", occlusionAtlas);

            if (emissionAtlas != null)
                material.SetTexture("_EmissionMap", emissionAtlas);

            return material;
        }

        private void ClearResult(MaterialCombineResult result)
        {
            if (result.BaseAtlas != null)
            {
                Object.Destroy(result.BaseAtlas);
                result.BaseAtlas = null;
            }

            if (result.NormalAtlas != null)
            {
                Object.Destroy(result.NormalAtlas);
                result.NormalAtlas = null;
            }

            if (result.MetallicAtlas != null)
            {
                Object.Destroy(result.MetallicAtlas);
                result.MetallicAtlas = null;
            }

            if (result.OcclusionAtlas != null)
            {
                Object.Destroy(result.OcclusionAtlas);
                result.OcclusionAtlas = null;
            }

            if (result.EmissionAtlas != null)
            {
                Object.Destroy(result.EmissionAtlas);
                result.EmissionAtlas = null;
            }

            if (result.CombinedMaterial != null)
            {
                Object.Destroy(result.CombinedMaterial);
                result.CombinedMaterial = null;
            }
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
