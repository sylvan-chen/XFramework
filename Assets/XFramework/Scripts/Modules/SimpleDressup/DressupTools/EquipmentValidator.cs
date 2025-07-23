using UnityEngine;

namespace SimpleDressup.Tools
{
    /// <summary>
    /// 装备验证工具 - 用于检查角色和装备的兼容性
    /// </summary>
    public class EquipmentValidator : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer characterRenderer;
        [SerializeField] private SkinnedMeshRenderer equipmentRenderer;

        /// <summary>
        /// 验证装备是否符合换装要求
        /// </summary>
        [ContextMenu("Validate Equipment")]
        public void ValidateEquipment()
        {
            if (characterRenderer == null || equipmentRenderer == null)
            {
                Debug.LogError("[DressupValidator] 请设置角色和装备的 SkinnedMeshRenderer");
                return;
            }

            Debug.Log("[DressupValidator] === 模型兼容性检查 ===");

            // 1. 检查根骨骼
            ValidateRootBone();

            // 2. 检查骨骼映射
            ValidateBoneMapping();

            // 3. 检查网格完整性
            ValidateMeshData();

            // 4. 检查材质设置
            ValidateMaterials();

            Debug.Log("[DressupValidator] === 检查完成 ===");
        }

        private void ValidateRootBone()
        {
            var charRootBone = characterRenderer.rootBone;
            var equipRootBone = equipmentRenderer.rootBone;

            if (charRootBone == null || equipRootBone == null)
            {
                Debug.LogWarning("[DressupValidator] ⚠️ 根骨骼未设置");
                return;
            }

            if (charRootBone.name != equipRootBone.name)
            {
                Debug.LogError($"[DressupValidator] ❌ 根骨骼名称不匹配: 角色='{charRootBone.name}', 装备='{equipRootBone.name}'");
            }
            else
            {
                Debug.Log($"[DressupValidator] ✅ 根骨骼匹配: {charRootBone.name}");
            }
        }

        private void ValidateBoneMapping()
        {
            var charBones = characterRenderer.bones;
            var equipBones = equipmentRenderer.bones;

            if (charBones == null || equipBones == null)
            {
                Debug.LogError("[DressupValidator] ❌ 骨骼数组为空");
                return;
            }

            // 构建角色骨骼名称映射
            var charBoneNames = new System.Collections.Generic.HashSet<string>();
            foreach (var bone in charBones)
            {
                if (bone != null)
                    charBoneNames.Add(bone.name);
            }

            // 检查装备骨骼是否都能在角色中找到对应
            int matchedBones = 0;
            int unmatchedBones = 0;

            foreach (var bone in equipBones)
            {
                if (bone == null) continue;

                if (charBoneNames.Contains(bone.name))
                {
                    matchedBones++;
                }
                else
                {
                    unmatchedBones++;
                    Debug.LogWarning($"[DressupValidator] ⚠️ 装备骨骼 '{bone.name}' 在角色中未找到对应");
                }
            }

            Debug.Log($"[DressupValidator] ✅ 骨骼映射: {matchedBones} 个匹配, {unmatchedBones} 个未匹配");

            if (unmatchedBones > 0)
            {
                Debug.LogWarning("[DressupValidator] ⚠️ 存在未匹配骨骼，可能影响换装效果");
            }
        }

        private void ValidateMeshData()
        {
            var charMesh = characterRenderer.sharedMesh;
            var equipMesh = equipmentRenderer.sharedMesh;

            if (charMesh == null || equipMesh == null)
            {
                Debug.LogError("[DressupValidator] ❌ 网格数据缺失");
                return;
            }

            // 检查必要的顶点属性
            bool charHasNormals = charMesh.normals.Length > 0;
            bool charHasUV = charMesh.uv.Length > 0;
            bool charHasBoneWeights = charMesh.boneWeights.Length > 0;

            bool equipHasNormals = equipMesh.normals.Length > 0;
            bool equipHasUV = equipMesh.uv.Length > 0;
            bool equipHasBoneWeights = equipMesh.boneWeights.Length > 0;

            Debug.Log($"[DressupValidator] 角色网格属性: 法线={charHasNormals}, UV={charHasUV}, 骨骼权重={charHasBoneWeights}");
            Debug.Log($"[DressupValidator] 装备网格属性: 法线={equipHasNormals}, UV={equipHasUV}, 骨骼权重={equipHasBoneWeights}");

            if (!equipHasBoneWeights)
            {
                Debug.LogError("[DressupValidator] ❌ 装备缺少骨骼权重数据，无法进行蒙皮");
            }

            if (!equipHasUV)
            {
                Debug.LogWarning("[DressupValidator] ⚠️ 装备缺少UV坐标，纹理合并可能失败");
            }
        }

        private void ValidateMaterials()
        {
            var equipMaterials = equipmentRenderer.sharedMaterials;

            if (equipMaterials == null || equipMaterials.Length == 0)
            {
                Debug.LogError("[DressupValidator] ❌ 装备没有材质");
                return;
            }

            for (int i = 0; i < equipMaterials.Length; i++)
            {
                var material = equipMaterials[i];
                if (material == null)
                {
                    Debug.LogWarning($"[DressupValidator] ⚠️ 材质槽 {i} 为空");
                    continue;
                }

                // 检查常见纹理属性
                bool hasMainTex = material.HasProperty("_MainTex") || material.HasProperty("_BaseMap");
                bool hasNormalMap = material.HasProperty("_BumpMap");

                Debug.Log($"[DressupValidator] 材质 {i} '{material.name}': 主纹理={hasMainTex}, 法线贴图={hasNormalMap}");
            }
        }
    }
}