using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using XFramework.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XFramework.SimpleDressup
{
    /// <summary>
    /// 简单换装系统主控制器
    /// </summary>
    /// <remarks>
    /// 负责协调整个换装流程：数据收集 → 图集生成 → 网格合并 → 结果应用
    /// 合并所有身体部件和外观部件的网格到同一个SkinnedMeshRenderer
    /// </remarks>
    public class SimpleDressupController : MonoBehaviour
    {
        [Header("基础配置")]
        [IntDropdown(256, 512, 1024, 2048, 4096)]
        [SerializeField] private int _atlasSize = 1024;
        [SerializeField] private bool _autoApplyOnStart = false;
        [SerializeField] private Shader _defaultShader;

        [Header("目标对象")]
        [SerializeField] private SkinnedMeshRenderer _targetRenderer;

        [Header("骨骼数据")]
        [SerializeField] private Transform _rootBone;

        [Header("外观部件")]
        [SerializeField] private List<DressupItem> _dressupItems;

        // 核心组件
        private MeshCombiner _meshCombiner;                         // 网格合并器
        private Mesh _combinedMesh;                                 // 合并后的网格

        private AtlasGenerator _atlasGenerator;                     // 图集生成器
        private Material _atlasMaterial;                            // 使用图集的材质

        // 骨骼数据
        private Transform[] _mainBones = new Transform[0];                // 主骨骼数组
        private Matrix4x4[] _bindPoses = new Matrix4x4[0];                // 绑定姿势矩阵
        private readonly Dictionary<string, Transform> _boneMap = new();  // 骨骼映射字典

        // 状态管理
        public bool IsInitialized { get; private set; } = false;
        public bool IsDressing { get; private set; } = false;

        /// <summary>
        /// 换装结果事件
        /// </summary>
        public System.Action<bool> OnDressupComplete;

#if UNITY_EDITOR
        public Material AtlasMaterial => _atlasMaterial;
#endif

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            if (_autoApplyOnStart)
            {
                ApplyDressupAsync().Forget();
            }
        }

        private void OnDestroy()
        {
            // 清理临时创建的材质
            if (_atlasMaterial != null && ShouldDestroyMaterial(_atlasMaterial))
            {
                DestroyImmediate(_atlasMaterial);
            }

            // 清理临时创建的网格
            if (_combinedMesh != null && ShouldDestroyMesh(_combinedMesh))
            {
                DestroyImmediate(_combinedMesh);
            }
        }

        private bool ShouldDestroyMaterial(Material material)
        {
            if (material == null) return false;

#if UNITY_EDITOR
            // 在编辑器中，检查材质是否是资源文件
            return !AssetDatabase.Contains(material);
#else
            return material.name.Contains("(Instance)") || material.name.Contains("(Clone)");
#endif
        }

        private bool ShouldDestroyMesh(Mesh mesh)
        {
            if (mesh == null) return false;

#if UNITY_EDITOR
            // 在编辑器中，检查网格是否是资源文件
            return !AssetDatabase.Contains(mesh);
#else
            return mesh.name.Contains("(Instance)") || mesh.name.Contains("(Clone)");
#endif
        }

        private void Init()
        {
            // 创建和初始化核心组件
            _meshCombiner = new MeshCombiner();
            _atlasGenerator = new AtlasGenerator();

            // 骨骼数据初始化
            _boneMap.Clear();
            _mainBones = _rootBone.GetComponentsInChildren<Transform>();
            _bindPoses = new Matrix4x4[_mainBones.Length];

            for (int i = 0; i < _mainBones.Length; i++)
            {
                var bone = _mainBones[i];
                if (bone == null) throw new System.ArgumentNullException($"Bone at index {i} is null in the hierarchy under {_rootBone.name}");

                // 骨骼绑定姿势
                _bindPoses[i] = _mainBones[i].worldToLocalMatrix * _rootBone.localToWorldMatrix;

                // 骨骼映射字典
                _boneMap[bone.name] = bone;
            }

            IsInitialized = true;
        }

        /// <summary>
        /// 应用当前的外观配置
        /// </summary>
        public async UniTask<bool> ApplyDressupAsync()
        {
            if (!IsInitialized)
            {
                Log.Error("[SimpleDressupController] Controller not initialized.");
                return false;
            }

            if (IsDressing)
            {
                Log.Warning("[SimpleDressupController] Dressing in progress, ignoring duplicate call.");
                return false;
            }

            if (_dressupItems.Count == 0)
            {
                Log.Warning("[SimpleDressupController] No valid dressup items to apply.");
                return false;
            }

            IsDressing = true;

            Log.Debug($"[SimpleDressupController] Start dressing process - {_dressupItems.Count} items.");

            // 1. 初始化所有部件
            InitDressupItems();

            // 2. 生成纹理图集
            bool atlasSuccess = await GenerateAndApplyAtlasAsync();
            if (!atlasSuccess)
            {
                Log.Error("[SimpleDressupController] Failed to generate texture atlas.");
                IsDressing = false;
                return false;
            }

            // 3. 合并网格
            bool meshSuccess = await CombineMeshesAsync();
            if (!meshSuccess)
            {
                Log.Error("[SimpleDressupController] Failed to combine meshes.");
                IsDressing = false;
                return false;
            }

            // 4. 应用到目标渲染器
            bool applySuccess = ApplyToTargetRenderer();
            if (!applySuccess)
            {
                Log.Error("[SimpleDressupController] Failed to apply to target renderer.");
                IsDressing = false;
                return false;
            }

            Log.Debug("[SimpleDressupController] Dressup process completed.");

            OnDressupComplete?.Invoke(true);
            IsDressing = false;
            return true;
        }

        /// <summary>
        /// 初始化当前所有外观部件
        /// </summary>
        private void InitDressupItems()
        {
            var invalidItems = new List<DressupItem>();

            foreach (var item in _dressupItems)
            {
                // 初始化
                item.Init();
                if (!item.IsValid)
                {
                    Log.Error($"[SimpleDressupController] Clothing item '{item.Renderer.name}' is invalid.");
                    invalidItems.Add(item);
                    continue;
                }
            }

            // 移除无效的部件
            foreach (var invalidItem in invalidItems)
            {
                _dressupItems.Remove(invalidItem);
            }
        }

        /// <summary>
        /// 生成纹理图集
        /// </summary>
        private async UniTask<bool> GenerateAndApplyAtlasAsync()
        {
            _atlasMaterial = await _atlasGenerator.BakeAtlasAndApplyAsync(_dressupItems, _atlasSize, _dressupItems[0].Renderer.sharedMaterial);

            return _atlasMaterial != null;
        }

        /// <summary>
        /// 合并网格
        /// </summary>
        private async UniTask<bool> CombineMeshesAsync()
        {
            if (_dressupItems.Count == 0)
            {
                Log.Warning("[SimpleDressupController] No valid items to combine.");
                return false;
            }

            await UniTask.NextFrame();

            _combinedMesh = await _meshCombiner.CombineMeshesAsync(_dressupItems, _bindPoses);

            if (_combinedMesh == null)
            {
                Log.Error("[SimpleDressupController] Failed to combine items.");
                return false;
            }

            Log.Debug($"[SimpleDressupController] Items combined successfully - {_dressupItems.Count} items → {_combinedMesh.subMeshCount} submeshes.");

            return true;
        }

        /// <summary>
        /// 应用到目标渲染器
        /// </summary>
        private bool ApplyToTargetRenderer()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = new GameObject("CombinedRenderer").AddComponent<SkinnedMeshRenderer>();
                _targetRenderer.transform.SetParent(transform);
            }

            // 应用合并的网格
            _targetRenderer.sharedMesh = _combinedMesh;
            // 应用材质
            var atlasMaterials = new Material[_combinedMesh.subMeshCount];
            for (int i = 0; i < atlasMaterials.Length; i++)
            {
                atlasMaterials[i] = _atlasMaterial;
            }
            _targetRenderer.sharedMaterials = atlasMaterials;
            // 应用骨骼
            _targetRenderer.bones = _mainBones;
            // 设置根骨骼
            _targetRenderer.rootBone = _rootBone;

            // 计算合适的本地边界
            CalculateAndSetLocalBounds(_targetRenderer);

            Log.Debug("[SimpleDressupController] Successfully applied to target renderer.");

            foreach (var item in _dressupItems)
            {
                if (item != null && item.Renderer != null)
                {
                    item.Renderer.enabled = false; // 禁用原部件的渲染器
                }
            }

            return true;
        }

        /// <summary>
        /// 计算并设置合适的本地边界
        /// 基于所有原始部件的localBounds计算合并后的边界
        /// </summary>
        private void CalculateAndSetLocalBounds(SkinnedMeshRenderer targetRenderer)
        {
            if (_dressupItems.Count == 0)
            {
                Log.Warning("[SimpleDressupController] No dressup items to calculate bounds from.");
                return;
            }

            // 计算所有部件的联合边界
            Bounds combinedBounds = new();
            bool firstBounds = true;

            foreach (var item in _dressupItems)
            {
                if (item?.Renderer != null)
                {
                    var itemBounds = item.Renderer.localBounds;

                    if (firstBounds)
                    {
                        combinedBounds = itemBounds;
                        firstBounds = false;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(itemBounds);
                    }
                }
            }

            if (!firstBounds)
            {
                targetRenderer.localBounds = combinedBounds;
            }
            else
            {
                Log.Warning("[SimpleDressupController] No valid bounds found from dressup items.");
            }
        }
    }
}
