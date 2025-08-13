using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.SimpleDressup
{
    /// <summary>
    /// 图集材质生成器
    /// </summary>
    public class AtlasGenerator
    {
        #region 常量定义

        /// <summary>
        /// 每帧处理的像素数量
        /// </summary>
        public const int PIXEL_PROCESS_COUNT_PER_FRAME = 15000;

        /// <summary>
        /// 纹理图集的格式
        /// </summary>
        public const TextureFormat TEXTURE_FORMAT = TextureFormat.RGBA32;

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

        public struct AtlasInfo
        {
            public bool IsValid;
            public Material ResultMaterial;
            public Texture2D BaseAtlas;
            public Texture2D NormalAtlas;
            public Texture2D MetallicAtlas;
            public Texture2D OcclusionAtlas;
            public Texture2D EmissionAtlas;
        }

        public struct TextureUnit
        {
            public SkinnedMeshRenderer SourceRenderer;
            public int SourceMaterialIndex;
            public Texture2D BaseMap;
            public Texture2D NormalMap;
            public Texture2D MetallicMap;
            public Texture2D OcclusionMap;
            public Texture2D EmissionMap;
            public Rect AtlasUV;

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
        /// 生成纹理图集并应用
        /// </summary>
        /// <param name="dressupItem">目标外观部位</param>
        /// <param name="atlasSize">图集尺寸</param>
        /// <param name="baseMaterial">基础材质</param>
        /// <returns>使用图集的材质</returns>
        public async UniTask<Material> BakeAtlasAndApplyAsync(DressupItem dressupItem, int atlasSize, Material baseMaterial)
        {
            DressupItem[] itemArray = new DressupItem[] { dressupItem };
            return await BakeAtlasAndApplyAsync(itemArray, atlasSize, baseMaterial);
        }

        /// <summary>
        /// 生成纹理图集并应用
        /// </summary>
        /// <param name="dressupItems">目标外观部位</param>
        /// <param name="atlasSize">图集尺寸</param>
        /// <param name="baseMaterial">基础材质</param>
        /// <returns>使用图集的材质</returns>
        public async UniTask<Material> BakeAtlasAndApplyAsync(IReadOnlyList<DressupItem> dressupItems, int atlasSize, Material baseMaterial)
        {
            if (dressupItems == null || dressupItems.Count == 0)
            {
                Log.Warning("[MaterialCombiner] No dressup items to generate atlas.");
                return null;
            }

            // 提取所有纹理单元
            var textureUnits = ExtractTextureUnits(dressupItems);
            if (textureUnits.Length == 0)
            {
                Log.Warning("[MaterialCombiner] No valid texture units extracted.");
                return null;
            }

            Log.Debug($"[MaterialCombiner] Extracted {textureUnits.Length} texture units.");

            // 打包纹理为图集
            var atlasInfo = await PackTexturesAsync(textureUnits, atlasSize);
            if (!atlasInfo.IsValid)
            {
                Log.Error("[MaterialCombiner] Failed to pack textures to atlas.");
                return null;
            }

            // 创建使用图集的材质
            var atlasMaterial = BuildMaterialWithAtlas(atlasInfo, baseMaterial);

            // 重映射每个DressupItem的网格UV坐标
            RemapMeshUVs(textureUnits);

            // 应用材质到每个目标外观部位
            foreach (var item in dressupItems)
            {
                if (!item.IsValid) continue;

                var newMaterials = new Material[item.Renderer.sharedMaterials.Length];
                for (int i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = atlasMaterial;
                }
                item.Renderer.sharedMaterials = newMaterials;
            }

            Log.Debug("[MaterialCombiner] Successfully generated atlas and updated UV mappings.");
            return atlasMaterial;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 批量从外观物品中提取纹理单元
        /// </summary>
        /// <param name="dressupItems">外观物品列表</param>
        /// <returns>纹理单元数组</returns>
        private TextureUnit[] ExtractTextureUnits(IReadOnlyList<DressupItem> dressupItems)
        {
            var textureUnits = new List<TextureUnit>();

            foreach (var item in dressupItems)
            {
                if (item == null || !item.IsValid) continue;

                var materials = item.Renderer.sharedMaterials;

                if (materials == null || materials.Length == 0) continue;

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    var material = materials[materialIndex];
                    if (material == null) continue;

                    var unit = new TextureUnit
                    {
                        SourceRenderer = item.Renderer,
                        SourceMaterialIndex = materialIndex,
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
        /// 打包纹理为图集
        /// </summary>
        /// <param name="textureUnits">纹理单元数组</param>
        /// <param name="atlasSize">图集大小</param>
        /// <returns>纹理图集</returns>
        private async UniTask<AtlasInfo> PackTexturesAsync(TextureUnit[] textureUnits, int atlasSize)
        {
            var result = new AtlasInfo() { IsValid = false };

            if (textureUnits == null || textureUnits.Length == 0)
            {
                Log.Warning("[MaterialCombiner] No texture units to build material.");
                return result;
            }

            try
            {
                // 先打包所有BaseMap用于确定统一的UV布局
                var baseTextures = new Texture2D[textureUnits.Length];
                for (int i = 0; i < textureUnits.Length; i++)
                {
                    if (textureUnits[i].BaseMap != null)
                        baseTextures[i] = textureUnits[i].BaseMap;
                    else
                        baseTextures[i] = Texture2D.whiteTexture; // 使用白色纹理填充空位，确保布局正确
                }

                var (baseAtlas, atlasUVs) = PackBaseTextures(baseTextures, atlasSize);

                // 统一使用BaseMap的UV布局
                for (int i = 0; i < textureUnits.Length; i++)
                {
                    textureUnits[i].AtlasUV = atlasUVs[i];
                }

                // 并行打包其他纹理
                var normalPackTask = PackOtherTexturesAsync(textureUnits, atlasSize, TextureType.Normal);
                var metallicPackTask = PackOtherTexturesAsync(textureUnits, atlasSize, TextureType.Metallic);
                var occlusionPackTask = PackOtherTexturesAsync(textureUnits, atlasSize, TextureType.Occlusion);
                var emissionPackTask = PackOtherTexturesAsync(textureUnits, atlasSize, TextureType.Emission);

                var (normalAtlas, metallicAtlas, occlusionAtlas, emissionAtlas) =
                    await UniTask.WhenAll(normalPackTask, metallicPackTask, occlusionPackTask, emissionPackTask);

                result.BaseAtlas = baseAtlas;
                result.NormalAtlas = normalAtlas;
                result.MetallicAtlas = metallicAtlas;
                result.OcclusionAtlas = occlusionAtlas;
                result.EmissionAtlas = emissionAtlas;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[MaterialCombiner] Failed to pack textures and build material with atlas: {ex.Message}");
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// 重映射网格的UV坐标以匹配图集
        /// </summary>
        /// <param name="textureUnits">纹理单元数组</param>
        private void RemapMeshUVs(TextureUnit[] textureUnits)
        {
            for (int itemIndex = 0; itemIndex < textureUnits.Length; itemIndex++)
            {
                var unit = textureUnits[itemIndex];
                var sourceRenderer = unit.SourceRenderer;

                var originalMesh = sourceRenderer.sharedMesh;
                if (originalMesh == null) continue;

                var newMesh = Object.Instantiate(originalMesh);
                newMesh.name = $"{originalMesh.name}_Atlas";

                var originalUVs = newMesh.uv;
                if (originalUVs == null || originalUVs.Length == 0)
                {
                    Log.Warning($"[MaterialCombiner] Mesh '{originalMesh.name}' has no UV coordinates.");
                    continue;
                }

                var atlasUV = unit.AtlasUV;
                var newUVs = new Vector2[originalUVs.Length];
                for (int i = 0; i < originalUVs.Length; i++)
                {
                    var originalUV = originalUVs[i];
                    // 将原始 UV 映射到图集中的对应区域
                    newUVs[i] = new Vector2(
                        atlasUV.x + originalUV.x * atlasUV.width,
                        atlasUV.y + originalUV.y * atlasUV.height
                    );
                }

                newMesh.uv = newUVs;

                sourceRenderer.sharedMesh = newMesh;
            }
        }

        /// <summary>
        /// 使用图集构建材质
        /// </summary>
        /// <param name="atlasInfo">图集信息</param>
        /// <param name="baseMaterial">基础材质</param>
        /// <returns>构建的材质</returns>
        private Material BuildMaterialWithAtlas(AtlasInfo atlasInfo, Material baseMaterial)
        {
            var resultMaterial = Object.Instantiate(baseMaterial);
            resultMaterial.name = "AtlasMaterial";
            ApplyTexturesToMaterial(resultMaterial, atlasInfo.BaseAtlas, atlasInfo.NormalAtlas, atlasInfo.MetallicAtlas, atlasInfo.OcclusionAtlas, atlasInfo.EmissionAtlas);
            return resultMaterial;
        }

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

        private (Texture2D atlas, Rect[] atlasUVs) PackBaseTextures(Texture2D[] textures, int atlasSize)
        {
            var atlas = new Texture2D(atlasSize, atlasSize, TEXTURE_FORMAT, false)
            {
                name = $"CombinedAtlas_{atlasSize}"
            };

            var atlasUVs = atlas.PackTextures(textures, 2, atlasSize);

            return (atlas, atlasUVs);
        }

        private async UniTask<Texture2D> PackOtherTexturesAsync(TextureUnit[] textureUnits, int atlasSize, TextureType textureType)
        {
            var atlas = new Texture2D(atlasSize, atlasSize, TEXTURE_FORMAT, false)
            {
                name = $"CombinedAtlas_{atlasSize}_{textureType}"
            };

            // 先填充默认颜色
            var backgroundColor = GetTextureDefaultColor(textureType);
            var pixels = new Color32[atlasSize * atlasSize];
            int processCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;

                processCount++;
                if (processCount >= PIXEL_PROCESS_COUNT_PER_FRAME)
                {
                    processCount = 0;
                    await UniTask.NextFrame();
                }
            }

            atlas.SetPixels32(pixels);

            // 将存在的贴图绘制到图集的对应位置
            for (int i = 0; i < textureUnits.Length; i++)
            {
                var unit = textureUnits[i];
                var sourceTexture = unit.GetTexture(textureType);

                if (sourceTexture == null) continue;

                await DrawTextureToAtlasAsync(sourceTexture, atlas, unit.AtlasUV);
            }

            atlas.Apply(true, false);

            return atlas;
        }

        private async UniTask DrawTextureToAtlasAsync(Texture2D texture, Texture2D atlas, Rect uvRect)
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
            int processCount = 0;
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

                    processCount++;
                    if (processCount >= PIXEL_PROCESS_COUNT_PER_FRAME)
                    {
                        processCount = 0;
                        await UniTask.NextFrame();
                    }
                }
            }
        }

        private void ApplyTexturesToMaterial(Material material, Texture2D baseAtlas, Texture2D normalAtlas, Texture2D metallicAtlas, Texture2D occlusionAtlas, Texture2D emissionAtlas)
        {
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

        #endregion

    }
}
