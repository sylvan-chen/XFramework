using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using XFramework.Utils;

namespace SimpleDressup
{
    /// <summary>
    /// 网格合并器 - 负责将多个 SkinnedMeshRenderer 合并成单一网格
    /// 处理顶点数据、骨骼权重、UV重映射等
    /// </summary>
    public class MeshCombiner
    {
        /// <summary>
        /// 合并实例 - 代表一个待合并的网格
        /// </summary>
        public struct CombineInstance
        {
            public DressupMesh MeshData;           // 网格数据
            public Material[] Materials;          // 材质数组
            public int[] TargetSubmeshIndices;    // 目标子网格索引映射

            public CombineInstance(DressupMesh meshData, Material[] materials)
            {
                MeshData = meshData;
                Materials = materials;
                TargetSubmeshIndices = new int[meshData?.SubMeshes?.Length ?? 0];
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
            public Transform[] Bones;
            public Matrix4x4[] BindPoses;
            public Transform RootBone;
        }

        private List<Vector3> _combinedVertices;
        private List<Vector3> _combinedNormals;
        private List<Vector2> _combinedUVs;
        private List<BoneWeight> _combinedBoneWeights;
        private List<int>[] _combinedTriangles;  // 每个子网格的三角形

        private List<Transform> _combinedBones;
        private List<Matrix4x4> _combinedBindPoses;
        private Dictionary<Transform, int> _boneIndexMap;

        /// <summary>
        /// 合并多个网格实例
        /// </summary>
        public CombineResult CombineMeshes(List<CombineInstance> instances, Transform rootBone = null)
        {
            var result = new CombineResult { Success = false };

            if (instances == null || instances.Count == 0)
            {
                Log.Warning("MeshCombiner: 合并实例列表为空");
                return result;
            }

            Log.Debug($"MeshCombiner: 开始合并 {instances.Count} 个网格");

            try
            {
                // 1. 初始化数据结构
                InitializeCombineData(instances);

                // 2. 构建骨骼映射
                BuildBoneMapping(instances, rootBone);

                // 3. 合并网格数据
                CombineMeshData(instances);

                // 4. 创建最终网格
                var mesh = CreateCombinedMesh();
                if (mesh != null)
                {
                    result.Success = true;
                    result.CombinedMesh = mesh;
                    result.CombinedMaterials = CollectCombinedMaterials(instances);
                    result.Bones = _combinedBones.ToArray();
                    result.BindPoses = _combinedBindPoses.ToArray();
                    result.RootBone = rootBone ?? (_combinedBones.Count > 0 ? _combinedBones[0] : null);

                    Log.Debug($"MeshCombiner: 合并成功 - 顶点数:{_combinedVertices.Count}, 子网格数:{_combinedTriangles.Length}");
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"MeshCombiner: 合并过程出错 - {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// 初始化合并数据结构
        /// </summary>
        private void InitializeCombineData(List<CombineInstance> instances)
        {
            // 计算总顶点数和子网格数
            int totalVertices = instances.Sum(inst => inst.MeshData?.VertexCount ?? 0);
            int maxSubmeshes = instances.Max(inst => inst.Materials?.Length ?? 0);

            // 初始化顶点数据列表
            _combinedVertices = new List<Vector3>(totalVertices);
            _combinedNormals = new List<Vector3>(totalVertices);
            _combinedUVs = new List<Vector2>(totalVertices);
            _combinedBoneWeights = new List<BoneWeight>(totalVertices);

            // 初始化三角形数据
            _combinedTriangles = new List<int>[maxSubmeshes];
            for (int i = 0; i < maxSubmeshes; i++)
            {
                _combinedTriangles[i] = new List<int>();
            }

            // 初始化骨骼相关数据
            _combinedBones = new List<Transform>();
            _combinedBindPoses = new List<Matrix4x4>();
            _boneIndexMap = new Dictionary<Transform, int>();
        }

        /// <summary>
        /// 构建骨骼映射表
        /// </summary>
        private void BuildBoneMapping(List<CombineInstance> instances, Transform rootBone)
        {
            // 收集所有唯一的骨骼
            var allBones = new HashSet<Transform>();

            foreach (var instance in instances)
            {
                if (instance.MeshData?.Bones != null)
                {
                    foreach (var bone in instance.MeshData.Bones)
                    {
                        if (bone != null)
                        {
                            allBones.Add(bone);
                        }
                    }
                }
            }

            // 确保根骨骼在第一位
            if (rootBone != null && !allBones.Contains(rootBone))
            {
                allBones.Add(rootBone);
            }

            // 构建骨骼列表和映射表
            var sortedBones = allBones.OrderBy(bone => bone.name).ToList();
            if (rootBone != null && sortedBones.Contains(rootBone))
            {
                sortedBones.Remove(rootBone);
                sortedBones.Insert(0, rootBone);
            }

            for (int i = 0; i < sortedBones.Count; i++)
            {
                var bone = sortedBones[i];
                _combinedBones.Add(bone);
                _boneIndexMap[bone] = i;

                // 添加对应的绑定姿态
                AddBindPoseForBone(bone, instances);
            }

            Log.Debug($"MeshCombiner: 构建了 {_combinedBones.Count} 个骨骼的映射");
        }

        /// <summary>
        /// 为骨骼添加绑定姿态
        /// </summary>
        private void AddBindPoseForBone(Transform bone, List<CombineInstance> instances)
        {
            // 尝试从实例中找到该骨骼的绑定姿态
            foreach (var instance in instances)
            {
                if (instance.MeshData?.Bones != null && instance.MeshData.BindPoses != null)
                {
                    for (int i = 0; i < instance.MeshData.Bones.Length; i++)
                    {
                        if (instance.MeshData.Bones[i] == bone)
                        {
                            _combinedBindPoses.Add(instance.MeshData.BindPoses[i]);
                            return;
                        }
                    }
                }
            }

            // 如果没找到，使用单位矩阵
            _combinedBindPoses.Add(Matrix4x4.identity);
        }

        /// <summary>
        /// 合并网格数据
        /// </summary>
        private void CombineMeshData(List<CombineInstance> instances)
        {
            int vertexOffset = 0;

            foreach (var instance in instances)
            {
                if (instance.MeshData == null || !instance.MeshData.IsValid())
                    continue;

                var meshData = instance.MeshData;
                int instanceVertexCount = meshData.VertexCount;

                // 合并顶点数据
                CombineVertexData(meshData, vertexOffset);

                // 合并骨骼权重
                CombineBoneWeights(meshData, vertexOffset);

                // 合并三角形索引
                CombineTriangles(meshData, instance.TargetSubmeshIndices, vertexOffset);

                vertexOffset += instanceVertexCount;
            }
        }

        /// <summary>
        /// 合并顶点数据
        /// </summary>
        private void CombineVertexData(DressupMesh meshData, int vertexOffset)
        {
            // 合并顶点
            if (meshData.Vertices != null)
                _combinedVertices.AddRange(meshData.Vertices);

            // 合并法线
            if (meshData.Normals != null && meshData.Normals.Length > 0)
                _combinedNormals.AddRange(meshData.Normals);
            else
                _combinedNormals.AddRange(new Vector3[meshData.VertexCount]); // 填充默认法线

            // 合并UV
            if (meshData.UV != null && meshData.UV.Length > 0)
                _combinedUVs.AddRange(meshData.UV);
            else
                _combinedUVs.AddRange(new Vector2[meshData.VertexCount]); // 填充默认UV
        }

        /// <summary>
        /// 合并骨骼权重
        /// </summary>
        private void CombineBoneWeights(DressupMesh meshData, int vertexOffset)
        {
            if (meshData.BoneWeights == null || meshData.Bones == null)
            {
                // 如果没有骨骼权重，添加默认权重
                _combinedBoneWeights.AddRange(new BoneWeight[meshData.VertexCount]);
                return;
            }

            // 重映射骨骼权重
            for (int i = 0; i < meshData.BoneWeights.Length; i++)
            {
                var originalWeight = meshData.BoneWeights[i];
                var newWeight = new BoneWeight();

                // 重映射骨骼索引
                newWeight.boneIndex0 = RemapBoneIndex(originalWeight.boneIndex0, meshData.Bones);
                newWeight.boneIndex1 = RemapBoneIndex(originalWeight.boneIndex1, meshData.Bones);
                newWeight.boneIndex2 = RemapBoneIndex(originalWeight.boneIndex2, meshData.Bones);
                newWeight.boneIndex3 = RemapBoneIndex(originalWeight.boneIndex3, meshData.Bones);

                // 复制权重值
                newWeight.weight0 = originalWeight.weight0;
                newWeight.weight1 = originalWeight.weight1;
                newWeight.weight2 = originalWeight.weight2;
                newWeight.weight3 = originalWeight.weight3;

                _combinedBoneWeights.Add(newWeight);
            }
        }

        /// <summary>
        /// 重映射骨骼索引
        /// </summary>
        private int RemapBoneIndex(int originalIndex, Transform[] originalBones)
        {
            if (originalIndex < 0 || originalIndex >= originalBones.Length)
                return 0;

            var bone = originalBones[originalIndex];
            return _boneIndexMap.ContainsKey(bone) ? _boneIndexMap[bone] : 0;
        }

        /// <summary>
        /// 合并三角形索引
        /// </summary>
        private void CombineTriangles(DressupMesh meshData, int[] targetSubmeshIndices, int vertexOffset)
        {
            if (meshData.SubMeshes == null) return;

            for (int submeshIndex = 0; submeshIndex < meshData.SubMeshes.Length; submeshIndex++)
            {
                var submesh = meshData.SubMeshes[submeshIndex];
                int targetIndex = targetSubmeshIndices[submeshIndex];

                if (targetIndex >= _combinedTriangles.Length) continue;

                // 提取该子网格的三角形
                var triangles = ExtractSubmeshTriangles(meshData.Triangles, submesh);

                // 重映射顶点索引并添加到目标子网格
                foreach (int triangle in triangles)
                {
                    _combinedTriangles[targetIndex].Add(triangle + vertexOffset);
                }
            }
        }

        /// <summary>
        /// 提取子网格的三角形
        /// </summary>
        private int[] ExtractSubmeshTriangles(int[] allTriangles, DressupMesh.SubMeshInfo submesh)
        {
            var triangles = new int[submesh.IndexCount];
            System.Array.Copy(allTriangles, submesh.IndexStart, triangles, 0, submesh.IndexCount);
            return triangles;
        }

        /// <summary>
        /// 创建最终合并的网格
        /// </summary>
        private Mesh CreateCombinedMesh()
        {
            var mesh = new Mesh();
            mesh.name = "CombinedDressupMesh";

            try
            {
                // 设置顶点数据
                mesh.SetVertices(_combinedVertices);
                if (_combinedNormals.Count > 0)
                    mesh.SetNormals(_combinedNormals);
                if (_combinedUVs.Count > 0)
                    mesh.SetUVs(0, _combinedUVs);

                // 设置子网格和三角形
                mesh.subMeshCount = _combinedTriangles.Length;
                for (int i = 0; i < _combinedTriangles.Length; i++)
                {
                    if (_combinedTriangles[i].Count > 0)
                        mesh.SetTriangles(_combinedTriangles[i], i);
                }

                // 设置骨骼权重
                if (_combinedBoneWeights.Count > 0)
                    mesh.boneWeights = _combinedBoneWeights.ToArray();

                // 设置绑定姿态
                if (_combinedBindPoses.Count > 0)
                    mesh.bindposes = _combinedBindPoses.ToArray();

                // 重新计算边界和法线
                mesh.RecalculateBounds();
                if (_combinedNormals.Count == 0)
                    mesh.RecalculateNormals();

                return mesh;
            }
            catch (System.Exception e)
            {
                Log.Error($"MeshCombiner: 创建网格失败 - {e.Message}");
                if (mesh != null)
                    Object.DestroyImmediate(mesh);
                return null;
            }
        }

        /// <summary>
        /// 收集合并后的材质
        /// </summary>
        private Material[] CollectCombinedMaterials(List<CombineInstance> instances)
        {
            var materialList = new List<Material>();

            // 按子网格索引收集材质
            for (int submeshIndex = 0; submeshIndex < _combinedTriangles.Length; submeshIndex++)
            {
                Material selectedMaterial = null;

                // 找到第一个使用该子网格的材质
                foreach (var instance in instances)
                {
                    if (instance.Materials != null)
                    {
                        for (int i = 0; i < instance.TargetSubmeshIndices.Length; i++)
                        {
                            if (instance.TargetSubmeshIndices[i] == submeshIndex)
                            {
                                if (i < instance.Materials.Length && instance.Materials[i] != null)
                                {
                                    selectedMaterial = instance.Materials[i];
                                    break;
                                }
                            }
                        }
                    }

                    if (selectedMaterial != null) break;
                }

                materialList.Add(selectedMaterial);
            }

            return materialList.ToArray();
        }
    }
}
