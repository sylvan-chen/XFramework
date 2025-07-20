using UnityEngine;
using System;
using XFramework.Utils;

namespace SimpleDressup
{
    /// <summary>
    /// 服装网格数据类 - 存储从 SkinnedMeshRenderer 提取的几何信息
    /// 这是预处理后的网格数据，运行时只需要做合并操作
    /// </summary>
    [CreateAssetMenu(fileName = "New Dressup Mesh", menuName = "SimpleDressup/Dressup Mesh")]
    public class DressupMesh : ScriptableObject
    {
        [Header("基础信息")]
        public string MeshName;
        public DressupSlotType SlotType;

        [Header("几何数据")]
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] UV;
        public int[] Triangles;

        [Header("蒙皮数据")]
        public BoneWeight[] BoneWeights;
        public Matrix4x4[] BindPoses;
        public Transform[] Bones;
        public Transform RootBone;

        [Header("材质关联")]
        public DressupMaterial[] Materials;

        [Header("子网格信息")]
        public SubMeshInfo[] SubMeshes;

        /// <summary>
        /// 获取顶点数量
        /// </summary>
        public int VertexCount => Vertices?.Length ?? 0;

        /// <summary>
        /// 获取三角形数量
        /// </summary>
        public int TriangleCount => Triangles?.Length / 3 ?? 0;

        /// <summary>
        /// 从 SkinnedMeshRenderer 提取数据
        /// </summary>
        public void ExtractFromRenderer(SkinnedMeshRenderer renderer, string rootBoneName = "Root")
        {
            if (renderer == null || renderer.sharedMesh == null)
            {
                Log.Error("DressupMesh: SkinnedMeshRenderer or Mesh is null");
                return;
            }

            Mesh mesh = renderer.sharedMesh;
            MeshName = mesh.name;

            // 提取几何数据
            Vertices = mesh.vertices;
            Normals = mesh.normals;
            UV = mesh.uv;
            Triangles = mesh.triangles;

            // 提取蒙皮数据
            BoneWeights = mesh.boneWeights;
            BindPoses = mesh.bindposes;
            Bones = renderer.bones;
            RootBone = renderer.rootBone;

            // 提取子网格信息
            int subMeshCount = mesh.subMeshCount;
            SubMeshes = new SubMeshInfo[subMeshCount];
            for (int i = 0; i < subMeshCount; i++)
            {
                var subMesh = mesh.GetSubMesh(i);
                SubMeshes[i] = new SubMeshInfo
                {
                    IndexStart = subMesh.indexStart,
                    IndexCount = subMesh.indexCount,
                    MaterialIndex = i
                };
            }

            Log.Debug($"DressupMesh: 成功提取网格数据 - 顶点数:{Vertices.Length}, 子网格数:{subMeshCount}");
        }

        /// <summary>
        /// 检查网格数据是否有效
        /// </summary>
        public bool IsValid()
        {
            return Vertices != null && Vertices.Length > 0 &&
                   Triangles != null && Triangles.Length > 0 &&
                   SubMeshes != null && SubMeshes.Length > 0;
        }

        /// <summary>
        /// 子网格信息 - 每个材质对应一个子网格
        /// </summary>
        [Serializable]
        public struct SubMeshInfo
        {
            [Tooltip("子网格的三角形索引起始位置")]
            public int IndexStart;

            [Tooltip("子网格的三角形索引数量")]
            public int IndexCount;

            [Tooltip("对应的材质索引")]
            public int MaterialIndex;
        }
    }
}
