using UnityEngine;
using System.Collections.Generic;
using System;
using XFramework.Utils;

namespace SimpleDressup
{
    /// <summary>
    /// 服装槽位数据类 - 代表一个完整的服装部件
    /// 组合了网格数据、材质信息和纹理片段
    /// </summary>
    [CreateAssetMenu(fileName = "New Dressup Slot", menuName = "SimpleDressup/Dressup Slot")]
    public class DressupSlot : ScriptableObject
    {
        [Header("基础信息")]
        public string SlotName;
        public DressupSlotType SlotType;

        [Header("网格和材质")]
        public DressupMesh Mesh;
        public DressupMaterial[] Materials;

        [Header("纹理片段列表")]
        public List<TextureFragment> TextureFragments = new List<TextureFragment>();

        [Header("渲染设置")]
        public float OverlayScale = 1.0f;  // 覆盖层缩放

        /// <summary>
        /// 获取槽位的总像素数 - 用于装箱算法
        /// </summary>
        public int TotalPixelCount
        {
            get
            {
                int total = 0;
                foreach (var fragment in TextureFragments)
                {
                    total += fragment.PixelCount;
                }
                return total;
            }
        }

        /// <summary>
        /// 从 GameObject 初始化槽位数据
        /// </summary>
        public void InitializeFromGameObject(GameObject gameObject, string rootBoneName = "Root")
        {
            if (gameObject == null)
            {
                Log.Error("DressSlot: GameObject is null");
                return;
            }

            var renderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                Log.Error($"DressSlot: No SkinnedMeshRenderer found in {gameObject.name}");
                return;
            }

            SlotName = gameObject.name;

            // 创建并提取网格数据
            Mesh = CreateInstance<DressupMesh>();
            Mesh.ExtractFromRenderer(renderer, rootBoneName);

            // 创建材质数组
            var sourceMaterials = renderer.sharedMaterials;
            Materials = new DressupMaterial[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                if (sourceMaterials[i] != null)
                {
                    Materials[i] = CreateDressupMaterial(sourceMaterials[i], i);
                }
            }

            // 创建纹理片段
            GenerateTextureFragments();

            Log.Debug($"DressSlot: 成功初始化槽位 {SlotName} - 材质数:{Materials.Length}, 纹理片段数:{TextureFragments.Count}");
        }

        /// <summary>
        /// 创建 DressupMaterial
        /// </summary>
        private DressupMaterial CreateDressupMaterial(Material sourceMaterial, int index)
        {
            var dressupMaterial = CreateInstance<DressupMaterial>();
            dressupMaterial.name = $"{SlotName}_Material_{index}";
            dressupMaterial.SourceMaterial = sourceMaterial;

            // 自动检测常用的纹理通道
            var channels = new List<DressupMaterial.TextureChannel>();

            // 检测基础贴图
            if (sourceMaterial.HasProperty("_BaseMap") || sourceMaterial.HasProperty("_MainTex"))
            {
                string propName = sourceMaterial.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                channels.Add(new DressupMaterial.TextureChannel
                {
                    PropertyName = propName,
                    EnableAtlas = true,
                    TextureType = DressupMaterial.TextureType.Diffuse
                });
            }

            // 检测法线贴图
            if (sourceMaterial.HasProperty("_BumpMap"))
            {
                channels.Add(new DressupMaterial.TextureChannel
                {
                    PropertyName = "_BumpMap",
                    EnableAtlas = true,
                    TextureType = DressupMaterial.TextureType.Normal
                });
            }

            dressupMaterial.TextureChannels = channels.ToArray();
            return dressupMaterial;
        }

        /// <summary>
        /// 生成纹理片段
        /// </summary>
        private void GenerateTextureFragments()
        {
            TextureFragments.Clear();

            for (int i = 0; i < Materials.Length; i++)
            {
                var material = Materials[i];
                if (material != null && material.IsValid())
                {
                    var fragment = new TextureFragment($"{SlotName}_Fragment_{i}", SlotType);
                    fragment.ExtractFromMaterial(material);

                    if (fragment.IsValid())
                    {
                        TextureFragments.Add(fragment);
                    }
                }
            }
        }

        /// <summary>
        /// 检查槽位是否有效
        /// </summary>
        public bool IsValid()
        {
            return Mesh != null && Mesh.IsValid() &&
                   Materials != null && Materials.Length > 0 &&
                   TextureFragments.Count > 0;
        }

        /// <summary>
        /// 获取指定材质的纹理片段
        /// </summary>
        public TextureFragment GetTextureFragment(int materialIndex)
        {
            if (materialIndex < 0 || materialIndex >= TextureFragments.Count)
                return null;

            return TextureFragments[materialIndex];
        }
    }
}
