using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.SimpleDressup
{
    /// <summary>
    /// 网格合并器 - 合并同骨骼的所有网格
    /// 1. 拆分所有实例的子网格（以材质-子网格为单位）
    /// 2. 合并相同材质的子网格
    /// 3. 按最终的子网格数量重建新的包含多个子网格的大网格
    /// </summary>
    public class MeshCombiner
    {
        #region 数据结构

        /// <summary>
        /// 合并多个SubmeshUnit并应用顶点遮挡剔除
        /// 处理bindPose冲突和重叠顶点的智能剔除
        /// </summary>
        private SubmeshUnit MergeSubmeshUnitsWithOcclusionCulling(List<SubmeshUnit> unitsToMerge, Material material)
        {
            if (unitsToMerge.Count == 0)
                return null;

            if (unitsToMerge.Count == 1)
                return unitsToMerge[0];

            // 构建顶点优先级信息列表
            var vertexPriorityInfos = new List<VertexPriorityInfo>();

            for (int unitIndex = 0; unitIndex < unitsToMerge.Count; unitIndex++)
            {
                var unit = unitsToMerge[unitIndex];

                for (int vertIndex = 0; vertIndex < unit.Vertices.Count; vertIndex++)
                {
                    var priorityInfo = new VertexPriorityInfo
                    {
                        WorldPosition = unit.Vertices[vertIndex],
                        Normal = unit.Normals[vertIndex],
                        Tangent = unit.Tangents[vertIndex],
                        UV = unit.Uvs[vertIndex],
                        BoneWeights = unit.BoneWeights[vertIndex],
                        UnitIndex = unitIndex,
                        VertexIndex = vertIndex,
                        DressupType = unit.DressupType,
                        Priority = CalculateVertexPriority(unit.DressupType, unit.Vertices[vertIndex]),
                        SourceUnit = unit
                    };

                    vertexPriorityInfos.Add(priorityInfo);
                }
            }

            // 应用遮挡剔除：移除被高优先级顶点遮挡的低优先级顶点
            var culledVertexInfos = ApplyOcclusionCulling(vertexPriorityInfos);

            // 从剔除后的顶点信息重建SubmeshUnit
            return BuildSubmeshUnitFromVertexInfos(culledVertexInfos, unitsToMerge, material);
        }

        /// <summary>
        /// 计算顶点优先级：不同部位有不同优先级，外层部位优先级更高
        /// </summary>
        private float CalculateVertexPriority(DressupType dressupType, Vector3 worldPos)
        {
            // 定义部位优先级，数值越大越优先显示（越外层）
            float basePriority = dressupType switch
            {
                DressupType.Hat => 1000f,       // 帽子 - 最外层
                DressupType.Hair => 900f,       // 头发 - 外层
                DressupType.Top => 800f,        // 上衣 - 外层
                DressupType.Bottom => 700f,     // 下衣 - 中层
                DressupType.Gloves => 600f,     // 手套 - 中层
                DressupType.Shoes => 500f,      // 鞋子 - 中层
                DressupType.Face => 400f,       // 脸部 - 内层
                DressupType.Body => 100f,       // 身体 - 最内层
                DressupType.None => 0f,         // 无类型 - 最低优先级
                _ => 0f
            };

            // 添加基于世界坐标的微小偏移，避免相同优先级时的不稳定性
            float positionOffset = (worldPos.x + worldPos.y + worldPos.z) * 0.001f;
            return basePriority + positionOffset;
        }

        /// <summary>
        /// 应用遮挡剔除：移除被高优先级顶点遮挡的顶点
        /// </summary>
        private List<VertexPriorityInfo> ApplyOcclusionCulling(List<VertexPriorityInfo> vertexInfos)
        {
            const float OCCLUSION_THRESHOLD = 0.01f; // 1cm的遮挡阈值

            var resultInfos = new List<VertexPriorityInfo>();

            // 按优先级降序排列，高优先级的顶点优先处理
            var sortedInfos = vertexInfos.OrderByDescending(info => info.Priority).ToArray();
            var isOccluded = new bool[sortedInfos.Length];

            for (int i = 0; i < sortedInfos.Length; i++)
            {
                if (isOccluded[i]) continue;

                var currentInfo = sortedInfos[i];
                resultInfos.Add(currentInfo);

                // 检查后续的低优先级顶点是否被当前顶点遮挡
                for (int j = i + 1; j < sortedInfos.Length; j++)
                {
                    if (isOccluded[j]) continue;

                    var otherInfo = sortedInfos[j];

                    // 计算两个顶点的距离
                    float distance = Vector3.Distance(currentInfo.WorldPosition, otherInfo.WorldPosition);

                    if (distance < OCCLUSION_THRESHOLD)
                    {
                        // 如果距离小于阈值，检查法线方向是否相似（表示在同一表面）
                        float normalDot = Vector3.Dot(currentInfo.Normal, otherInfo.Normal);

                        if (normalDot > 0.8f) // 法线夹角小于约36度
                        {
                            // 低优先级顶点被遮挡，标记为移除
                            isOccluded[j] = true;

                            Log.Debug($"[MeshCombiner] Vertex {j} (Priority: {otherInfo.Priority:F1}) " +
                                      $"occluded by vertex {i} (Priority: {currentInfo.Priority:F1}), " +
                                      $"distance: {distance:F4}m");
                        }
                    }
                }
            }

            Log.Info($"[MeshCombiner] Occlusion culling: {vertexInfos.Count} -> {resultInfos.Count} vertices " +
                     $"({vertexInfos.Count - resultInfos.Count} culled)");

            return resultInfos;
        }

        /// <summary>
        /// 从剔除后的顶点优先级信息重建SubmeshUnit
        /// 智能合并骨骼信息，处理bindPose冲突
        /// </summary>
        private SubmeshUnit BuildSubmeshUnitFromVertexInfos(List<VertexPriorityInfo> vertexInfos,
                                                             List<SubmeshUnit> originalUnits,
                                                             Material material)
        {
            if (vertexInfos.Count == 0)
                return null;

            // 步骤1: 构建统一的骨骼映射表
            var unifiedBoneMapping = BuildUnifiedBoneMapping(vertexInfos, originalUnits);

            // 步骤2: 重映射骨骼权重到统一的骨骼索引
            var remappedVertexInfos = RemapBoneWeights(vertexInfos, unifiedBoneMapping);

            // 步骤3: 创建合并后的网格
            var mergedMesh = new Mesh();
            mergedMesh.name = $"OcclusionCulledMesh_{material.name}";

            // 准备顶点数据
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uvs = new List<Vector2>();
            var boneWeights = new List<BoneWeight>();
            var triangles = new List<int>();

            // 构建顶点映射：原始(UnitIndex, VertexIndex) -> 新索引
            var vertexIndexMap = new Dictionary<(int unitIndex, int vertexIndex), int>();

            // 添加剔除后的顶点
            for (int i = 0; i < remappedVertexInfos.Count; i++)
            {
                var info = remappedVertexInfos[i];

                vertices.Add(info.WorldPosition);
                normals.Add(info.Normal);
                tangents.Add(info.Tangent);
                uvs.Add(info.UV);
                boneWeights.Add(info.BoneWeights);

                vertexIndexMap[(info.UnitIndex, info.VertexIndex)] = i;
            }

            // 重建三角形，只保留所有顶点都存在的三角形
            foreach (var originalUnit in originalUnits)
            {
                int unitIndex = originalUnits.IndexOf(originalUnit);

                for (int triIndex = 0; triIndex < originalUnit.Triangles.Count; triIndex += 3)
                {
                    var v0 = originalUnit.Triangles[triIndex];
                    var v1 = originalUnit.Triangles[triIndex + 1];
                    var v2 = originalUnit.Triangles[triIndex + 2];

                    // 检查三角形的所有顶点是否都在剔除后的顶点集合中
                    if (vertexIndexMap.TryGetValue((unitIndex, v0), out var newV0) &&
                        vertexIndexMap.TryGetValue((unitIndex, v1), out var newV1) &&
                        vertexIndexMap.TryGetValue((unitIndex, v2), out var newV2))
                    {
                        triangles.Add(newV0);
                        triangles.Add(newV1);
                        triangles.Add(newV2);
                    }
                }
            }

            // 将数据设置到Mesh中
            mergedMesh.SetVertices(vertices);
            mergedMesh.SetNormals(normals);
            mergedMesh.SetTangents(tangents);
            mergedMesh.SetUVs(0, uvs);
            mergedMesh.boneWeights = boneWeights.ToArray();
            mergedMesh.SetTriangles(triangles, 0);

            // 选择最高优先级的DressupType
            var highestDressupType = vertexInfos.OrderByDescending(info => info.Priority)
                                               .First().DressupType;

            Log.Info($"[MeshCombiner] Built merged unit with occlusion culling: {vertices.Count} vertices, " +
                     $"{triangles.Count / 3} triangles, {unifiedBoneMapping.FinalBones.Count} bones, DressupType: {highestDressupType}");

            // 使用SubmeshUnit构造函数创建新实例，传入合并后的骨骼信息
            return new SubmeshUnit(mergedMesh, 0, material, highestDressupType,
                                   unifiedBoneMapping.FinalBones.ToArray(),
                                   unifiedBoneMapping.FinalBindPoses.ToArray());
        }

        /// <summary>
        /// 构建统一的骨骼映射表，智能处理bindPose冲突
        /// </summary>
        private UnifiedBoneMapping BuildUnifiedBoneMapping(List<VertexPriorityInfo> vertexInfos, List<SubmeshUnit> originalUnits)
        {
            var mapping = new UnifiedBoneMapping();

            // 收集所有涉及的SubmeshUnit和它们的骨骼
            var involvedUnits = vertexInfos.Select(v => v.SourceUnit).Distinct().ToList();

            foreach (var unit in involvedUnits)
            {
                for (int i = 0; i < unit.Bones.Count; i++)
                {
                    var bone = unit.Bones[i];
                    var bindPose = unit.BindPoses[i];

                    // 检查这个骨骼是否已经在最终列表中
                    int existingIndex = mapping.FinalBones.IndexOf(bone);

                    if (existingIndex >= 0)
                    {
                        // 骨骼已存在，检查bindPose是否冲突
                        var existingBindPose = mapping.FinalBindPoses[existingIndex];

                        if (!MatrixApproximatelyEqual(existingBindPose, bindPose))
                        {
                            // bindPose冲突，根据优先级决定保留哪个
                            var existingUnitPriority = GetUnitMaxPriority(mapping.FinalBones[existingIndex], vertexInfos);
                            var currentUnitPriority = GetUnitMaxPriority(bone, vertexInfos);

                            if (currentUnitPriority > existingUnitPriority)
                            {
                                // 当前单元优先级更高，替换bindPose
                                mapping.FinalBindPoses[existingIndex] = bindPose;

                                Log.Info($"[MeshCombiner] Bone '{bone.name}' bindPose conflict resolved: " +
                                         $"Using higher priority bindPose (Priority: {currentUnitPriority:F1} > {existingUnitPriority:F1})");
                            }
                        }

                        // 记录映射关系
                        mapping.BoneIndexMapping[(unit, i)] = existingIndex;
                    }
                    else
                    {
                        // 新骨骼，直接添加
                        mapping.FinalBones.Add(bone);
                        mapping.FinalBindPoses.Add(bindPose);
                        mapping.BoneIndexMapping[(unit, i)] = mapping.FinalBones.Count - 1;
                    }
                }
            }

            Log.Info($"[MeshCombiner] Unified bone mapping: {mapping.FinalBones.Count} bones from {involvedUnits.Count} units");

            return mapping;
        }

        /// <summary>
        /// 获取指定骨骼在顶点信息中的最高优先级
        /// </summary>
        private float GetUnitMaxPriority(Transform bone, List<VertexPriorityInfo> vertexInfos)
        {
            float maxPriority = 0f;

            foreach (var vertexInfo in vertexInfos)
            {
                if (vertexInfo.SourceUnit.Bones.Contains(bone))
                {
                    maxPriority = Math.Max(maxPriority, vertexInfo.Priority);
                }
            }

            return maxPriority;
        }

        /// <summary>
        /// 重映射骨骼权重到统一的骨骼索引系统
        /// </summary>
        private List<VertexPriorityInfo> RemapBoneWeights(List<VertexPriorityInfo> vertexInfos, UnifiedBoneMapping mapping)
        {
            var remappedInfos = new List<VertexPriorityInfo>();

            foreach (var info in vertexInfos)
            {
                var newInfo = new VertexPriorityInfo
                {
                    WorldPosition = info.WorldPosition,
                    Normal = info.Normal,
                    Tangent = info.Tangent,
                    UV = info.UV,
                    UnitIndex = info.UnitIndex,
                    VertexIndex = info.VertexIndex,
                    DressupType = info.DressupType,
                    Priority = info.Priority,
                    SourceUnit = info.SourceUnit,
                    BoneWeights = RemapSingleBoneWeight(info.BoneWeights, info.SourceUnit, mapping)
                };

                remappedInfos.Add(newInfo);
            }

            return remappedInfos;
        }

        /// <summary>
        /// 重映射单个顶点的骨骼权重
        /// </summary>
        private BoneWeight RemapSingleBoneWeight(BoneWeight originalWeight, SubmeshUnit sourceUnit, UnifiedBoneMapping mapping)
        {
            var newWeight = new BoneWeight();

            // 重映射boneIndex0
            if (originalWeight.weight0 > 0 && originalWeight.boneIndex0 < sourceUnit.Bones.Count)
            {
                if (mapping.BoneIndexMapping.TryGetValue((sourceUnit, originalWeight.boneIndex0), out var newIndex0))
                {
                    newWeight.boneIndex0 = newIndex0;
                    newWeight.weight0 = originalWeight.weight0;
                }
            }

            // 重映射boneIndex1
            if (originalWeight.weight1 > 0 && originalWeight.boneIndex1 < sourceUnit.Bones.Count)
            {
                if (mapping.BoneIndexMapping.TryGetValue((sourceUnit, originalWeight.boneIndex1), out var newIndex1))
                {
                    newWeight.boneIndex1 = newIndex1;
                    newWeight.weight1 = originalWeight.weight1;
                }
            }

            // 重映射boneIndex2
            if (originalWeight.weight2 > 0 && originalWeight.boneIndex2 < sourceUnit.Bones.Count)
            {
                if (mapping.BoneIndexMapping.TryGetValue((sourceUnit, originalWeight.boneIndex2), out var newIndex2))
                {
                    newWeight.boneIndex2 = newIndex2;
                    newWeight.weight2 = originalWeight.weight2;
                }
            }

            // 重映射boneIndex3
            if (originalWeight.weight3 > 0 && originalWeight.boneIndex3 < sourceUnit.Bones.Count)
            {
                if (mapping.BoneIndexMapping.TryGetValue((sourceUnit, originalWeight.boneIndex3), out var newIndex3))
                {
                    newWeight.boneIndex3 = newIndex3;
                    newWeight.weight3 = originalWeight.weight3;
                }
            }

            return newWeight;
        }

        /// <summary>
        /// 合并多个SubmeshUnit (传统方法，不进行遮挡剔除)级信息 - 用于处理重叠顶点的遮挡关系
        /// </summary>
        /// <summary>
        /// 顶点优先级信息，用于遮挡剔除计算
        /// </summary>
        private class VertexPriorityInfo
        {
            public Vector3 WorldPosition { get; set; }
            public Vector3 Normal { get; set; }
            public Vector4 Tangent { get; set; }
            public Vector2 UV { get; set; }
            public BoneWeight BoneWeights { get; set; }
            public int UnitIndex { get; set; }
            public int VertexIndex { get; set; }
            public DressupType DressupType { get; set; }
            public float Priority { get; set; } // 优先级，数值越大越优先显示
            public SubmeshUnit SourceUnit { get; set; } // 源SubmeshUnit，用于获取骨骼信息
        }

        /// <summary>
        /// 统一骨骼映射信息
        /// </summary>
        private class UnifiedBoneMapping
        {
            public List<Transform> FinalBones { get; set; } = new();
            public List<Matrix4x4> FinalBindPoses { get; set; } = new();
            public Dictionary<(SubmeshUnit unit, int originalIndex), int> BoneIndexMapping { get; set; } = new();
        }

        /// <summary>
        /// 子网格单元 - 单个材质的网格数据
        /// 对于SkinnedMeshRenderer和MeshRenderer，统一转换为SkinnedMeshRenderer格式输出
        /// </summary>
        private class SubmeshUnit
        {
            private readonly Mesh _sourceMesh;
            private readonly int _submeshIndex;
            private readonly Material _material;
            private readonly bool _isSkinnedMesh;
            private readonly Matrix4x4 _transformMatrix;  // 用于静态网格的坐标转换
            private readonly DressupType _dressupType;     // 所属的换装类型，用于优先级判断
            private readonly List<Vector3> _vertices = new();
            private readonly List<Vector3> _normals = new();
            private readonly List<Vector4> _tangents = new();
            private readonly List<Vector2> _uvs = new();
            private readonly List<BoneWeight> _boneWeights = new();
            private readonly List<int> _triangles = new();

            // 骨骼和bindPose信息 - 每个SubmeshUnit保存自己的骨骼绑定
            private readonly List<Transform> _bones = new();
            private readonly List<Matrix4x4> _bindPoses = new();

            public Material Material => _material;
            public DressupType DressupType => _dressupType;
            public List<Vector3> Vertices => _vertices;
            public List<Vector3> Normals => _normals;
            public List<Vector4> Tangents => _tangents;
            public List<Vector2> Uvs => _uvs;
            public List<BoneWeight> BoneWeights => _boneWeights;
            public List<int> Triangles => _triangles;
            public List<Transform> Bones => _bones;
            public List<Matrix4x4> BindPoses => _bindPoses;

            public bool IsValid => _vertices.Count > 0 && _triangles.Count > 0;
            public int VertexCount => _vertices.Count;

            public SubmeshUnit(Mesh sourceMesh, int submeshIndex, Material material, DressupType dressupType, Transform[] bones = null, Matrix4x4[] bindPoses = null)
            {
                _sourceMesh = sourceMesh;
                _submeshIndex = submeshIndex;
                _material = material;
                _isSkinnedMesh = true;
                _dressupType = dressupType;
                _transformMatrix = Matrix4x4.identity;

                // 存储骨骼和bindPose信息
                if (bones != null && bindPoses != null && bones.Length == bindPoses.Length)
                {
                    _bones.AddRange(bones);
                    _bindPoses.AddRange(bindPoses);
                }

                InitData();
            }

            // 为静态MeshRenderer使用的构造函数，会进行坐标转换，统一到骨骼空间
            public SubmeshUnit(Mesh sourceMesh, int submeshIndex, Material material, Transform meshTransform, Transform targetRootBone, DressupType dressupType)
            {
                _sourceMesh = sourceMesh;
                _submeshIndex = submeshIndex;
                _material = material;
                _isSkinnedMesh = false;
                _dressupType = dressupType;
                // 计算从静态网格Transform到目标根骨骼的变换矩阵
                if (targetRootBone == null)
                    _transformMatrix = Matrix4x4.identity;
                else
                    _transformMatrix = targetRootBone.worldToLocalMatrix * meshTransform.localToWorldMatrix;

                // 静态网格默认绑定到根骨骼
                if (targetRootBone != null)
                {
                    _bones.Add(targetRootBone);
                    _bindPoses.Add(targetRootBone.worldToLocalMatrix);
                }

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
                if (_isSkinnedMesh && !hasBoneWeights)
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

                    // 顶点位置（必须存在）- 如果是静态网格，应用坐标变换
                    Vector3 vertex = _sourceMesh.vertices[oldIndex];
                    if (!_isSkinnedMesh)
                    {
                        vertex = _transformMatrix.MultiplyPoint3x4(vertex);
                    }
                    _vertices.Add(vertex);

                    // 法线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasNormals)
                    {
                        Vector3 normal = _sourceMesh.normals[oldIndex];
                        if (!_isSkinnedMesh)
                        {
                            // 法线需要使用逆转置矩阵变换
                            normal = _transformMatrix.inverse.transpose.MultiplyVector(normal).normalized;
                        }
                        _normals.Add(normal);
                    }

                    // 切线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasTangents)
                    {
                        Vector4 tangent = _sourceMesh.tangents[oldIndex];
                        if (!_isSkinnedMesh)
                        {
                            // 切线的xyz部分需要变换，w分量保持不变
                            Vector3 tangentDir = _transformMatrix.MultiplyVector(new Vector3(tangent.x, tangent.y, tangent.z)).normalized;
                            tangent = new Vector4(tangentDir.x, tangentDir.y, tangentDir.z, tangent.w);
                        }
                        _tangents.Add(tangent);
                    }

                    // UV坐标（无法自动计算，使用默认值，可能影响材质渲染效果）
                    if (hasUV)
                    {
                        _uvs.Add(_sourceMesh.uv[oldIndex]);
                    }
                    else
                    {
                        _uvs.Add(Vector2.zero);
                    }

                    // 骨骼权重（无法自动计算，使用默认值，可能影响材质渲染效果）
                    if (_isSkinnedMesh && hasBoneWeights)
                    {
                        // 动态网格：使用原有的骨骼权重
                        _boneWeights.Add(_sourceMesh.boneWeights[oldIndex]);
                    }
                    else
                    {
                        // 静态网格或缺失骨骼权重的动态网格：使用默认权重（绑定到根骨骼，权重为1）
                        _boneWeights.Add(new BoneWeight { weight0 = 1.0f, boneIndex0 = 0 });
                    }
                }

                // 三角形索引重映射
                foreach (var triangle in triangles)
                {
                    _triangles.Add(vertexRemapping[triangle]);
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
            public Dictionary<Material, int> SubmeshMap;
            public Transform[] Bones;
            public Matrix4x4[] BindPoses;
            public Transform RootBone;
        }

        #endregion

        #region 私有字段

        private Transform _rootBone;
        private List<Transform> _combinedBones;
        private List<Matrix4x4> _combinedBindPoses;

        #endregion

        #region 公共接口

        /// <summary>
        /// 合并所有换装部件为一个大网格（仅支持同骨骼）
        /// </summary>
        public CombineResult Combine(List<DressupItem> items)
        {
            if (items == null || items.Count == 0)
            {
                Log.Warning("[MeshCombiner] No items to combine");
                return new CombineResult { Success = false };
            }

            // 先确定根骨骼
            DetermineRootBone(items);

            var submeshUnits = ExtractSubmeshUnits(items);
            ExtractCombinedBones(submeshUnits);
            var mergedSubmeshUnits = MergeSubmeshUnitsByMaterial(submeshUnits);
            var finalMesh = BuildFinalMesh(mergedSubmeshUnits);

            return finalMesh;
        }

        /// <summary>
        /// 确定根骨骼
        /// </summary>
        private void DetermineRootBone(List<DressupItem> items)
        {
            _rootBone = null;
            // 寻找一个有效的根骨骼作为参考（来自任何一个SkinnedMeshRenderer）
            foreach (var item in items)
            {
                if (item.IsValid && item.IsSkinnedMesh && item.RootBone != null)
                {
                    _rootBone = item.RootBone;
                    break;
                }
            }
        }

        #endregion

        #region 核心实现

        /// <summary>
        /// 提取最终的合并骨骼 - 从SubmeshUnit中收集
        /// </summary>
        private void ExtractCombinedBones(List<SubmeshUnit> submeshUnits)
        {
            _combinedBones = new List<Transform>();
            _combinedBindPoses = new List<Matrix4x4>();

            // 从所有SubmeshUnit中收集骨骼信息
            foreach (var unit in submeshUnits)
            {
                for (int i = 0; i < unit.Bones.Count; i++)
                {
                    RegisterBone(unit.Bones[i], unit.BindPoses[i]);
                }
            }

            // 确定根骨骼
            _rootBone = _combinedBones.FirstOrDefault();
        }

        /// <summary>
        /// 注册单个骨骼，处理重复和bindPose冲突
        /// </summary>

        private void RegisterBone(Transform bone, Matrix4x4 bindPose)
        {
            if (bone == null)
            {
                Log.Error("[MeshCombiner] Attempted to register a null bone.");
                return;
            }

            // 检查是否已经存在相同的骨骼
            int existingIndex = _combinedBones.IndexOf(bone);
            if (existingIndex >= 0)
            {
                // 验证bindPose是否一致
                Matrix4x4 existingBindPose = _combinedBindPoses[existingIndex];
                if (!MatrixApproximatelyEqual(existingBindPose, bindPose))
                {
                    Log.Warning($"[MeshCombiner] Bone '{bone.name}' has conflicting bindPoses. " +
                              $"This may indicate overlapping meshes with different binding. Using first bindPose.");
                }
                return;
            }

            _combinedBones.Add(bone);
            _combinedBindPoses.Add(bindPose);
        }

        /// <summary>
        /// 矩阵近似相等比较
        /// </summary>
        private bool MatrixApproximatelyEqual(Matrix4x4 a, Matrix4x4 b, float tolerance = 0.001f)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Mathf.Abs(a[i] - b[i]) > tolerance)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 步骤1: 拆分所有实例的子网格
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
                    SubmeshUnit unit;

                    if (item.IsSkinnedMesh)
                    {
                        unit = new SubmeshUnit(mesh, submeshIndex, material, item.DressupType, item.Bones, item.Mesh.bindposes);
                    }
                    else
                    {
                        unit = new SubmeshUnit(mesh, submeshIndex, material, item.Renderer.transform, _rootBone, item.DressupType);
                    }

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
        /// 将所有相同材质的SubmeshUnit合并成一个SubmeshUnit，并处理顶点遮挡
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
                    // 多个单元需要合并，应用顶点遮挡剔除
                    var mergedUnit = MergeSubmeshUnitsWithOcclusionCulling(unitsToMerge, material);
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
            return new SubmeshUnit(mergedMesh, 0, material, DressupType.None);
        }

        /// <summary>
        /// 步骤3: 按最终的子网格数量重建新的包含多个子网格的大网格
        /// </summary>
        private CombineResult BuildFinalMesh(List<SubmeshUnit> submeshUnits)
        {
            var result = new CombineResult { Success = false };

            if (submeshUnits.Count == 0)
            {
                Log.Warning("[MeshCombiner] No submesh units to build final mesh.");
                return result;
            }

            // 重新收集最终的骨骼信息（因为可能经过了遮挡剔除）
            _combinedBones.Clear();
            _combinedBindPoses.Clear();
            foreach (var unit in submeshUnits)
            {
                for (int i = 0; i < unit.Bones.Count; i++)
                {
                    RegisterBone(unit.Bones[i], unit.BindPoses[i]);
                }
            }

            // 合并所有SubmeshUnit的数据
            var finalVertices = new List<Vector3>();
            var finalNormals = new List<Vector3>();
            var finalTangents = new List<Vector4>();
            var finalUVs = new List<Vector2>();
            var finalBoneWeights = new List<BoneWeight>();
            var finalTriangles = new List<int>[submeshUnits.Count];
            var finalMaterials = new Material[submeshUnits.Count];
            var submeshMap = new Dictionary<Material, int>();

            int vertexOffset = 0;

            for (int i = 0; i < submeshUnits.Count; i++)
            {
                var unit = submeshUnits[i];
                finalTriangles[i] = new List<int>();
                finalMaterials[i] = unit.Material;
                submeshMap[unit.Material] = i;

                // 添加顶点数据
                finalVertices.AddRange(unit.Vertices);
                finalNormals.AddRange(unit.Normals);
                finalTangents.AddRange(unit.Tangents);
                finalUVs.AddRange(unit.Uvs);
                finalBoneWeights.AddRange(unit.BoneWeights);

                // 重映射三角形索引
                foreach (int triangleIndex in unit.Triangles)
                {
                    finalTriangles[i].Add(triangleIndex + vertexOffset);
                }

                vertexOffset += unit.VertexCount;
            }

            // 创建最终网格
            var mesh = CreateFinalCombinedMesh(finalVertices, finalNormals, finalTangents, finalUVs, finalBoneWeights, finalTriangles);

            if (mesh != null)
            {
                result.Success = true;
                result.CombinedMesh = mesh;
                result.CombinedMaterials = finalMaterials;
                result.Bones = _combinedBones.ToArray();
                result.BindPoses = _combinedBindPoses.ToArray();
                result.RootBone = _rootBone;
                result.SubmeshMap = submeshMap;

                Log.Info($"[XFramework] [MeshCombiner] Successfully built final mesh with {submeshUnits.Count} submeshes, " +
                         $"{finalVertices.Count} vertices, {finalTriangles.Sum(t => t.Count)} triangles, " +
                         $"{_combinedBones.Count} bones");
            }

            return result;
        }

        /// <summary>
        /// 创建最终的合并网格
        /// </summary>
        private Mesh CreateFinalCombinedMesh(List<Vector3> vertices, List<Vector3> normals, List<Vector4> tangents, List<Vector2> uvs,
            List<BoneWeight> boneWeights, List<int>[] triangles)
        {
            try
            {
                var mesh = new Mesh
                {
                    name = "CombinedMesh"
                };

                // 设置顶点数据 
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTangents(tangents);
                mesh.SetUVs(0, uvs);

                // 设置子网格
                mesh.subMeshCount = triangles.Length;
                for (int i = 0; i < triangles.Length; i++)
                {
                    if (triangles[i].Count > 0)
                    {
                        mesh.SetTriangles(triangles[i], i);
                    }
                }

                // 设置骨骼权重
                mesh.boneWeights = boneWeights.ToArray();

                // 设置绑定姿势
                mesh.bindposes = _combinedBindPoses.ToArray();

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
