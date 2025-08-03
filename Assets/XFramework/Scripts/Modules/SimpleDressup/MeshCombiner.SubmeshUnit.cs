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
        public struct SubmeshUnit
        {
            private int[] _triangles;
            private Vector3[] _vertices;
            private Vector3[] _normals;
            private Vector4[] _tangents;
            private Vector2[] _uvs;
            private BoneWeight[] _boneWeights;

            public readonly int[] Triangles => _triangles;
            public readonly Vector3[] Vertices => _vertices;
            public readonly Vector3[] Normals => _normals;
            public readonly Vector4[] Tangents => _tangents;
            public readonly Vector2[] Uvs => _uvs;
            public readonly BoneWeight[] BoneWeights => _boneWeights;

            public readonly bool IsValid => _vertices?.Length > 0 && _triangles?.Length > 0;
            public readonly int VertexCount => _vertices?.Length ?? 0;
            public readonly int TriangleIndexCount => _triangles?.Length ?? 0;

            public static SubmeshUnit Create(Mesh sourceMesh, int submeshIndex)
            {
                if (sourceMesh == null) throw new ArgumentNullException(nameof(sourceMesh));

                var sourceVertices = sourceMesh.vertices;
                var sourceNormals = sourceMesh.normals;
                var sourceTangents = sourceMesh.tangents;
                var sourceUVs = sourceMesh.uv;
                var sourceBoneWeights = sourceMesh.boneWeights;
                var sourceSubtriangles = sourceMesh.GetTriangles(submeshIndex);

                return Create(sourceVertices, sourceNormals, sourceTangents, sourceUVs, sourceBoneWeights, sourceSubtriangles);
            }

            public static SubmeshUnit Create(Vector3[] sourceVertices, Vector3[] sourceNormals, Vector4[] sourceTangents,
                Vector2[] sourceUVs, BoneWeight[] sourceBoneWeights, int[] sourceSubtriangles)
            {
                var unit = new SubmeshUnit();

                int sourceVertexCount = sourceVertices?.Length ?? 0;

                bool hasNormals = sourceNormals != null && sourceNormals.Length == sourceVertexCount;
                bool hasTangents = sourceTangents != null && sourceTangents.Length == sourceVertexCount;
                bool hasUV = sourceUVs != null && sourceUVs.Length == sourceVertexCount;
                bool hasBoneWeights = sourceBoneWeights != null && sourceBoneWeights.Length == sourceVertexCount;

                if (!hasNormals)
                    Log.Debug($"[MeshCombiner] Source mesh missing normals, will recalculate.");
                if (!hasTangents)
                    Log.Debug($"[MeshCombiner] Source mesh missing tangents, will recalculate.");
                if (!hasUV)
                    Log.Warning($"[MeshCombiner] Source mesh missing UV coordinates, using default values. This may affect texture rendering.");
                if (!hasBoneWeights)
                    Log.Warning($"[MeshCombiner] Source mesh missing bone weights, using default weights. This may affect skinning.");

                // 提取新旧顶点索引映射 (原索引 -> 0, 1, 2, ...)
                int usedVertexCount = 0;
                int[] newIndexToOld;
                Dictionary<int, int> oldIndexToNew;
                // 获取索引范围
                int maxVertexIndex = 0;
                for (int i = 0; i < sourceSubtriangles.Length; i++)
                {
                    if (sourceSubtriangles[i] > maxVertexIndex)
                        maxVertexIndex = sourceSubtriangles[i];
                }
                // 根据索引范围使用不同提取算法
                if (maxVertexIndex < 10000)  // 小范围使用bool数组标记
                {
                    var vertexUseFlags = new bool[maxVertexIndex + 1];

                    for (int i = 0; i < sourceSubtriangles.Length; i++)
                    {
                        int vertexIndex = sourceSubtriangles[i];
                        if (!vertexUseFlags[vertexIndex])
                        {
                            vertexUseFlags[vertexIndex] = true;
                            usedVertexCount++;
                        }
                    }

                    newIndexToOld = new int[usedVertexCount];
                    oldIndexToNew = new Dictionary<int, int>(usedVertexCount);
                    int newIndex = 0;
                    for (int i = 0; i < vertexUseFlags.Length; i++)
                    {
                        if (vertexUseFlags[i])
                        {
                            newIndexToOld[newIndex] = i;
                            oldIndexToNew[i] = newIndex;
                            newIndex++;
                        }
                    }
                }
                else  // 大范围使用排序去重
                {
                    Array.Sort(sourceSubtriangles);

                    usedVertexCount = 1; // 包含第一个顶点
                    for (int i = 1; i < sourceSubtriangles.Length; i++)
                    {
                        if (sourceSubtriangles[i] != sourceSubtriangles[i - 1])
                            usedVertexCount++;
                    }

                    newIndexToOld = new int[usedVertexCount];
                    oldIndexToNew = new Dictionary<int, int>(usedVertexCount);

                    newIndexToOld[0] = sourceSubtriangles[0];
                    oldIndexToNew[sourceSubtriangles[0]] = 0;

                    int newIndex = 1;
                    for (int i = 1; i < sourceSubtriangles.Length; i++)
                    {
                        if (sourceSubtriangles[i] != sourceSubtriangles[i - 1])
                        {
                            newIndexToOld[newIndex] = sourceSubtriangles[i];
                            oldIndexToNew[sourceSubtriangles[i]] = newIndex;
                            newIndex++;
                        }
                    }
                }

                unit._triangles = new int[sourceSubtriangles.Length];
                unit._vertices = new Vector3[usedVertexCount];
                unit._normals = new Vector3[usedVertexCount];
                unit._tangents = new Vector4[usedVertexCount];
                unit._uvs = new Vector2[usedVertexCount];
                unit._boneWeights = new BoneWeight[usedVertexCount];

                // 复制顶点数据
                for (int newIndex = 0; newIndex < newIndexToOld.Length; newIndex++)
                {
                    int oldIndex = newIndexToOld[newIndex];

                    // 顶点位置
                    unit._vertices[newIndex] = sourceVertices[oldIndex];

                    // 法线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasNormals)
                        unit._normals[newIndex] = sourceNormals[oldIndex];

                    // 切线数据（如果不存在或损坏，会在后续重新计算）
                    if (hasTangents)
                        unit._tangents[newIndex] = sourceTangents[oldIndex];

                    // UV坐标（无法自动计算，使用默认值，可能影响材质渲染效果）
                    if (hasUV)
                        unit._uvs[newIndex] = sourceUVs[oldIndex];
                    else
                        unit._uvs[newIndex] = Vector2.zero;

                    // 骨骼权重（无法自动计算，使用默认值，可能影响蒙皮效果）
                    if (hasBoneWeights)
                        unit._boneWeights[newIndex] = sourceBoneWeights[oldIndex];
                    else
                        unit._boneWeights[newIndex] = new BoneWeight { weight0 = 1.0f, boneIndex0 = 0 };
                }

                // 三角形索引重映射
                for (int i = 0; i < sourceSubtriangles.Length; i++)
                {
                    unit._triangles[i] = oldIndexToNew[sourceSubtriangles[i]];
                }

                return unit;
            }
        }
    }
}