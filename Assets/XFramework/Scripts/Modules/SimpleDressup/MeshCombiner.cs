using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.SimpleDressup
{
    /// <summary>
    /// 网格合并器 - 基于材质的分组策略
    /// 1. 拆分所有实例的子网格（以材质-子网格为单位）
    /// 2. 合并相同材质的子网格
    /// 3. 按最终的子网格数量重建新的包含多个子网格的大网格
    /// </summary>
    public class MeshCombiner
    {
        #region 数据结构

        /// <summary>
        /// 子网格单元
        /// </summary>
        private class SubmeshUnit
        {
            public Material Material { get; private set; }
            public List<int> Triangles { get; private set; } = new();
            public List<Vector3> Vertices { get; private set; } = new();
            public List<Vector3> Normals { get; private set; } = new();
            public List<Vector4> Tangents { get; private set; } = new();
            public List<Vector2> Uvs { get; private set; } = new();
            public List<BoneWeight> BoneWeights { get; private set; } = new();

            public bool IsValid => Vertices.Count > 0 && Triangles.Count > 0;
            public int VertexCount => Vertices.Count;

            public static SubmeshUnit Create(Mesh sourceMesh, int submeshIndex, Material material)
            {
                if (sourceMesh == null) throw new ArgumentNullException(nameof(sourceMesh));

                var unit = new SubmeshUnit
                {
                    Material = material
                };

                // 检查源网格数据完整性
                bool hasNormals = sourceMesh.normals != null && sourceMesh.normals.Length == sourceMesh.vertexCount;
                bool hasTangents = sourceMesh.tangents != null && sourceMesh.tangents.Length == sourceMesh.vertexCount;
                bool hasUV = sourceMesh.uv != null && sourceMesh.uv.Length == sourceMesh.vertexCount;
                bool hasBoneWeights = sourceMesh.boneWeights != null && sourceMesh.boneWeights.Length == sourceMesh.vertexCount;

                if (!hasNormals)
                    Log.Debug($"[MeshCombiner] Mesh '{sourceMesh.name}' missing normals, will recalculate.");
                if (!hasTangents)
                    Log.Debug($"[MeshCombiner] Mesh '{sourceMesh.name}' missing tangents, will recalculate.");
                if (!hasUV)
                    Log.Warning($"[MeshCombiner] Mesh '{sourceMesh.name}' missing UV coordinates, using default values. This may affect texture rendering.");
                if (!hasBoneWeights)
                    Log.Warning($"[MeshCombiner] Mesh '{sourceMesh.name}' missing bone weights, using default weights. This may affect skinning.");

                // 提取使用的顶点
                var subTriangles = sourceMesh.GetTriangles(submeshIndex);
                var usedVertexIndices = new HashSet<int>(subTriangles);

                // 顶点重映射，原索引 -> 0, 1, 2, ...
                var vertexRemapping = new Dictionary<int, int>();
                int newIndex = 0;
                foreach (var oldIndex in usedVertexIndices)
                {
                    vertexRemapping[oldIndex] = newIndex++;

                    // 顶点位置
                    unit.Vertices.Add(sourceMesh.vertices[oldIndex]);

                    // 法线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasNormals)
                        unit.Normals.Add(sourceMesh.normals[oldIndex]);

                    // 切线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasTangents)
                        unit.Tangents.Add(sourceMesh.tangents[oldIndex]);

                    // UV坐标（无法自动计算，使用默认值，可能影响材质渲染效果）
                    if (hasUV)
                        unit.Uvs.Add(sourceMesh.uv[oldIndex]);
                    else
                        unit.Uvs.Add(Vector2.zero);

                    // 骨骼权重（无法自动计算，使用默认值，可能影响蒙皮效果）
                    if (hasBoneWeights)
                        unit.BoneWeights.Add(sourceMesh.boneWeights[oldIndex]);
                    else
                        unit.BoneWeights.Add(new BoneWeight { weight0 = 1.0f, boneIndex0 = 0 });
                }

                // 三角形索引重映射
                foreach (var oldIndex in subTriangles)
                {
                    unit.Triangles.Add(vertexRemapping[oldIndex]);
                }

                return unit;
            }
        }

        /// <summary>
        /// 合并结果
        /// </summary>
        public struct MeshCombineResult
        {
            public bool Success;
            public Mesh CombinedMesh;
            public Material[] CombinedMaterials;
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 合并所有换装部件为一个大网格
        /// </summary>
        /// <param name="items">换装部件列表</param>
        /// <param name="bindPoses">绑定姿势矩阵数组</param>
        public async UniTask<MeshCombineResult> CombineMeshesAsync(List<DressupItem> items, IReadOnlyList<Matrix4x4> bindPoses)
        {
            if (items == null || items.Count == 0)
            {
                Log.Error("[MeshCombiner] No items to combine");
                return new MeshCombineResult { Success = false };
            }

            var submeshUnits = ExtractSubmeshUnits(items);
            var mergedSubmeshUnits = await MergeSubmeshUnitsByMaterialAsync(submeshUnits);
            var result = BuildFinalMesh(mergedSubmeshUnits, bindPoses);

            return result;
        }

        #endregion

        #region 核心实现

        /// <summary>
        /// 提取拆分所有实例的子网格
        /// </summary>
        private List<SubmeshUnit> ExtractSubmeshUnits(List<DressupItem> items)
        {
            var submeshUnits = new List<SubmeshUnit>();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (!item.IsValid) continue;

                var mesh = item.Mesh;
                var materials = item.Materials;

                // 为每个子网格创建一个子网格单元
                for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
                {
                    // 多余的材质直接忽略
                    if (submeshIndex >= materials.Length) break;

                    var material = materials[submeshIndex];

                    var unit = SubmeshUnit.Create(mesh, submeshIndex, material);
                    if (unit.IsValid)
                    {
                        submeshUnits.Add(unit);
                    }
                }
            }

            return submeshUnits;
        }

        /// <summary>
        /// 策略1：按材质分组合并子网格
        /// 将所有相同材质的SubmeshUnit合并成一个SubmeshUnit
        /// </summary>
        private async UniTask<List<SubmeshUnit>> MergeSubmeshUnitsByMaterialAsync(List<SubmeshUnit> submeshUnits)
        {
            var materialGroupMap = new Dictionary<Material, List<SubmeshUnit>>();

            // 按材质分组
            foreach (var unit in submeshUnits)
            {
                if (!materialGroupMap.TryGetValue(unit.Material, out var group))
                {
                    group = new List<SubmeshUnit>();
                    materialGroupMap[unit.Material] = group;
                }
                group.Add(unit);
            }

            var mergedUnits = new List<SubmeshUnit>();

            // 合并每个材质分组中的所有SubmeshUnit
            foreach (var kvp in materialGroupMap)
            {
                var material = kvp.Key;
                var unitsToMerge = kvp.Value;

                if (unitsToMerge.Count == 1)
                {
                    // 只有一个单元，直接添加
                    mergedUnits.Add(unitsToMerge[0]);
                }
                else
                {
                    // 多个单元需要合并
                    var mergedUnit = await MergeSubmeshUnitsAsync(unitsToMerge, material);
                    if (!mergedUnit.IsValid)
                    {
                        Log.Error($"[MeshCombiner] Merged unit for material {material.name} is invalid. " +
                                  $"Vertices: {mergedUnit.Vertices.Count}, Triangles: {mergedUnit.Triangles.Count}");
                        continue;
                    }
                    mergedUnits.Add(mergedUnit);
                }
            }

            return mergedUnits;
        }

        /// <summary>
        /// 合并多个SubmeshUnit
        /// </summary>
        private async UniTask<SubmeshUnit> MergeSubmeshUnitsAsync(List<SubmeshUnit> unitsToMerge, Material material)
        {
            // 创建一个虚拟网格来容纳合并后的数据
            var mergedMesh = new Mesh();
            mergedMesh.name = $"MergedMesh_{material.name}";

            var allVertices = new List<Vector3>();
            var allNormals = new List<Vector3>();
            var allTangents = new List<Vector4>();
            var allUVs = new List<Vector2>();
            var allBoneWeights = new List<BoneWeight>();
            var allTriangles = new List<int>();

            int vertexOffset = 0;

            // 合并所有单元的数据
            int processedCount = 0;
            foreach (var unit in unitsToMerge)
            {
                allVertices.AddRange(unit.Vertices);
                allNormals.AddRange(unit.Normals);
                allTangents.AddRange(unit.Tangents);
                allUVs.AddRange(unit.Uvs);
                allBoneWeights.AddRange(unit.BoneWeights);

                // 重映射三角形索引
                foreach (var triangle in unit.Triangles)
                {
                    allTriangles.Add(triangle + vertexOffset);
                    processedCount++;
                    if (processedCount % 5000 == 0)
                    {
                        processedCount = 0;
                        await UniTask.NextFrame(); // 避免长时间阻塞主线程
                    }
                }
                vertexOffset += unit.VertexCount;
            }

            // 设置合并后的网格数据
            mergedMesh.SetVertices(allVertices);
            mergedMesh.SetNormals(allNormals);
            mergedMesh.SetTangents(allTangents);
            mergedMesh.SetUVs(0, allUVs);
            mergedMesh.boneWeights = allBoneWeights.ToArray();
            mergedMesh.SetTriangles(allTriangles, 0);

            // 如果原始法线数据不完整，重新计算法线
            if (allNormals.Count == 0 || allNormals.Count != allVertices.Count)
                mergedMesh.RecalculateNormals();

            // 如果原始切线数据不完整，重新计算切线
            if (allTangents.Count == 0 || allTangents.Count != allVertices.Count)
                mergedMesh.RecalculateTangents();

            Log.Debug($"[XFramework] [MeshCombiner] Merged {unitsToMerge.Count} units for material {material.name}: " +
                     $"{allVertices.Count} vertices, {allTriangles.Count} triangles");

            // 创建新的SubmeshUnit表示合并结果
            return SubmeshUnit.Create(mergedMesh, 0, material);
        }

        /// <summary>
        /// 按给定的子网格单元建立一个新的包含多个子网格的大网格
        /// </summary>
        /// <param name="submeshUnits">子网格单元列表</param>
        /// <param name="bindPoses">绑定姿势矩阵数组</param>
        private MeshCombineResult BuildFinalMesh(IReadOnlyList<SubmeshUnit> submeshUnits, IReadOnlyList<Matrix4x4> bindPoses)
        {
            var result = new MeshCombineResult { Success = false };

            if (submeshUnits.Count == 0)
            {
                Log.Warning("[MeshCombiner] No submesh units to build final mesh.");
                return result;
            }

            var combinedMaterials = new Material[submeshUnits.Count];
            var combinedVertices = new List<Vector3>();
            var combinedNormals = new List<Vector3>();
            var combinedTangents = new List<Vector4>();
            var combinedUVs = new List<Vector2>();
            var combinedBoneWeights = new List<BoneWeight>();
            var submeshToTriangles = new int[submeshUnits.Count][];

            int vertexOffset = 0;

            for (int i = 0; i < submeshUnits.Count; i++)
            {
                var unit = submeshUnits[i];

                // 添加材质
                combinedMaterials[i] = unit.Material;
                // 添加顶点数据
                combinedVertices.AddRange(unit.Vertices);
                combinedNormals.AddRange(unit.Normals);
                combinedTangents.AddRange(unit.Tangents);
                combinedUVs.AddRange(unit.Uvs);
                combinedBoneWeights.AddRange(unit.BoneWeights);
                // 三角形索引重映射到新的顶点索引
                submeshToTriangles[i] = new int[unit.Triangles.Count];
                for (int j = 0; j < unit.Triangles.Count; j++)
                {
                    var triangleInUnit = unit.Triangles[j];
                    submeshToTriangles[i][j] = triangleInUnit + vertexOffset;
                }

                vertexOffset += unit.VertexCount;
            }

            // 创建最终网格
            var finalMesh = CreateFinalCombinedMesh(submeshToTriangles, combinedVertices, combinedNormals, combinedTangents, combinedUVs, combinedBoneWeights, bindPoses);

            if (finalMesh != null)
            {
                result.Success = true;
                result.CombinedMesh = finalMesh;
                result.CombinedMaterials = combinedMaterials;

                Log.Info($"[XFramework] [MeshCombiner] Successfully built final mesh with {submeshUnits.Count} submeshes, " +
                         $"{combinedVertices.Count} vertices, {submeshToTriangles.Sum(t => t.Length)} triangles");
            }

            return result;
        }

        /// <summary>
        /// 创建最终的合并网格
        /// </summary>
        private Mesh CreateFinalCombinedMesh(IReadOnlyList<int[]> submeshToTriangles, IReadOnlyList<Vector3> vertices, IReadOnlyList<Vector3> normals,
            IReadOnlyList<Vector4> tangents, IReadOnlyList<Vector2> uvs, IReadOnlyList<BoneWeight> boneWeights, IReadOnlyList<Matrix4x4> bindPoses)
        {
            try
            {
                var mesh = new Mesh
                {
                    name = "CombinedMesh",
                    hideFlags = HideFlags.HideAndDontSave
                };

                // 设置顶点数据 
                mesh.SetVertices(vertices.ToArray());
                mesh.SetNormals(normals.ToArray());
                mesh.SetTangents(tangents.ToArray());
                mesh.SetUVs(0, uvs.ToArray());

                mesh.subMeshCount = submeshToTriangles.Count;
                for (int submeshIndex = 0; submeshIndex < submeshToTriangles.Count; submeshIndex++)
                {
                    mesh.SetTriangles(submeshToTriangles[submeshIndex], submeshIndex);
                }

                mesh.boneWeights = boneWeights.ToArray();
                mesh.bindposes = bindPoses.ToArray();

                return mesh;
            }
            catch (Exception e)
            {
                Log.Error($"[MeshCombiner] CreateFinalCombinedMesh error - {e.Message}");
                return null;
            }
        }

        #endregion

        #region 性能优化


        #endregion
    }
}
