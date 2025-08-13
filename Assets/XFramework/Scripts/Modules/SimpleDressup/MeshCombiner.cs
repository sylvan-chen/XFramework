using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.SimpleDressup
{
    /// <summary>
    /// 网格合并器
    /// </summary>
    /// <remarks>
    /// 核心功能：
    /// 1. 拆分、合并外观部件的所有子网格单元（SubmeshUnit）
    /// 2. 将一个或多个子网格单元合并成一个完整的网格
    /// 3. 仅支持异步处理，避免主线程阻塞
    /// </remarks>
    public partial class MeshCombiner
    {
        #region 常量配置

        /// <summary>
        /// 每帧处理的三角形数量
        /// </summary>
        private const int TRIANGLE_PROCESS_COUNT_PER_FRAME = 1000;

        #endregion

        #region 数据结构/枚举

        public enum SubMeshStrategy
        {
            /// <summary>
            /// 合并所有子网格
            /// </summary>
            MergeAll,

            /// <summary>
            /// 保持所有子网格独立
            /// </summary>
            Independent,
        }

        /// <summary>
        /// 合并网格信息
        /// </summary>
        private struct CombinedMeshInfo
        {
            public bool Success;
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public Vector4[] Tangents;
            public Vector2[] UVs;
            public BoneWeight[] BoneWeights;
            public int[] Triangles;
            public List<int>[] SubmeshToTriangles;
        }

        #endregion

        #region 公共接口

        public async UniTask<Mesh> CombineMeshesAsync(IReadOnlyList<DressupItem> dressupItems, Matrix4x4[] bindPoses, SubMeshStrategy strategy = SubMeshStrategy.MergeAll)
        {
            var submeshUnits = await ExtractSubmeshUnitsAsync(dressupItems);

            return await BuildCombinedMeshAsync(submeshUnits, bindPoses, strategy);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 提取外观部件的子网格单元
        /// </summary>
        /// <param name="dressupItem">外观部件</param>
        /// <returns>子网格单元列表</returns>
        private async UniTask<SubmeshUnit[]> ExtractSubmeshUnitsAsync(DressupItem dressupItem)
        {
            if (!dressupItem.IsValid) return Array.Empty<SubmeshUnit>();

            var mesh = dressupItem.Renderer.sharedMesh;
            int submeshCount = dressupItem.SubmeshCount;

            var resultUnits = new SubmeshUnit[submeshCount];
            int processedCount = 0;

            // 为每个子网格创建一个子网格单元
            for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
            {
                var unit = SubmeshUnit.Create(mesh, submeshIndex);
                if (unit.IsValid)
                {
                    if (submeshIndex < dressupItem.Renderer.sharedMaterials.Length)
                        unit.SubmeshMaterial = dressupItem.Renderer.sharedMaterials[submeshIndex];
                    else
                        Debug.LogError($"[MeshCombiner] Missing material for submesh {submeshIndex}.");

                    resultUnits[submeshIndex] = unit;
                }

                processedCount++;
                if (processedCount >= TRIANGLE_PROCESS_COUNT_PER_FRAME)
                {
                    processedCount = 0;
                    await UniTask.NextFrame();
                }
            }
            return resultUnits;
        }

        /// <summary>
        /// 批量提取外观部件的子网格单元
        /// </summary>
        /// <param name="dressupItems">外观部件列表</param>
        /// <returns>子网格单元列表</returns>
        private async UniTask<SubmeshUnit[]> ExtractSubmeshUnitsAsync(IReadOnlyList<DressupItem> dressupItems)
        {
            int submeshCount = 0;
            for (int i = 0; i < dressupItems.Count; i++)
            {
                if (dressupItems[i].IsValid)
                    submeshCount += dressupItems[i].SubmeshCount;
            }

            var allSubmeshUnits = new SubmeshUnit[submeshCount];
            int lastTargetIndex = 0;

            for (int i = 0; i < dressupItems.Count; i++)
            {
                var item = dressupItems[i];

                var submeshUnits = await ExtractSubmeshUnitsAsync(item);
                if (submeshUnits.Length == 0) continue;

                Array.Copy(submeshUnits, 0, allSubmeshUnits, lastTargetIndex, submeshUnits.Length);
                lastTargetIndex += submeshUnits.Length;
            }

            return allSubmeshUnits;
        }

        /// <summary>
        /// 根据子网格单元列表建立合并网格
        /// </summary>
        /// <param name="submeshUnits">子网格单元列表</param>
        /// <param name="bindPoses">绑定姿势矩阵数组</param>
        /// <returns>合并网格</returns>
        private async UniTask<Mesh> BuildCombinedMeshAsync(IReadOnlyList<SubmeshUnit> submeshUnits, Matrix4x4[] bindPoses, SubMeshStrategy strategy)
        {
            var combinedMeshInfo = await GenerateCombinedMeshInfoAsync(submeshUnits, strategy);
            if (!combinedMeshInfo.Success)
            {
                Log.Error("[MeshCombiner] Failed to generate combined mesh info.");
                return null;
            }

            Log.Info($"[XFramework] [MeshCombiner] Successfully built mesh with {submeshUnits.Count} submeshes, " +
                     $"{combinedMeshInfo.Vertices.Length} vertices, {combinedMeshInfo.Triangles.Length} triangle indices.");

            try
            {
                var mesh = new Mesh
                {
                    name = "CombinedMesh",
                    hideFlags = HideFlags.HideAndDontSave
                };

                // 设置顶点数据 
                mesh.SetVertices(combinedMeshInfo.Vertices);
                mesh.SetNormals(combinedMeshInfo.Normals);
                mesh.SetTangents(combinedMeshInfo.Tangents);
                mesh.SetUVs(0, combinedMeshInfo.UVs);

                var submeshToTriangles = combinedMeshInfo.SubmeshToTriangles;

                mesh.subMeshCount = submeshToTriangles.Length;
                for (int submeshIndex = 0; submeshIndex < submeshToTriangles.Length; submeshIndex++)
                {
                    mesh.SetTriangles(submeshToTriangles[submeshIndex], submeshIndex);
                }

                mesh.boneWeights = combinedMeshInfo.BoneWeights;
                mesh.bindposes = bindPoses;

                if (mesh.normals == null || mesh.normals.Length == 0)
                    mesh.RecalculateNormals();

                if (mesh.tangents == null || mesh.tangents.Length == 0)
                    mesh.RecalculateTangents();

                mesh.RecalculateBounds();

                return mesh;
            }
            catch (Exception e)
            {
                Log.Error($"[MeshCombiner] CreateFinalCombinedMesh error - {e.Message}");
                return null;
            }
        }

        private async UniTask<CombinedMeshInfo> GenerateCombinedMeshInfoAsync(IReadOnlyList<SubmeshUnit> submeshUnits, SubMeshStrategy strategy)
        {
            var result = new CombinedMeshInfo { Success = false };

            if (submeshUnits == null || submeshUnits.Count == 0)
            {
                Log.Warning("[MeshCombiner] No submesh units to build mesh.");
                return result;
            }

            int totalVertexCount = 0;
            int totalTriangleCount = 0;

            for (int i = 0; i < submeshUnits.Count; i++)
            {
                var unit = submeshUnits[i];
                if (unit.IsValid)
                {
                    totalVertexCount += unit.VertexCount;
                    totalTriangleCount += unit.TriangleIndexCount;
                }
            }

            int submeshCount = strategy switch
            {
                SubMeshStrategy.MergeAll => 1,
                SubMeshStrategy.Independent => submeshUnits.Count,
                _ => submeshUnits.Count
            };

            result.Vertices = new Vector3[totalVertexCount];
            result.Normals = new Vector3[totalVertexCount];
            result.Tangents = new Vector4[totalVertexCount];
            result.UVs = new Vector2[totalVertexCount];
            result.BoneWeights = new BoneWeight[totalVertexCount];
            result.Triangles = new int[totalTriangleCount];
            result.SubmeshToTriangles = new List<int>[submeshCount];

            for (int i = 0; i < submeshCount; i++)
            {
                result.SubmeshToTriangles[i] = new List<int>();
            }

            int vertexOffset = 0;
            int triangleOffset = 0;
            int processedCount = 0;

            for (int unitIndex = 0; unitIndex < submeshUnits.Count; unitIndex++)
            {
                var unit = submeshUnits[unitIndex];
                if (!unit.IsValid) continue;

                int vertexCount = unit.VertexCount;
                int triangleIndexCount = unit.TriangleIndexCount;

                Array.Copy(unit.Vertices, 0, result.Vertices, vertexOffset, vertexCount);
                Array.Copy(unit.Normals, 0, result.Normals, vertexOffset, vertexCount);
                Array.Copy(unit.Tangents, 0, result.Tangents, vertexOffset, vertexCount);
                Array.Copy(unit.Uvs, 0, result.UVs, vertexOffset, vertexCount);
                Array.Copy(unit.BoneWeights, 0, result.BoneWeights, vertexOffset, vertexCount);

                var targetSubmeshIndex = strategy switch
                {
                    SubMeshStrategy.MergeAll => 0,
                    SubMeshStrategy.Independent => unitIndex,
                    _ => unitIndex
                };

                // 三角形索引重映射到新的顶点索引
                var unitTriangles = unit.Triangles;
                for (int i = 0; i < triangleIndexCount; i++)
                {
                    result.Triangles[triangleOffset + i] = unitTriangles[i] + vertexOffset;
                    result.SubmeshToTriangles[targetSubmeshIndex].Add(unitTriangles[i] + vertexOffset);

                    processedCount++;
                    if (processedCount >= TRIANGLE_PROCESS_COUNT_PER_FRAME)
                    {
                        processedCount = 0;
                        await UniTask.NextFrame();
                    }
                }

                vertexOffset += vertexCount;
                triangleOffset += triangleIndexCount;
            }

            result.Success = true;
            return result;
        }

        #endregion
    }
}
