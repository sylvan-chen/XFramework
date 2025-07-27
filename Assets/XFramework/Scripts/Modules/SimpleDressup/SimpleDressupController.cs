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
    /// 负责协调整个换装流程：数据收集→图集生成→网格合并→结果应用
    /// </summary>
    public class SimpleDressupController : MonoBehaviour
    {
        [Header("基础配置")]
        [IntDropdown(256, 512, 1024, 2048, 4096)]
        [SerializeField] private int _atlasSize = 1024;
        [SerializeField] private bool _autoApplyOnStart = false;

        [Header("目标对象")]
        [SerializeField] private SkinnedMeshRenderer _targetRenderer;

        [Header("换装部件")]
        [SerializeField] private List<DressupItem> _dressupItems;

        // 核心组件
        private MeshCombiner _meshCombiner;      // 网格合并器

        // 运行时数据
        private MeshCombiner.CombineResult _meshCombineResult;

        // 状态管理
        public bool IsInitialized { get; private set; } = false;
        public bool IsDressing { get; private set; } = false;

        /// <summary>
        /// 换装结果事件
        /// </summary>
        public System.Action<bool> OnDressupComplete;

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            if (_autoApplyOnStart)
            {
                ApplyCurrentDressupAsync().Forget();
            }
        }

        private void OnDestroy()
        {
            // 清理临时创建的材质 - 只清理运行时创建的材质，跳过资源文件材质
            if (_meshCombineResult.CombinedMaterials != null)
            {
                foreach (var mat in _meshCombineResult.CombinedMaterials)
                {
                    // 只销毁运行时创建的材质实例，不销毁资源文件中的材质
                    if (mat != null && ShouldDestroyMaterial(mat))
                    {
                        DestroyImmediate(mat);
                    }
                }
            }
            // 清理临时创建的网格
            if (_meshCombineResult.CombinedMesh != null && ShouldDestroyMesh(_meshCombineResult.CombinedMesh))
            {
                DestroyImmediate(_meshCombineResult.CombinedMesh);
            }
        }

        private bool ShouldDestroyMaterial(Material material)
        {
            if (material == null) return false;

#if UNITY_EDITOR
            // 在编辑器中，检查材质是否是资源文件
            return !AssetDatabase.Contains(material);
#else
            // 在运行时，检查材质名称是否包含 "Instance"
            // 运行时创建的材质通常会有 "Instance" 后缀
            return material.name.Contains("(Instance)");
#endif
        }

        private bool ShouldDestroyMesh(Mesh mesh)
        {
            if (mesh == null) return false;

#if UNITY_EDITOR
            // 在编辑器中，检查网格是否是资源文件
            return !AssetDatabase.Contains(mesh);
#else
            // 在运行时，检查网格名称是否包含 "Instance"
            // 运行时创建的网格通常会有 "Instance" 后缀
            return mesh.name.Contains("(Instance)");
#endif
        }

        private void Init()
        {
            _meshCombiner = new MeshCombiner();

            IsInitialized = true;

            // 初始化换装部件
            var invalidItems = new List<DressupItem>();
            for (int i = 0; i < _dressupItems.Count; i++)
            {
                var item = _dressupItems[i];
                item.Init();
                if (!item.IsValid)
                {
                    Log.Error($"[SimpleDressupController] Dressup item '{item.Renderer.name}' is invalid.");
                    invalidItems.Add(_dressupItems[i]);
                }
            }
            // 移除无效的部件
            foreach (var invalidItem in invalidItems)
            {
                _dressupItems.Remove(invalidItem);
            }
        }

        /// <summary>
        /// 应用当前的换装配置
        /// </summary>
        public async UniTask<bool> ApplyCurrentDressupAsync()
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

            try
            {
                Log.Debug($"[SimpleDressupController] Start dressing process - {_dressupItems.Count} items.");

                // 1. 生成纹理图集
                bool atlasSuccess = await GenerateTextureAtlasAsync();
                if (!atlasSuccess)
                {
                    Log.Error("[SimpleDressupController] Failed to generate texture atlas.");
                    return false;
                }

                // 2. 合并网格
                bool meshSuccess = await CombineMeshesAsync();
                if (!meshSuccess)
                {
                    Log.Error("[SimpleDressupController] Failed to combine meshes.");
                    return false;
                }

                // 3. 应用到目标渲染器
                bool applySuccess = ApplyToTargetRenderer();
                if (!applySuccess)
                {
                    Log.Error("[SimpleDressupController] Failed to apply to target renderer.");
                    return false;
                }

                Log.Debug("[SimpleDressupController] Dressup process completed.");

                OnDressupComplete?.Invoke(true);
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[SimpleDressupController] Dressup process error - {e.Message}");
                OnDressupComplete?.Invoke(false);
                return false;
            }
            finally
            {
                IsDressing = false;
            }
        }

        /// <summary>
        /// 生成纹理图集
        /// </summary>
        private async UniTask<bool> GenerateTextureAtlasAsync()
        {
            // TODO: 如果需要纹理合并，这里需要从DressupItem的Materials中提取纹理
            // 当前简化处理，直接返回成功
            await UniTask.NextFrame();
            return true;
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

            _meshCombineResult = _meshCombiner.Combine(_dressupItems);
            if (!_meshCombineResult.Success)
            {
                Log.Error("[SimpleDressupController] Failed to combine items.");
                return false;
            }

            Log.Debug($"[SimpleDressupController] Items combined successfully - {_dressupItems.Count} items → {_meshCombineResult.CombinedMaterials?.Length ?? 0} submeshes.");

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
            _targetRenderer.sharedMesh = _meshCombineResult.CombinedMesh;
            // 应用材质
            _targetRenderer.sharedMaterials = _meshCombineResult.CombinedMaterials;
            // 应用骨骼
            _targetRenderer.bones = _meshCombineResult.Bones;
            // 设置根骨骼
            _targetRenderer.rootBone = _meshCombineResult.RootBone;

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
    }
}
