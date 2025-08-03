using System;
using System.Collections.Generic;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.SimpleDressup
{
    public partial class MeshCombiner
    {
        /// <summary>
        /// 子网格单元
        /// </summary>
        private struct SubmeshUnit
        {
            private Material _material;
            private int[] _triangles;
            private Vector3[] _vertices;
            private Vector3[] _normals;
            private Vector4[] _tangents;
            private Vector2[] _uvs;
            private BoneWeight[] _boneWeights;

            public readonly Material Material => _material;
            public readonly int[] Triangles => _triangles;
            public readonly Vector3[] Vertices => _vertices;
            public readonly Vector3[] Normals => _normals;
            public readonly Vector4[] Tangents => _tangents;
            public readonly Vector2[] Uvs => _uvs;
            public readonly BoneWeight[] BoneWeights => _boneWeights;

            public readonly bool IsValid => _vertices?.Length > 0 && _triangles?.Length > 0;
            public readonly int VertexCount => _vertices?.Length ?? 0;

            public static SubmeshUnit Create(Mesh sourceMesh, int submeshIndex, Material material)
            {
                if (sourceMesh == null) throw new ArgumentNullException(nameof(sourceMesh));

                var unit = new SubmeshUnit
                {
                    _material = material
                };

                var sourceVertices = sourceMesh.vertices;
                var sourceNormals = sourceMesh.normals;
                var sourceTangents = sourceMesh.tangents;
                var sourceUVs = sourceMesh.uv;
                var sourceBoneWeights = sourceMesh.boneWeights;
                var sourceVertexCount = sourceMesh.vertexCount;

                // 检查源网格数据完整性
                bool hasNormals = sourceNormals != null && sourceNormals.Length == sourceVertexCount;
                bool hasTangents = sourceTangents != null && sourceTangents.Length == sourceVertexCount;
                bool hasUV = sourceUVs != null && sourceUVs.Length == sourceVertexCount;
                bool hasBoneWeights = sourceBoneWeights != null && sourceBoneWeights.Length == sourceVertexCount;

                if (!hasNormals)
                    Log.Debug($"[MeshCombiner] Mesh '{sourceMesh.name}' missing normals, will recalculate.");
                if (!hasTangents)
                    Log.Debug($"[MeshCombiner] Mesh '{sourceMesh.name}' missing tangents, will recalculate.");
                if (!hasUV)
                    Log.Warning($"[MeshCombiner] Mesh '{sourceMesh.name}' missing UV coordinates, using default values. This may affect texture rendering.");
                if (!hasBoneWeights)
                    Log.Warning($"[MeshCombiner] Mesh '{sourceMesh.name}' missing bone weights, using default weights. This may affect skinning.");

                // 提取使用的顶点索引和新旧顶点索引映射 (原索引 -> 0, 1, 2, ...)
                var subTriangles = sourceMesh.GetTriangles(submeshIndex);
                int usedVertexCount = 0;
                int[] usedVertexIndices;
                Dictionary<int, int> vertexIndexMap;
                // 获取索引范围
                int maxVertexIndex = 0;
                for (int i = 0; i < subTriangles.Length; i++)
                {
                    if (subTriangles[i] > maxVertexIndex)
                        maxVertexIndex = subTriangles[i];
                }
                // 根据索引范围使用不同提取算法
                if (maxVertexIndex < 10000)  // 小范围使用bool数组标记
                {
                    var vertexUseFlags = new bool[maxVertexIndex + 1];

                    for (int i = 0; i < subTriangles.Length; i++)
                    {
                        int vertexIndex = subTriangles[i];
                        if (!vertexUseFlags[vertexIndex])
                        {
                            vertexUseFlags[vertexIndex] = true;
                            usedVertexCount++;
                        }
                    }

                    usedVertexIndices = new int[usedVertexCount];
                    vertexIndexMap = new Dictionary<int, int>(usedVertexCount);
                    int newIndex = 0;
                    for (int i = 0; i < vertexUseFlags.Length; i++)
                    {
                        if (vertexUseFlags[i])
                        {
                            usedVertexIndices[newIndex] = i;
                            vertexIndexMap[i] = newIndex;
                            newIndex++;
                        }
                    }
                }
                else  // 大范围使用排序去重
                {
                    Array.Sort(subTriangles);

                    usedVertexCount = 1; // 包含第一个顶点
                    for (int i = 1; i < subTriangles.Length; i++)
                    {
                        if (subTriangles[i] != subTriangles[i - 1])
                            usedVertexCount++;
                    }

                    usedVertexIndices = new int[usedVertexCount];
                    vertexIndexMap = new Dictionary<int, int>(usedVertexCount);

                    usedVertexIndices[0] = subTriangles[0];
                    vertexIndexMap[subTriangles[0]] = 0;

                    int newIndex = 1;
                    for (int i = 1; i < subTriangles.Length; i++)
                    {
                        if (subTriangles[i] != subTriangles[i - 1])
                        {
                            // 新顶点索引
                            usedVertexIndices[newIndex] = subTriangles[i];
                            vertexIndexMap[subTriangles[i]] = newIndex;
                            newIndex++;
                        }
                    }
                }

                unit._triangles = new int[subTriangles.Length];
                unit._vertices = new Vector3[usedVertexCount];
                unit._normals = new Vector3[usedVertexCount];
                unit._tangents = new Vector4[usedVertexCount];
                unit._uvs = new Vector2[usedVertexCount];
                unit._boneWeights = new BoneWeight[usedVertexCount];

                // 复制顶点数据
                for (int i = 0; i < usedVertexIndices.Length; i++)
                {
                    int oldIndex = usedVertexIndices[i];

                    // 顶点位置
                    unit._vertices[i] = sourceVertices[oldIndex];

                    // 法线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasNormals)
                        unit._normals[i] = sourceNormals[oldIndex];

                    // 切线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasTangents)
                        unit._tangents[i] = sourceTangents[oldIndex];

                    // UV坐标（无法自动计算，使用默认值，可能影响材质渲染效果）
                    if (hasUV)
                        unit._uvs[i] = sourceUVs[oldIndex];
                    else
                        unit._uvs[i] = Vector2.zero;

                    // 骨骼权重（无法自动计算，使用默认值，可能影响蒙皮效果）
                    if (hasBoneWeights)
                        unit._boneWeights[i] = sourceBoneWeights[oldIndex];
                    else
                        unit._boneWeights[i] = new BoneWeight { weight0 = 1.0f, boneIndex0 = 0 };
                }

                // 三角形索引重映射
                for (int i = 0; i < subTriangles.Length; i++)
                {
                    unit._triangles[i] = vertexIndexMap[subTriangles[i]];
                }

                return unit;
            }
        }
    }
}