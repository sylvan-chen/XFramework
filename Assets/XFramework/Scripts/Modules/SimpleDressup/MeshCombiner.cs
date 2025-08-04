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

        private const int TRIANGLE_PROCESS_COUNT_PER_FRAME = 1000; // 三角形每帧处理数量

        #endregion

        #region 数据结构

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
            public int[][] SubmeshToTriangles;
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 提取外观部件的子网格单元
        /// </summary>
        /// <param name="dressupItem">外观部件</param>
        /// <returns>子网格单元列表</returns>
        public async UniTask<SubmeshUnit[]> ExtractSubmeshUnitsAsync(DressupItem dressupItem)
        {
            return await ExtractSubmeshUnitsInternal(dressupItem);
        }

        /// <summary>
        /// 批量提取外观部件的子网格单元
        /// </summary>
        /// <param name="dressupItems">外观部件列表</param>
        /// <returns>子网格单元列表</returns>
        public async UniTask<SubmeshUnit[]> ExtractSubmeshUnitsAsync(IReadOnlyList<DressupItem> dressupItems)
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

                var submeshUnits = await ExtractSubmeshUnitsInternal(item);
                if (submeshUnits.Length == 0) continue;

                Array.Copy(submeshUnits, 0, allSubmeshUnits, lastTargetIndex, submeshUnits.Length);
                lastTargetIndex += submeshUnits.Length;
            }

            return allSubmeshUnits;
        }

        /// <summary>
        /// 合并多个子网格单元
        /// </summary>
        /// <param name="submeshUnits">待合并的子网格单元列表</param>
        /// <returns>合并后的子网格单元</returns>
        public async UniTask<SubmeshUnit> CombineSubmeshUnitsAsync(IReadOnlyList<SubmeshUnit> submeshUnits)
        {
            var combinedInfo = await GenerateCombinedMeshInfoAsync(submeshUnits);
            if (!combinedInfo.Success)
            {
                Log.Error("[MeshCombiner] Failed to generate combined mesh info.");
                return default;
            }

            return SubmeshUnit.Create(combinedInfo.Vertices, combinedInfo.Normals, combinedInfo.Tangents, combinedInfo.UVs,
                combinedInfo.BoneWeights, combinedInfo.Triangles);
        }

        /// <summary>
        /// 将多个子网格单元建立成真正的网格
        /// </summary>
        /// <param name="submeshUnits">子网格单元列表</param>
        /// <param name="bindPoses">绑定姿势矩阵数组</param>
        /// <returns>合并后的网格</returns>
        public async UniTask<Mesh> BuildMeshAsync(IReadOnlyList<SubmeshUnit> submeshUnits, Matrix4x4[] bindPoses)
        {
            var combinedInfo = await GenerateCombinedMeshInfoAsync(submeshUnits);
            if (!combinedInfo.Success)
            {
                Log.Error("[MeshCombiner] Failed to generate combined mesh info.");
                return null;
            }

            Log.Info($"[XFramework] [MeshCombiner] Successfully built mesh with {submeshUnits.Count} submeshes, " +
                     $"{combinedInfo.Vertices.Length} vertices, {combinedInfo.SubmeshToTriangles.Sum(t => t.Length)} triangles");

            return BuildMeshInternal(combinedInfo.SubmeshToTriangles, combinedInfo.Vertices, combinedInfo.Normals,
                combinedInfo.Tangents, combinedInfo.UVs, combinedInfo.BoneWeights, bindPoses);
        }

        #endregion

        #region 核心实现

        private async UniTask<SubmeshUnit[]> ExtractSubmeshUnitsInternal(DressupItem dressupItem)
        {
            if (!dressupItem.IsValid) return Array.Empty<SubmeshUnit>();

            var mesh = dressupItem.Mesh;
            int submeshCount = dressupItem.SubmeshCount;

            var submeshUnits = new SubmeshUnit[submeshCount];
            int processedCount = 0;

            // 为每个子网格创建一个子网格单元
            for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
            {
                var unit = SubmeshUnit.Create(mesh, submeshIndex);
                if (unit.IsValid)
                {
                    submeshUnits[submeshIndex] = unit;
                }

                processedCount++;
                if (processedCount >= TRIANGLE_PROCESS_COUNT_PER_FRAME)
                {
                    processedCount = 0;
                    await UniTask.NextFrame();
                }
            }
            return submeshUnits;
        }

        private async UniTask<CombinedMeshInfo> GenerateCombinedMeshInfoAsync(IReadOnlyList<SubmeshUnit> submeshUnits)
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

            result.Vertices = new Vector3[totalVertexCount];
            result.Normals = new Vector3[totalVertexCount];
            result.Tangents = new Vector4[totalVertexCount];
            result.UVs = new Vector2[totalVertexCount];
            result.BoneWeights = new BoneWeight[totalVertexCount];
            result.Triangles = new int[totalTriangleCount];
            result.SubmeshToTriangles = new int[submeshUnits.Count][];

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

                // 三角形索引重映射到新的顶点索引
                var unitTriangles = unit.Triangles;
                result.SubmeshToTriangles[unitIndex] = new int[triangleIndexCount];
                for (int i = 0; i < triangleIndexCount; i++)
                {
                    result.Triangles[triangleOffset + i] = unitTriangles[i] + vertexOffset;
                    result.SubmeshToTriangles[unitIndex][i] = unitTriangles[i] + vertexOffset;

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

        private Mesh BuildMeshInternal(int[][] submeshToTriangles, Vector3[] vertices, Vector3[] normals, Vector4[] tangents,
            Vector2[] uvs, BoneWeight[] boneWeights, Matrix4x4[] bindPoses)
        {
            try
            {
                var mesh = new Mesh
                {
                    name = "CombinedMesh",
                    hideFlags = HideFlags.HideAndDontSave
                };

                // 设置顶点数据 
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTangents(tangents);
                mesh.SetUVs(0, uvs);

                mesh.subMeshCount = submeshToTriangles.Length;
                for (int submeshIndex = 0; submeshIndex < submeshToTriangles.Length; submeshIndex++)
                {
                    mesh.SetTriangles(submeshToTriangles[submeshIndex], submeshIndex);
                }

                mesh.boneWeights = boneWeights;
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

        #endregion
    }
}
