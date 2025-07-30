using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
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
            private readonly Mesh _sourceMesh;
            private readonly int _submeshIndex;
            private readonly Material _material;
            private readonly List<int> _triangles = new();
            private readonly List<Vector3> _vertices = new();
            private readonly List<Vector3> _normals = new();
            private readonly List<Vector4> _tangents = new();
            private readonly List<Vector2> _uvs = new();
            private readonly List<BoneWeight> _boneWeights = new();

            public Material Material => _material;
            public List<int> Triangles => _triangles;
            public List<Vector3> Vertices => _vertices;
            public List<Vector3> Normals => _normals;
            public List<Vector4> Tangents => _tangents;
            public List<Vector2> Uvs => _uvs;
            public List<BoneWeight> BoneWeights => _boneWeights;

            public bool IsValid => _vertices.Count > 0 && _triangles.Count > 0;
            public int VertexCount => _vertices.Count;

            public SubmeshUnit(Mesh sourceMesh, int submeshIndex, Material material)
            {
                _sourceMesh = sourceMesh;
                _submeshIndex = submeshIndex;
                _material = material;
                InitData();
            }

            private void InitData()
            {
                if (_sourceMesh == null) throw new ArgumentNullException(nameof(_sourceMesh));

                // 检查源网格数据完整性
                bool hasNormals = _sourceMesh.normals != null && _sourceMesh.normals.Length == _sourceMesh.vertices.Length;
                bool hasTangents = _sourceMesh.tangents != null && _sourceMesh.tangents.Length == _sourceMesh.vertices.Length;
                bool hasUV = _sourceMesh.uv != null && _sourceMesh.uv.Length == _sourceMesh.vertices.Length;
                bool hasBoneWeights = _sourceMesh.boneWeights != null && _sourceMesh.boneWeights.Length == _sourceMesh.vertices.Length;

                // 对源网格不完整的情况打印日志提醒
                if (!hasNormals)
                    Log.Debug($"[MeshCombiner] Mesh '{_sourceMesh.name}' missing normals, will recalculate.");
                if (!hasTangents)
                    Log.Debug($"[MeshCombiner] Mesh '{_sourceMesh.name}' missing tangents, will recalculate.");
                if (!hasUV)
                    Log.Warning($"[MeshCombiner] Mesh '{_sourceMesh.name}' missing UV coordinates, using default values. This may affect texture rendering.");
                if (!hasBoneWeights)
                    Log.Warning($"[MeshCombiner] Mesh '{_sourceMesh.name}' missing bone weights, using default weights. This may affect skinning.");

                // 提取子网格数据
                var submesh = _sourceMesh.GetSubMesh(_submeshIndex);
                var triangles = _sourceMesh.GetTriangles(_submeshIndex);

                var usedVertices = new HashSet<int>();
                foreach (var triangle in triangles)
                {
                    usedVertices.Add(triangle);
                }

                // 顶点重映射，缩短数组需要的长度以节约内存
                var vertexRemapping = new Dictionary<int, int>();
                int newIndex = 0;

                var sortedVertices = usedVertices.OrderBy(x => x);
                foreach (var oldIndex in sortedVertices)
                {
                    vertexRemapping[oldIndex] = newIndex++;

                    // 顶点位置
                    _vertices.Add(_sourceMesh.vertices[oldIndex]);

                    // 法线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasNormals)
                        _normals.Add(_sourceMesh.normals[oldIndex]);

                    // 切线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasTangents)
                        _tangents.Add(_sourceMesh.tangents[oldIndex]);

                    // UV坐标（无法自动计算，使用默认值，可能影响材质渲染效果）
                    if (hasUV)
                        _uvs.Add(_sourceMesh.uv[oldIndex]);
                    else
                        _uvs.Add(Vector2.zero);

                    // 骨骼权重（无法自动计算，使用默认值，可能影响蒙皮效果）
                    if (hasBoneWeights)
                        _boneWeights.Add(_sourceMesh.boneWeights[oldIndex]);
                    else
                        _boneWeights.Add(new BoneWeight { weight0 = 1.0f, boneIndex0 = 0 });
                }

                // 三角形索引重映射
                foreach (var oldIndex in triangles)
                {
                    _triangles.Add(vertexRemapping[oldIndex]);
                }
            }
        }

        /// <summary>
        /// 合并结果
        /// </summary>
        public struct CombineResult
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
        public CombineResult Combine(List<DressupItem> items, IReadOnlyList<Matrix4x4> bindPoses)
        {
            if (items == null || items.Count == 0)
            {
                Log.Error("[MeshCombiner] No items to combine");
                return new CombineResult { Success = false };
            }

            var submeshUnits = ExtractSubmeshUnits(items);
            var mergedSubmeshUnits = MergeSubmeshUnitsByMaterial(submeshUnits);
            var result = BuildFinalMesh(mergedSubmeshUnits, bindPoses);

            return result;
        }

        #endregion

        #region 核心实现

        /// <summary>
        /// 步骤1: 提取拆分所有实例的子网格
        /// </summary>
        private List<SubmeshUnit> ExtractSubmeshUnits(List<DressupItem> items)
        {
            var submeshUnits = new List<SubmeshUnit>();

            foreach (var item in items)
            {
                if (!item.IsValid) continue;

                var mesh = item.Mesh;
                var materials = item.Materials;

                // 为每个子网格创建一个子网格单元
                for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
                {
                    // 多余的材质直接忽略
                    if (submeshIndex >= materials.Length) break;

                    var material = materials[submeshIndex];
                    var unit = new SubmeshUnit(mesh, submeshIndex, material);

                    if (unit.IsValid)
                    {
                        submeshUnits.Add(unit);
                    }
                }
            }

            return submeshUnits;
        }

        /// <summary>
        /// 步骤2: 按材质分组并合并子网格
        /// 将所有相同材质的SubmeshUnit合并成一个SubmeshUnit
        /// </summary>
        private List<SubmeshUnit> MergeSubmeshUnitsByMaterial(List<SubmeshUnit> submeshUnits)
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
                    var mergedUnit = MergeSubmeshUnits(unitsToMerge, material);
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
        /// 合并多个相同材质的SubmeshUnit
        /// </summary>
        private SubmeshUnit MergeSubmeshUnits(List<SubmeshUnit> unitsToMerge, Material material)
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
            return new SubmeshUnit(mergedMesh, 0, material);
        }

        /// <summary>
        /// 步骤3: 按最终的子网格数量重建新的包含多个子网格的大网格
        /// </summary>
        private CombineResult BuildFinalMesh(IReadOnlyList<SubmeshUnit> submeshUnits, IReadOnlyList<Matrix4x4> bindPoses)
        {
            var result = new CombineResult { Success = false };

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
                    name = "CombinedMesh"
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
    }
}
