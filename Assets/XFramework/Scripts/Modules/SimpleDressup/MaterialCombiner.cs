using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.SimpleDressup
{
    public class MaterialCombiner
    {
        #region 数据结构

        private enum TextureType
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

        private class TextureUnit
        {
            public Texture2D BaseMap;
            public Texture2D NormalMap;
            public Texture2D MetallicMap;
            public Texture2D OcclusionMap;
            public Texture2D EmissionMap;
            public Rect UvRect;
        }

        #endregion

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

        private List<TextureUnit> CollectTextureUnits(List<DressupItem> dressupItems)
        {
            var textureUnits = new List<TextureUnit>();

            foreach (var item in dressupItems)
            {
                if (item == null || item.Materials == null || item.Materials.Length == 0)
                {
                    // Log.Warning($"[MaterialCombiner] Item '{item.Name}' has no materials.");
                    continue;
                }

                foreach (var material in item.Materials)
                {
                    if (material == null) continue;

                    var unit = new TextureUnit
                    {
                        BaseMap = material.GetTexture("_MainTex") as Texture2D,
                        NormalMap = material.GetTexture("_BumpMap") as Texture2D,
                        MetallicMap = material.GetTexture("_MetallicGlossMap") as Texture2D,
                        OcclusionMap = material.GetTexture("_OcclusionMap") as Texture2D,
                        EmissionMap = material.GetTexture("_EmissionMap") as Texture2D
                    };

                    // 计算UV矩形
                    // unit.UvRect = CalculateUvRect(unit);

                    textureUnits.Add(unit);
                }
            }

            return textureUnits;
        }
    }
}
