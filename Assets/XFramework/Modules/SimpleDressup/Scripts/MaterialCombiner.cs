using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.SimpleDressup
{
    /// <summary>
    /// 材质合并器
    /// </summary>
    public class MaterialCombiner
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

        private struct AtlasInfo
        {
            public bool IsValid;
            public Texture2D BaseAtlas;
            public Texture2D NormalAtlas;
            public Texture2D MetallicAtlas;
            public Texture2D OcclusionAtlas;
            public Texture2D EmissionAtlas;
        }

        #endregion

        #region 公开接口

        /// <summary>
        /// 从外观物品中提取材质数据
        /// </summary>
        /// <remarks>
        /// 只提取有效的材质（多于子网格的材质略过）
        /// </remarks>
        /// <param name="outlookItems">外观物品列表</param>
        /// <returns>材质数据数组</returns>
        public DressupMaterialData[] ExtractMaterialData(IReadOnlyList<DressupOutlookItem> outlookItems)
        {
            var materialDatas = new List<DressupMaterialData>();

            foreach (var item in outlookItems)
            {
                var materials = item.Renderer.sharedMaterials;
                int submeshCount = item.Renderer.sharedMesh.subMeshCount;
                if (materials.Length < submeshCount)
                {
                    Log.Warning($"[MaterialCombiner] Outlook item ({item.OutlookType}) has fewer materials than submeshes. " +
                                $"Materials: {materials.Length}, Submeshes: {submeshCount}");
                }

                // 只提取有效的材质（多于子网格的材质略过）
                for (int materialIndex = 0; materialIndex < materials.Length && materialIndex < submeshCount; materialIndex++)
                {
                    var material = materials[materialIndex];
                    if (material == null) continue;

                    var data = new DressupMaterialData
                    {
                        Name = material.name,
                        BaseMap = ExtractTexture(material, TextureType.Base),
                        NormalMap = ExtractTexture(material, TextureType.Normal),
                        MetallicMap = ExtractTexture(material, TextureType.Metallic),
                        OcclusionMap = ExtractTexture(material, TextureType.Occlusion),
                        EmissionMap = ExtractTexture(material, TextureType.Emission)
                    };

                    materialDatas.Add(data);
                }
            }

            return materialDatas.ToArray();
        }

        /// <summary>
        /// 合并材质
        /// </summary>
        /// <param name="combineUnits">合并单元列表</param>
        /// <param name="atlasSize">图集大小</param>
        /// <param name="baseMaterial">基础材质</param>
        /// <returns>纹理图集</returns>
        public async UniTask<Material> CombineMaterialsAsync(DressupCombineUnit[] combineUnits, int atlasSize, Material baseMaterial)
        {
            try
            {
                // 打包基础纹理图集
                var baseAtlas = GenerateBaseAtlas(combineUnits, atlasSize);
                if (baseAtlas == null)
                {
                    Log.Error("[MaterialCombiner] Failed to combine material.");
                    return null;
                }

                // 并行打包其他纹理图集
                var normalPackTask = GenerateOtherAtlasAsync(combineUnits, atlasSize, TextureType.Normal);
                var metallicPackTask = GenerateOtherAtlasAsync(combineUnits, atlasSize, TextureType.Metallic);
                var occlusionPackTask = GenerateOtherAtlasAsync(combineUnits, atlasSize, TextureType.Occlusion);
                var emissionPackTask = GenerateOtherAtlasAsync(combineUnits, atlasSize, TextureType.Emission);

                var (normalAtlas, metallicAtlas, occlusionAtlas, emissionAtlas) =
                    await UniTask.WhenAll(normalPackTask, metallicPackTask, occlusionPackTask, emissionPackTask);

                // 重映射 UV
                ApplyAtlasRectRemapping(combineUnits);

                // 构建图集材质
                var atlasInfo = new AtlasInfo
                {
                    IsValid = false,
                    BaseAtlas = baseAtlas,
                    NormalAtlas = normalAtlas,
                    MetallicAtlas = metallicAtlas,
                    OcclusionAtlas = occlusionAtlas,
                    EmissionAtlas = emissionAtlas
                };

                var combinedMaterial = BuildAtlasMaterialFromBaseMaterial(atlasInfo, baseMaterial);

                Log.Debug($"[MaterialCombiner] Successfully combine material from {combineUnits.Length} materials.");
                return combinedMaterial;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[MaterialCombiner] Failed to combined material: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 应用图集UV重映射
        /// </summary>
        /// <param name="combineUnits">合并单元列表</param>
        public void ApplyAtlasRectRemapping(DressupCombineUnit[] combineUnits)
        {
            for (int i = 0; i < combineUnits.Length; i++)
            {
                var materialData = combineUnits[i].MaterialData;
                var subMeshData = combineUnits[i].SubmeshData;

                var originalUVs = subMeshData.UVs;
                if (originalUVs == null || originalUVs.Length == 0)
                {
                    Log.Warning($"[MaterialCombiner] SubmeshData in CombineUnit {i} has NO uv to remap.");
                    continue;
                }

                var targetRect = materialData.AtlasRect;
                var targetUVs = new Vector2[originalUVs.Length];

                for (int uvIndex = 0; uvIndex < originalUVs.Length; uvIndex++)
                {
                    var originalUV = originalUVs[uvIndex];
                    // 将原始 UV 映射到图集中的对应区域
                    targetUVs[uvIndex] = new Vector2(
                        targetRect.x + originalUV.x * targetRect.width,
                        targetRect.y + originalUV.y * targetRect.height
                    );
                }

                combineUnits[i].SubmeshData.UVs = targetUVs;
            }
        }

        #endregion

        #region 内部实现

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

        private Texture2D GenerateBaseAtlas(DressupCombineUnit[] combineUnits, int atlasSize)
        {
            var baseTextures = new Texture2D[combineUnits.Length];
            for (int i = 0; i < combineUnits.Length; i++)
            {
                var data = combineUnits[i].MaterialData;

                if (data == null || data.BaseMap == null)
                {
                    Log.Error($"[MaterialCombiner] Material data [{i}] has no BaseMap.");
                    return null;
                }

                baseTextures[i] = data.BaseMap;
            }

            var atlas = new Texture2D(atlasSize, atlasSize, TEXTURE_FORMAT, false)
            {
                name = $"CombinedAtlas_{atlasSize}"
            };

            var atlasRects = atlas.PackTextures(baseTextures, 2, atlasSize);

            // 保存图集坐标
            for (int i = 0; i < combineUnits.Length; i++)
            {
                combineUnits[i].MaterialData.AtlasRect = atlasRects[i];
            }

            return atlas;
        }

        private async UniTask<Texture2D> GenerateOtherAtlasAsync(DressupCombineUnit[] combineUnits, int atlasSize, TextureType textureType)
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
            for (int i = 0; i < combineUnits.Length; i++)
            {
                var data = combineUnits[i].MaterialData;
                var texture = data.GetTexture(textureType);

                if (texture == null) continue;

                await BlitTextureToAtlasAsync(texture, atlas, data.AtlasRect);
            }

            atlas.Apply(true, false);

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

        private async UniTask BlitTextureToAtlasAsync(Texture2D texture, Texture2D atlas, Rect uvRect)
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

        private Material BuildAtlasMaterialFromBaseMaterial(AtlasInfo atlasInfo, Material baseMaterial)
        {
            if (baseMaterial == null)
            {
                Log.Error("[MaterialCombiner] Base material is null, cannot build new material.");
                return null;
            }

            var resultMaterial = Object.Instantiate(baseMaterial);
            resultMaterial.name = $"{baseMaterial.name}_Atlas";
#if UNITY_EDITOR
            resultMaterial.hideFlags = HideFlags.DontSave;
#else
            resultMaterial.hideFlags = HideFlags.HideAndDontSave;
#endif

            ApplyTexturesToMaterial(resultMaterial, atlasInfo.BaseAtlas, atlasInfo.NormalAtlas, atlasInfo.MetallicAtlas, atlasInfo.OcclusionAtlas, atlasInfo.EmissionAtlas);

            return resultMaterial;
        }

        private Material BuildAtlasMaterialWithShader(AtlasInfo atlasInfo, Shader shader)
        {
            if (shader == null)
            {
                Log.Error("[MaterialCombiner] Shader is null, cannot build new material.");
                return null;
            }

            var resultMaterial = new Material(shader)
            {
                name = $"{shader.name}_Atlas",
#if UNITY_EDITOR
                hideFlags = HideFlags.DontSave,
#else
                hideFlags = HideFlags.HideAndDontSave,
#endif
            };

            ApplyTexturesToMaterial(resultMaterial, atlasInfo.BaseAtlas, atlasInfo.NormalAtlas, atlasInfo.MetallicAtlas, atlasInfo.OcclusionAtlas, atlasInfo.EmissionAtlas);

            return resultMaterial;
        }

        private void ApplyTexturesToMaterial(Material material, Texture2D baseAtlas, Texture2D normalAtlas, Texture2D metallicAtlas, Texture2D occlusionAtlas, Texture2D emissionAtlas)
        {
            if (baseAtlas != null)
                SetTextureProperty(material, baseAtlas, "_BaseMap", "_MainTex", "_BaseColorMap", "_Albedo", "_Diffuse");

            if (normalAtlas != null)
                SetTextureProperty(material, normalAtlas, "_BumpMap", "_NormalMap", "_DetailNormalMap");

            if (metallicAtlas != null)
                SetTextureProperty(material, metallicAtlas, "_MetallicGlossMap", "_MetallicMap", "_SpecGlossMap");

            if (occlusionAtlas != null)
                SetTextureProperty(material, occlusionAtlas, "_OcclusionMap", "_AOMap", "_AmbientOcclusionMap");

            if (emissionAtlas != null)
                SetTextureProperty(material, emissionAtlas, "_EmissionMap", "_EmissiveMap", "_EmissionColorMap");
        }

        private void SetTextureProperty(Material material, Texture texture, params string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length - 1; i++)
            {
                if (material.HasTexture(propertyNames[i]))
                {
                    material.SetTexture(propertyNames[i], texture);
                    return;
                }
            }
        }

        #endregion
    }
}
