using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using XFramework.Utils;

namespace XFramework.SimpleDressup
{
    /// <summary>
    /// 优化版网格合并器 - 基于材质的分组策略
    /// 性能优化包括：数组替代List、预分配内存、分帧处理、对象池、Job System等
    /// 1. 拆分所有实例的子网格（以材质-子网格为单位）
    /// 2. 合并相同材质的子网格
    /// 3. 按最终的子网格数量重建新的包含多个子网格的大网格
    /// </summary>
    public class MeshCombinerOptimized
    {
        #region 常量配置

        private const int FRAME_BUDGET_MS = 16; // 每帧最大处理时间（毫秒）
        private const int BATCH_SIZE = 1000; // 批处理大小
        private const int DEFAULT_VERTEX_CAPACITY = 65536; // 默认顶点容量
        private const int DEFAULT_TRIANGLE_CAPACITY = 196608; // 默认三角形容量（顶点数*3）
        private const int LARGE_MESH_THRESHOLD = 10000; // 大型网格阈值（三角形数量）

        #endregion

        #region 数据结构

        /// <summary>
        /// 优化的子网格单元 - 使用数组减少内存分配和提高访问性能
        /// </summary>
        private class SubmeshUnit
        {
            public Material Material { get; set; }
            public int[] Triangles { get; set; }
            public Vector3[] Vertices { get; set; }
            public Vector3[] Normals { get; set; }
            public Vector4[] Tangents { get; set; }
            public Vector2[] Uvs { get; set; }
            public BoneWeight[] BoneWeights { get; set; }

            public bool IsValid => Vertices != null && Vertices.Length > 0 && Triangles != null && Triangles.Length > 0;
            public int VertexCount => Vertices?.Length ?? 0;
            public int TriangleCount => Triangles?.Length ?? 0;

            /// <summary>
            /// 优化的创建方法 - 减少内存分配和提高性能
            /// </summary>
            public static SubmeshUnit Create(Mesh sourceMesh, int submeshIndex, Material material)
            {
                if (sourceMesh == null) throw new ArgumentNullException(nameof(sourceMesh));

                var unit = new SubmeshUnit
                {
                    Material = material
                };

                // 缓存源网格数据数组，避免重复获取
                var sourceVertices = sourceMesh.vertices;
                var sourceNormals = sourceMesh.normals;
                var sourceTangents = sourceMesh.tangents;
                var sourceUVs = sourceMesh.uv;
                var sourceBoneWeights = sourceMesh.boneWeights;
                var vertexCount = sourceMesh.vertexCount;

                // 检查源网格数据完整性
                bool hasNormals = sourceNormals != null && sourceNormals.Length == vertexCount;
                bool hasTangents = sourceTangents != null && sourceTangents.Length == vertexCount;
                bool hasUV = sourceUVs != null && sourceUVs.Length == vertexCount;
                bool hasBoneWeights = sourceBoneWeights != null && sourceBoneWeights.Length == vertexCount;

                if (!hasNormals)
                    Log.Debug($"[MeshCombinerOptimized] Mesh '{sourceMesh.name}' missing normals, will recalculate.");
                if (!hasTangents)
                    Log.Debug($"[MeshCombinerOptimized] Mesh '{sourceMesh.name}' missing tangents, will recalculate.");
                if (!hasUV)
                    Log.Warning($"[MeshCombinerOptimized] Mesh '{sourceMesh.name}' missing UV coordinates, using default values. This may affect texture rendering.");
                if (!hasBoneWeights)
                    Log.Warning($"[MeshCombinerOptimized] Mesh '{sourceMesh.name}' missing bone weights, using default weights. This may affect skinning.");

                // 提取子网格三角形索引
                var subTriangles = sourceMesh.GetTriangles(submeshIndex);
                if (subTriangles == null || subTriangles.Length == 0)
                {
                    return unit; // 返回无效单元
                }

                // 优化方案1：使用 bool 数组标记顶点（适用于顶点索引范围较小的情况）
                var maxVertexIndex = 0;
                for (int i = 0; i < subTriangles.Length; i++)
                {
                    if (subTriangles[i] > maxVertexIndex)
                        maxVertexIndex = subTriangles[i];
                }

                int usedVertexCount;
                int[] usedVertexArray;
                Dictionary<int, int> vertexRemapping;

                // 根据顶点范围选择最优算法
                if (maxVertexIndex < 10000) // 小范围使用 bool 数组
                {
                    var vertexUsed = new bool[maxVertexIndex + 1];
                    usedVertexCount = 0;

                    // 标记使用的顶点
                    for (int i = 0; i < subTriangles.Length; i++)
                    {
                        var vertexIndex = subTriangles[i];
                        if (!vertexUsed[vertexIndex])
                        {
                            vertexUsed[vertexIndex] = true;
                            usedVertexCount++;
                        }
                    }

                    // 收集使用的顶点索引
                    usedVertexArray = new int[usedVertexCount];
                    vertexRemapping = new Dictionary<int, int>(usedVertexCount);
                    var arrayIndex = 0;

                    for (int i = 0; i <= maxVertexIndex; i++)
                    {
                        if (vertexUsed[i])
                        {
                            usedVertexArray[arrayIndex] = i;
                            vertexRemapping[i] = arrayIndex;
                            arrayIndex++;
                        }
                    }
                }
                else // 大范围使用排序去重
                {
                    // 复制并排序三角形索引
                    var sortedTriangles = new int[subTriangles.Length];
                    Array.Copy(subTriangles, sortedTriangles, subTriangles.Length);
                    Array.Sort(sortedTriangles);

                    // 计算唯一顶点数量
                    usedVertexCount = 1;
                    for (int i = 1; i < sortedTriangles.Length; i++)
                    {
                        if (sortedTriangles[i] != sortedTriangles[i - 1])
                            usedVertexCount++;
                    }

                    // 创建唯一顶点数组和映射
                    usedVertexArray = new int[usedVertexCount];
                    vertexRemapping = new Dictionary<int, int>(usedVertexCount);

                    usedVertexArray[0] = sortedTriangles[0];
                    vertexRemapping[sortedTriangles[0]] = 0;

                    int uniqueIndex = 1;
                    for (int i = 1; i < sortedTriangles.Length; i++)
                    {
                        if (sortedTriangles[i] != sortedTriangles[i - 1])
                        {
                            usedVertexArray[uniqueIndex] = sortedTriangles[i];
                            vertexRemapping[sortedTriangles[i]] = uniqueIndex;
                            uniqueIndex++;
                        }
                    }
                }

                // 预分配数组容量，避免动态扩容
                unit.Vertices = new Vector3[usedVertexCount];
                unit.Normals = hasNormals ? new Vector3[usedVertexCount] : new Vector3[0];
                unit.Tangents = hasTangents ? new Vector4[usedVertexCount] : new Vector4[0];
                unit.Uvs = new Vector2[usedVertexCount];
                unit.BoneWeights = new BoneWeight[usedVertexCount];
                unit.Triangles = new int[subTriangles.Length];

                // 复制顶点数据
                for (int i = 0; i < usedVertexArray.Length; i++)
                {
                    var oldIndex = usedVertexArray[i];

                    // 复制顶点数据
                    unit.Vertices[i] = sourceVertices[oldIndex];

                    if (hasNormals)
                        unit.Normals[i] = sourceNormals[oldIndex];

                    if (hasTangents)
                        unit.Tangents[i] = sourceTangents[oldIndex];

                    if (hasUV)
                        unit.Uvs[i] = sourceUVs[oldIndex];
                    else
                        unit.Uvs[i] = Vector2.zero;

                    if (hasBoneWeights)
                        unit.BoneWeights[i] = sourceBoneWeights[oldIndex];
                    else
                        unit.BoneWeights[i] = new BoneWeight { weight0 = 1.0f, boneIndex0 = 0 };
                }

                // 重映射三角形索引
                for (int i = 0; i < subTriangles.Length; i++)
                {
                    unit.Triangles[i] = vertexRemapping[subTriangles[i]];
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
                Log.Error("[MeshCombinerOptimized] No items to combine");
                return new MeshCombineResult { Success = false };
            }

            var submeshUnits = await ExtractSubmeshUnitsAsync(items);
            var mergedSubmeshUnits = await MergeSubmeshUnitsByMaterialAsync(submeshUnits);
            var result = await BuildFinalMeshAsync(mergedSubmeshUnits, bindPoses);

            return result;
        }

        #endregion

        #region 核心实现

        /// <summary>
        /// 优化的提取子网格单元方法 - 预估容量减少内存分配
        /// </summary>
        private async UniTask<List<SubmeshUnit>> ExtractSubmeshUnitsAsync(List<DressupItem> items)
        {
            // 预估总的子网格数量
            int estimatedSubmeshCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsValid)
                    estimatedSubmeshCount += items[i].Mesh.subMeshCount;
            }

            var submeshUnits = new List<SubmeshUnit>(estimatedSubmeshCount);
            int processedCount = 0;
            var frameTimer = System.Diagnostics.Stopwatch.StartNew();

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

                    // 检查是否使用 Job System 处理大型网格
                    var subTriangles = mesh.GetTriangles(submeshIndex);
                    SubmeshUnit unit;

                    if (subTriangles.Length > LARGE_MESH_THRESHOLD)
                    {
                        unit = await CreateSubmeshUnitWithJobsAsync(mesh, submeshIndex, material);
                    }
                    else
                    {
                        unit = SubmeshUnit.Create(mesh, submeshIndex, material);
                    }

                    if (unit.IsValid)
                    {
                        submeshUnits.Add(unit);
                    }

                    processedCount++;
                    // 分帧处理，避免长时间阻塞
                    if (processedCount % BATCH_SIZE == 0 && frameTimer.ElapsedMilliseconds > FRAME_BUDGET_MS)
                    {
                        await UniTask.NextFrame();
                        frameTimer.Restart();
                    }
                }
            }

            return submeshUnits;
        }

        /// <summary>
        /// 策略1：按材质分组合并子网格 - 优化版本
        /// 将所有相同材质的SubmeshUnit合并成一个SubmeshUnit
        /// </summary>
        private async UniTask<List<SubmeshUnit>> MergeSubmeshUnitsByMaterialAsync(List<SubmeshUnit> submeshUnits)
        {
            var materialGroupMap = MeshCombinerPools.GetMaterialGroupMap();
            try
            {
                // 按材质分组 - 使用 for 循环减少装箱
                for (int i = 0; i < submeshUnits.Count; i++)
                {
                    var unit = submeshUnits[i];
                    if (!materialGroupMap.TryGetValue(unit.Material, out var group))
                    {
                        group = MeshCombinerPools.GetSubmeshUnitList();
                        materialGroupMap[unit.Material] = group;
                    }
                    group.Add(unit);
                }

                var mergedUnits = new List<SubmeshUnit>(materialGroupMap.Count);
                int processedGroups = 0;
                var frameTimer = System.Diagnostics.Stopwatch.StartNew();

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
                            Log.Error($"[MeshCombinerOptimized] Merged unit for material {material.name} is invalid. " +
                                      $"Vertices: {mergedUnit.VertexCount}, Triangles: {mergedUnit.TriangleCount}");
                            continue;
                        }
                        mergedUnits.Add(mergedUnit);
                    }

                    processedGroups++;
                    // 分帧处理大量材质分组
                    if (processedGroups % 10 == 0 && frameTimer.ElapsedMilliseconds > FRAME_BUDGET_MS)
                    {
                        await UniTask.NextFrame();
                        frameTimer.Restart();
                    }
                }

                return mergedUnits;
            }
            finally
            {
                // 清理对象池
                foreach (var group in materialGroupMap.Values)
                {
                    MeshCombinerPools.ReturnSubmeshUnitList(group);
                }
                MeshCombinerPools.ReturnMaterialGroupMap(materialGroupMap);
            }
        }

        /// <summary>
        /// 高性能合并多个SubmeshUnit - 使用预分配数组和批处理
        /// </summary>
        private async UniTask<SubmeshUnit> MergeSubmeshUnitsAsync(List<SubmeshUnit> unitsToMerge, Material material)
        {
            // 预计算总容量，避免动态扩容
            int totalVertexCount = 0;
            int totalTriangleCount = 0;
            for (int i = 0; i < unitsToMerge.Count; i++)
            {
                totalVertexCount += unitsToMerge[i].VertexCount;
                totalTriangleCount += unitsToMerge[i].TriangleCount;
            }

            // 预分配合并后的数组
            var allVertices = new Vector3[totalVertexCount];
            var allNormals = new Vector3[totalVertexCount];
            var allTangents = new Vector4[totalVertexCount];
            var allUVs = new Vector2[totalVertexCount];
            var allBoneWeights = new BoneWeight[totalVertexCount];
            var allTriangles = new int[totalTriangleCount];

            int vertexOffset = 0;
            int triangleOffset = 0;
            int processedTriangles = 0;
            var frameTimer = System.Diagnostics.Stopwatch.StartNew();

            // 高效合并所有单元的数据
            for (int unitIndex = 0; unitIndex < unitsToMerge.Count; unitIndex++)
            {
                var unit = unitsToMerge[unitIndex];
                var unitVertexCount = unit.VertexCount;
                var unitTriangleCount = unit.TriangleCount;

                // 使用 Array.Copy 进行批量复制，性能优于逐个复制
                Array.Copy(unit.Vertices, 0, allVertices, vertexOffset, unitVertexCount);
                if (unit.Normals.Length > 0)
                    Array.Copy(unit.Normals, 0, allNormals, vertexOffset, unitVertexCount);
                if (unit.Tangents.Length > 0)
                    Array.Copy(unit.Tangents, 0, allTangents, vertexOffset, unitVertexCount);
                Array.Copy(unit.Uvs, 0, allUVs, vertexOffset, unitVertexCount);
                Array.Copy(unit.BoneWeights, 0, allBoneWeights, vertexOffset, unitVertexCount);

                // 重映射三角形索引 - 批处理以提高性能
                for (int i = 0; i < unitTriangleCount; i++)
                {
                    allTriangles[triangleOffset + i] = unit.Triangles[i] + vertexOffset;
                    processedTriangles++;

                    // 分帧处理大量三角形
                    if (processedTriangles % BATCH_SIZE == 0 && frameTimer.ElapsedMilliseconds > FRAME_BUDGET_MS)
                    {
                        await UniTask.NextFrame();
                        frameTimer.Restart();
                    }
                }

                vertexOffset += unitVertexCount;
                triangleOffset += unitTriangleCount;
            }

            // 创建优化的虚拟网格
            var mergedMesh = new Mesh
            {
                name = $"MergedMesh_{material.name}",
                hideFlags = HideFlags.HideAndDontSave
            };

            // 直接设置数组数据，避免 ToArray() 调用
            mergedMesh.SetVertices(allVertices);
            if (allNormals.Length > 0 && allNormals[0] != Vector3.zero)
                mergedMesh.SetNormals(allNormals);
            if (allTangents.Length > 0 && allTangents[0] != Vector4.zero)
                mergedMesh.SetTangents(allTangents);
            mergedMesh.SetUVs(0, allUVs);
            mergedMesh.boneWeights = allBoneWeights;
            mergedMesh.SetTriangles(allTriangles, 0);

            // 只在必要时重新计算法线和切线
            if (allNormals.Length == 0 || allNormals[0] == Vector3.zero)
                mergedMesh.RecalculateNormals();
            if (allTangents.Length == 0 || allTangents[0] == Vector4.zero)
                mergedMesh.RecalculateTangents();

            Log.Debug($"[XFramework] [MeshCombinerOptimized] Merged {unitsToMerge.Count} units for material {material.name}: " +
                     $"{totalVertexCount} vertices, {totalTriangleCount} triangles");

            // 创建新的SubmeshUnit表示合并结果
            return SubmeshUnit.Create(mergedMesh, 0, material);
        }

        /// <summary>
        /// 高性能构建最终网格 - 使用预分配和分帧处理
        /// </summary>
        /// <param name="submeshUnits">子网格单元列表</param>
        /// <param name="bindPoses">绑定姿势矩阵数组</param>
        private async UniTask<MeshCombineResult> BuildFinalMeshAsync(IReadOnlyList<SubmeshUnit> submeshUnits, IReadOnlyList<Matrix4x4> bindPoses)
        {
            var result = new MeshCombineResult { Success = false };

            if (submeshUnits.Count == 0)
            {
                Log.Warning("[MeshCombinerOptimized] No submesh units to build final mesh.");
                return result;
            }

            // 预计算总容量
            int totalVertexCount = 0;
            for (int i = 0; i < submeshUnits.Count; i++)
            {
                totalVertexCount += submeshUnits[i].VertexCount;
            }

            // 预分配数组
            var combinedMaterials = new Material[submeshUnits.Count];
            var combinedVertices = new Vector3[totalVertexCount];
            var combinedNormals = new Vector3[totalVertexCount];
            var combinedTangents = new Vector4[totalVertexCount];
            var combinedUVs = new Vector2[totalVertexCount];
            var combinedBoneWeights = new BoneWeight[totalVertexCount];
            var submeshToTriangles = new int[submeshUnits.Count][];

            int vertexOffset = 0;
            var frameTimer = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < submeshUnits.Count; i++)
            {
                var unit = submeshUnits[i];
                var unitVertexCount = unit.VertexCount;
                var unitTriangleCount = unit.TriangleCount;

                // 添加材质
                combinedMaterials[i] = unit.Material;

                // 批量复制顶点数据
                Array.Copy(unit.Vertices, 0, combinedVertices, vertexOffset, unitVertexCount);
                if (unit.Normals.Length > 0)
                    Array.Copy(unit.Normals, 0, combinedNormals, vertexOffset, unitVertexCount);
                if (unit.Tangents.Length > 0)
                    Array.Copy(unit.Tangents, 0, combinedTangents, vertexOffset, unitVertexCount);
                Array.Copy(unit.Uvs, 0, combinedUVs, vertexOffset, unitVertexCount);
                Array.Copy(unit.BoneWeights, 0, combinedBoneWeights, vertexOffset, unitVertexCount);

                // 三角形索引重映射到新的顶点索引
                submeshToTriangles[i] = new int[unitTriangleCount];
                for (int j = 0; j < unitTriangleCount; j++)
                {
                    submeshToTriangles[i][j] = unit.Triangles[j] + vertexOffset;
                }

                vertexOffset += unitVertexCount;

                // 分帧处理大量子网格
                if (i % 50 == 0 && frameTimer.ElapsedMilliseconds > FRAME_BUDGET_MS)
                {
                    await UniTask.NextFrame();
                    frameTimer.Restart();
                }
            }

            // 创建最终网格
            var finalMesh = CreateFinalCombinedMesh(submeshToTriangles, combinedVertices, combinedNormals,
                combinedTangents, combinedUVs, combinedBoneWeights, bindPoses);

            if (finalMesh != null)
            {
                result.Success = true;
                result.CombinedMesh = finalMesh;
                result.CombinedMaterials = combinedMaterials;

                // 计算总三角形数
                int totalTriangles = 0;
                for (int i = 0; i < submeshToTriangles.Length; i++)
                {
                    totalTriangles += submeshToTriangles[i].Length;
                }

                Log.Info($"[XFramework] [MeshCombinerOptimized] Successfully built final mesh with {submeshUnits.Count} submeshes, " +
                         $"{totalVertexCount} vertices, {totalTriangles} triangles");
            }

            return result;
        }

        /// <summary>
        /// 创建最终的合并网格 - 优化版本，直接使用数组避免转换
        /// </summary>
        private Mesh CreateFinalCombinedMesh(IReadOnlyList<int[]> submeshToTriangles, Vector3[] vertices, Vector3[] normals,
            Vector4[] tangents, Vector2[] uvs, BoneWeight[] boneWeights, IReadOnlyList<Matrix4x4> bindPoses)
        {
            try
            {
                var mesh = new Mesh
                {
                    name = "CombinedMeshOptimized",
                    hideFlags = HideFlags.HideAndDontSave
                };

                // 直接使用数组设置顶点数据，避免 ToArray() 调用
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTangents(tangents);
                mesh.SetUVs(0, uvs);

                mesh.subMeshCount = submeshToTriangles.Count;
                for (int submeshIndex = 0; submeshIndex < submeshToTriangles.Count; submeshIndex++)
                {
                    mesh.SetTriangles(submeshToTriangles[submeshIndex], submeshIndex);
                }

                mesh.boneWeights = boneWeights;

                // 转换 bindPoses 为数组
                var bindPosesArray = new Matrix4x4[bindPoses.Count];
                for (int i = 0; i < bindPoses.Count; i++)
                {
                    bindPosesArray[i] = bindPoses[i];
                }
                mesh.bindposes = bindPosesArray;

                return mesh;
            }
            catch (Exception e)
            {
                Log.Error($"[MeshCombinerOptimized] CreateFinalCombinedMesh error - {e.Message}");
                return null;
            }
        }

        #endregion

        #region 性能优化

        /// <summary>
        /// 对象池管理器 - 减少 GC 压力
        /// </summary>
        private static class MeshCombinerPools
        {
            private static readonly Stack<List<SubmeshUnit>> _submeshUnitListPool = new();
            private static readonly Stack<Dictionary<Material, List<SubmeshUnit>>> _materialGroupMapPool = new();
            private static readonly Stack<HashSet<int>> _vertexIndexSetPool = new();
            private static readonly Stack<Dictionary<int, int>> _vertexRemappingPool = new();

            public static List<SubmeshUnit> GetSubmeshUnitList()
            {
                if (_submeshUnitListPool.Count > 0)
                {
                    var list = _submeshUnitListPool.Pop();
                    list.Clear();
                    return list;
                }
                return new List<SubmeshUnit>();
            }

            public static void ReturnSubmeshUnitList(List<SubmeshUnit> list)
            {
                if (list != null && _submeshUnitListPool.Count < 10)
                {
                    list.Clear();
                    _submeshUnitListPool.Push(list);
                }
            }

            public static Dictionary<Material, List<SubmeshUnit>> GetMaterialGroupMap()
            {
                if (_materialGroupMapPool.Count > 0)
                {
                    var dict = _materialGroupMapPool.Pop();
                    dict.Clear();
                    return dict;
                }
                return new Dictionary<Material, List<SubmeshUnit>>();
            }

            public static void ReturnMaterialGroupMap(Dictionary<Material, List<SubmeshUnit>> dict)
            {
                if (dict != null && _materialGroupMapPool.Count < 5)
                {
                    dict.Clear();
                    _materialGroupMapPool.Push(dict);
                }
            }

            public static HashSet<int> GetVertexIndexSet()
            {
                if (_vertexIndexSetPool.Count > 0)
                {
                    var set = _vertexIndexSetPool.Pop();
                    set.Clear();
                    return set;
                }
                return new HashSet<int>();
            }

            public static void ReturnVertexIndexSet(HashSet<int> set)
            {
                if (set != null && _vertexIndexSetPool.Count < 20)
                {
                    set.Clear();
                    _vertexIndexSetPool.Push(set);
                }
            }

            public static Dictionary<int, int> GetVertexRemapping()
            {
                if (_vertexRemappingPool.Count > 0)
                {
                    var dict = _vertexRemappingPool.Pop();
                    dict.Clear();
                    return dict;
                }
                return new Dictionary<int, int>();
            }

            public static void ReturnVertexRemapping(Dictionary<int, int> dict)
            {
                if (dict != null && _vertexRemappingPool.Count < 20)
                {
                    dict.Clear();
                    _vertexRemappingPool.Push(dict);
                }
            }
        }

        /// <summary>
        /// Job System 三角形重映射任务
        /// </summary>
        private struct TriangleRemappingJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> sourceTriangles;
            [ReadOnly] public NativeArray<int> vertexMapping;
            [ReadOnly] public int vertexOffset;
            [WriteOnly] public NativeArray<int> resultTriangles;

            public void Execute(int index)
            {
                resultTriangles[index] = vertexMapping[sourceTriangles[index]] + vertexOffset;
            }
        }

        /// <summary>
        /// Job System 顶点数据复制任务
        /// </summary>
        private struct VertexCopyJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> sourceVertices;
            [ReadOnly] public NativeArray<int> usedIndices;
            [WriteOnly] public NativeArray<Vector3> targetVertices;

            public void Execute(int index)
            {
                targetVertices[index] = sourceVertices[usedIndices[index]];
            }
        }

        /// <summary>
        /// Job System 法线数据复制任务
        /// </summary>
        private struct NormalCopyJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> sourceNormals;
            [ReadOnly] public NativeArray<int> usedIndices;
            [WriteOnly] public NativeArray<Vector3> targetNormals;

            public void Execute(int index)
            {
                targetNormals[index] = sourceNormals[usedIndices[index]];
            }
        }

        /// <summary>
        /// Job System 切线数据复制任务
        /// </summary>
        private struct TangentCopyJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector4> sourceTangents;
            [ReadOnly] public NativeArray<int> usedIndices;
            [WriteOnly] public NativeArray<Vector4> targetTangents;

            public void Execute(int index)
            {
                targetTangents[index] = sourceTangents[usedIndices[index]];
            }
        }

        /// <summary>
        /// Job System UV 数据复制任务
        /// </summary>
        private struct UVCopyJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector2> sourceUVs;
            [ReadOnly] public NativeArray<int> usedIndices;
            [WriteOnly] public NativeArray<Vector2> targetUVs;

            public void Execute(int index)
            {
                targetUVs[index] = sourceUVs[usedIndices[index]];
            }
        }

        /// <summary>
        /// Job System 骨骼权重数据复制任务
        /// </summary>
        private struct BoneWeightCopyJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<BoneWeight> sourceBoneWeights;
            [ReadOnly] public NativeArray<int> usedIndices;
            [WriteOnly] public NativeArray<BoneWeight> targetBoneWeights;

            public void Execute(int index)
            {
                targetBoneWeights[index] = sourceBoneWeights[usedIndices[index]];
            }
        }

        /// <summary>
        /// 使用 Job System 优化大型网格处理
        /// </summary>
        private async UniTask<SubmeshUnit> CreateSubmeshUnitWithJobsAsync(Mesh sourceMesh, int submeshIndex, Material material)
        {
            if (sourceMesh == null) throw new ArgumentNullException(nameof(sourceMesh));

            var unit = new SubmeshUnit { Material = material };

            // 获取子网格三角形索引
            var subTriangles = sourceMesh.GetTriangles(submeshIndex);
            if (subTriangles == null || subTriangles.Length == 0)
                return unit;

            // 对于大型网格，使用 Job System 进行并行处理
            return await ProcessLargeMeshWithJobsAsync(sourceMesh, subTriangles, material);
        }

        /// <summary>
        /// 使用 Job System 处理大型网格
        /// </summary>
        private async UniTask<SubmeshUnit> ProcessLargeMeshWithJobsAsync(Mesh sourceMesh, int[] subTriangles, Material material)
        {
            // 缓存源网格数据，避免重复获取
            var sourceVertices = sourceMesh.vertices;
            var sourceNormals = sourceMesh.normals;
            var sourceTangents = sourceMesh.tangents;
            var sourceUVs = sourceMesh.uv;
            var sourceBoneWeights = sourceMesh.boneWeights;
            var vertexCount = sourceMesh.vertexCount;

            // 检查数据完整性
            bool hasNormals = sourceNormals != null && sourceNormals.Length == vertexCount;
            bool hasTangents = sourceTangents != null && sourceTangents.Length == vertexCount;
            bool hasUV = sourceUVs != null && sourceUVs.Length == vertexCount;
            bool hasBoneWeights = sourceBoneWeights != null && sourceBoneWeights.Length == vertexCount;

            // 使用优化的顶点索引提取算法（与主方法保持一致）
            var maxVertexIndex = 0;
            for (int i = 0; i < subTriangles.Length; i++)
            {
                if (subTriangles[i] > maxVertexIndex)
                    maxVertexIndex = subTriangles[i];
            }

            int usedVertexCount;
            int[] usedVertexArray;
            Dictionary<int, int> vertexRemapping;

            // 根据顶点范围选择最优算法（与主方法保持一致）
            if (maxVertexIndex < 10000)
            {
                var vertexUsed = new bool[maxVertexIndex + 1];
                usedVertexCount = 0;

                for (int i = 0; i < subTriangles.Length; i++)
                {
                    var vertexIndex = subTriangles[i];
                    if (!vertexUsed[vertexIndex])
                    {
                        vertexUsed[vertexIndex] = true;
                        usedVertexCount++;
                    }
                }

                usedVertexArray = new int[usedVertexCount];
                vertexRemapping = new Dictionary<int, int>(usedVertexCount);
                var arrayIndex = 0;

                for (int i = 0; i <= maxVertexIndex; i++)
                {
                    if (vertexUsed[i])
                    {
                        usedVertexArray[arrayIndex] = i;
                        vertexRemapping[i] = arrayIndex;
                        arrayIndex++;
                    }
                }
            }
            else
            {
                var sortedTriangles = new int[subTriangles.Length];
                Array.Copy(subTriangles, sortedTriangles, subTriangles.Length);
                Array.Sort(sortedTriangles);

                usedVertexCount = 1;
                for (int i = 1; i < sortedTriangles.Length; i++)
                {
                    if (sortedTriangles[i] != sortedTriangles[i - 1])
                        usedVertexCount++;
                }

                usedVertexArray = new int[usedVertexCount];
                vertexRemapping = new Dictionary<int, int>(usedVertexCount);

                usedVertexArray[0] = sortedTriangles[0];
                vertexRemapping[sortedTriangles[0]] = 0;

                int uniqueIndex = 1;
                for (int i = 1; i < sortedTriangles.Length; i++)
                {
                    if (sortedTriangles[i] != sortedTriangles[i - 1])
                    {
                        usedVertexArray[uniqueIndex] = sortedTriangles[i];
                        vertexRemapping[sortedTriangles[i]] = uniqueIndex;
                        uniqueIndex++;
                    }
                }
            }

            // 使用 Job System 并行处理顶点数据
            var sourceVerticesNative = new NativeArray<Vector3>(sourceVertices, Allocator.TempJob);
            var usedIndicesNative = new NativeArray<int>(usedVertexArray, Allocator.TempJob);
            var targetVerticesNative = new NativeArray<Vector3>(usedVertexCount, Allocator.TempJob);

            // 可选的其他数据 NativeArray
            NativeArray<Vector3> sourceNormalsNative = default;
            NativeArray<Vector3> targetNormalsNative = default;
            NativeArray<Vector4> sourceTangentsNative = default;
            NativeArray<Vector4> targetTangentsNative = default;
            NativeArray<Vector2> sourceUVsNative = default;
            NativeArray<Vector2> targetUVsNative = default;
            NativeArray<BoneWeight> sourceBoneWeightsNative = default;
            NativeArray<BoneWeight> targetBoneWeightsNative = default;

            try
            {
                // 创建可选数据的 NativeArray
                if (hasNormals)
                {
                    sourceNormalsNative = new NativeArray<Vector3>(sourceNormals, Allocator.TempJob);
                    targetNormalsNative = new NativeArray<Vector3>(usedVertexCount, Allocator.TempJob);
                }
                if (hasTangents)
                {
                    sourceTangentsNative = new NativeArray<Vector4>(sourceTangents, Allocator.TempJob);
                    targetTangentsNative = new NativeArray<Vector4>(usedVertexCount, Allocator.TempJob);
                }
                if (hasUV)
                {
                    sourceUVsNative = new NativeArray<Vector2>(sourceUVs, Allocator.TempJob);
                    targetUVsNative = new NativeArray<Vector2>(usedVertexCount, Allocator.TempJob);
                }
                if (hasBoneWeights)
                {
                    sourceBoneWeightsNative = new NativeArray<BoneWeight>(sourceBoneWeights, Allocator.TempJob);
                    targetBoneWeightsNative = new NativeArray<BoneWeight>(usedVertexCount, Allocator.TempJob);
                }

                // 并行复制顶点数据
                var vertexCopyJob = new VertexCopyJob
                {
                    sourceVertices = sourceVerticesNative,
                    usedIndices = usedIndicesNative,
                    targetVertices = targetVerticesNative
                };

                var jobHandle = vertexCopyJob.Schedule(usedVertexCount, 1000);

                // 可选数据的并行复制
                if (hasNormals)
                {
                    var normalCopyJob = new NormalCopyJob
                    {
                        sourceNormals = sourceNormalsNative,
                        usedIndices = usedIndicesNative,
                        targetNormals = targetNormalsNative
                    };
                    jobHandle = normalCopyJob.Schedule(usedVertexCount, 1000, jobHandle);
                }

                if (hasTangents)
                {
                    var tangentCopyJob = new TangentCopyJob
                    {
                        sourceTangents = sourceTangentsNative,
                        usedIndices = usedIndicesNative,
                        targetTangents = targetTangentsNative
                    };
                    jobHandle = tangentCopyJob.Schedule(usedVertexCount, 1000, jobHandle);
                }

                if (hasUV)
                {
                    var uvCopyJob = new UVCopyJob
                    {
                        sourceUVs = sourceUVsNative,
                        usedIndices = usedIndicesNative,
                        targetUVs = targetUVsNative
                    };
                    jobHandle = uvCopyJob.Schedule(usedVertexCount, 1000, jobHandle);
                }

                if (hasBoneWeights)
                {
                    var boneWeightCopyJob = new BoneWeightCopyJob
                    {
                        sourceBoneWeights = sourceBoneWeightsNative,
                        usedIndices = usedIndicesNative,
                        targetBoneWeights = targetBoneWeightsNative
                    };
                    jobHandle = boneWeightCopyJob.Schedule(usedVertexCount, 1000, jobHandle);
                }

                // 等待所有 Job 完成
                await UniTask.WaitUntil(() => jobHandle.IsCompleted);
                jobHandle.Complete();

                // 创建结果单元
                var unit = new SubmeshUnit { Material = material };

                // 复制处理后的数据
                unit.Vertices = targetVerticesNative.ToArray();
                unit.Normals = hasNormals ? targetNormalsNative.ToArray() : new Vector3[0];
                unit.Tangents = hasTangents ? targetTangentsNative.ToArray() : new Vector4[0];
                unit.Uvs = hasUV ? targetUVsNative.ToArray() : new Vector2[usedVertexCount];
                unit.BoneWeights = hasBoneWeights ? targetBoneWeightsNative.ToArray() : new BoneWeight[usedVertexCount];

                // 处理默认值
                if (!hasUV)
                {
                    for (int i = 0; i < usedVertexCount; i++)
                        unit.Uvs[i] = Vector2.zero;
                }
                if (!hasBoneWeights)
                {
                    for (int i = 0; i < usedVertexCount; i++)
                        unit.BoneWeights[i] = new BoneWeight { weight0 = 1.0f, boneIndex0 = 0 };
                }

                // 重映射三角形索引
                unit.Triangles = new int[subTriangles.Length];
                for (int i = 0; i < subTriangles.Length; i++)
                {
                    unit.Triangles[i] = vertexRemapping[subTriangles[i]];
                }

                return unit;
            }
            finally
            {
                // 清理所有 NativeArray
                if (sourceVerticesNative.IsCreated) sourceVerticesNative.Dispose();
                if (usedIndicesNative.IsCreated) usedIndicesNative.Dispose();
                if (targetVerticesNative.IsCreated) targetVerticesNative.Dispose();
                if (sourceNormalsNative.IsCreated) sourceNormalsNative.Dispose();
                if (targetNormalsNative.IsCreated) targetNormalsNative.Dispose();
                if (sourceTangentsNative.IsCreated) sourceTangentsNative.Dispose();
                if (targetTangentsNative.IsCreated) targetTangentsNative.Dispose();
                if (sourceUVsNative.IsCreated) sourceUVsNative.Dispose();
                if (targetUVsNative.IsCreated) targetUVsNative.Dispose();
                if (sourceBoneWeightsNative.IsCreated) sourceBoneWeightsNative.Dispose();
                if (targetBoneWeightsNative.IsCreated) targetBoneWeightsNative.Dispose();
            }
        }

        #endregion
    }
}
