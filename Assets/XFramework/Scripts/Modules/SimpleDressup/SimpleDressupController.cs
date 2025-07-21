using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using XFramework.Utils;

namespace SimpleDressup
{
    /// <summary>
    /// 简单换装系统主控制器
    /// 负责协调整个换装流程：数据收集→图集生成→网格合并→结果应用
    /// </summary>
    public class SimpleDressupController : MonoBehaviour
    {
        [Header("基础配置")]
        [SerializeField] private int _atlasSize = 1024;
        [SerializeField] private bool _autoApplyOnStart = false;
        [SerializeField] private Transform _rootBone;

        [Header("目标对象")]
        [SerializeField] private SkinnedMeshRenderer _targetRenderer;
        [SerializeField] private Transform _dressupParent;

        // [Header("原始角色部件")]
        // [SerializeField] private SkinnedMeshRenderer[] _originalBodyParts; // 原始角色的多个部件

        // 核心组件
        private AtlasGenerator _atlasGenerator;
        private MeshCombiner _meshCombiner;

        // 运行时数据
        private List<DressupSlot> _currentSlots;
        private Dictionary<DressupSlotType, DressupSlot> _slotMap;
        private MeshCombiner.CombineResult _lastCombineResult;
        private AtlasGenerator.AtlasResult _lastAtlasResult;

        // 状态管理
        private bool _isInitialized = false;
        private bool _isDressing = false;

        public bool IsInitialized => _isInitialized;
        public bool IsDressing => _isDressing;
        public RenderTexture CurrentAtlas => _lastAtlasResult.AtlasTexture;
        public Mesh CurrentCombinedMesh => _lastCombineResult.CombinedMesh;

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

        /// <summary>
        /// 初始化核心组件
        /// </summary>
        private void Init()
        {

            _atlasGenerator = new AtlasGenerator(_atlasSize);
            _meshCombiner = new MeshCombiner();

            _currentSlots = new List<DressupSlot>();
            _slotMap = new Dictionary<DressupSlotType, DressupSlot>();

            _isInitialized = true;

            Log.Debug("[SimpleDressupController] Initialization complete.");
        }

        /// <summary>
        /// 设置换装部件
        /// </summary>
        public void SetDressupSlot(DressupSlot slot)
        {
            if (slot == null || !_isInitialized)
            {
                Log.Warning("[SimpleDressupController] Invalid dressup slot or controller not initialized.");
                return;
            }

            // 移除同类型的旧部件
            RemoveDressupSlot(slot.SlotType);

            // 添加新部件
            _currentSlots.Add(slot);
            _slotMap[slot.SlotType] = slot;

            Log.Debug($"[SimpleDressupController] Set dressup slot - {slot.SlotType}.");
        }


        /// <summary>
        /// 从 GameObject 创建并设置换装部件
        /// </summary>
        public void SetDressupSlotFromGameObject(GameObject dressupObject, DressupSlotType slotType)
        {
            if (dressupObject == null)
            {
                Log.Warning("[SimpleDressupController] dressupObject is null.");
                return;
            }

            var slot = ScriptableObject.CreateInstance<DressupSlot>();
            slot.SlotType = slotType;
            slot.InitializeFromGameObject(dressupObject);
            SetDressupSlot(slot);
        }

        /// <summary>
        /// 移除指定类型的换装部件
        /// </summary>
        public void RemoveDressupSlot(DressupSlotType slotType)
        {
            if (!_isInitialized) return;

            var slot = _slotMap.GetValueOrDefault(slotType);
            if (slot != null)
            {
                _currentSlots.Remove(slot);
                _slotMap.Remove(slotType);

                Log.Debug($"[SimpleDressupController] Remove dressup slot - {slotType}.");
            }
        }

        /// <summary>
        /// 获取指定类型的换装部件
        /// </summary>
        public DressupSlot GetDressupSlot(DressupSlotType slotType)
        {
            return _slotMap.GetValueOrDefault(slotType);
        }

        /// <summary>
        /// 批量设置换装部件
        /// </summary>
        public void SetDressupSlots(IEnumerable<DressupSlot> slots)
        {
            if (slots == null) return;

            foreach (var slot in slots)
            {
                SetDressupSlot(slot);
            }
        }

        /// <summary>
        /// 清空所有换装部件
        /// </summary>
        public void ClearAllSlots()
        {
            _currentSlots.Clear();
            _slotMap.Clear();

            Log.Debug("[SimpleDressupController] Clear all dressup slots.");
        }

        /// <summary>
        /// 应用当前的换装配置（异步）
        /// </summary>
        public async UniTask<bool> ApplyCurrentDressupAsync()
        {
            if (!_isInitialized)
            {
                Log.Error("[SimpleDressupController] Controller not initialized.");
                return false;
            }

            if (_isDressing)
            {
                Log.Warning("[SimpleDressupController] Dressing in progress, ignoring duplicate call.");
                return false;
            }

            if (_currentSlots.Count == 0)
            {
                Log.Warning("[SimpleDressupController] No dressup slots to apply.");
                return false;
            }

            _isDressing = true;

            try
            {
                Log.Debug($"[SimpleDressupController] Start dressing process - {_currentSlots.Count} slots.");

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
                _isDressing = false;
            }
        }

        /// <summary>
        /// 生成纹理图集
        /// </summary>
        private async UniTask<bool> GenerateTextureAtlasAsync()
        {
            // 收集所有纹理片段
            var allFragments = new List<TextureFragment>();

            foreach (var slot in _currentSlots)
            {
                if (slot.TextureFragments != null)
                {
                    allFragments.AddRange(slot.TextureFragments);
                }
            }

            if (allFragments.Count == 0)
            {
                Log.Warning("[SimpleDressupController] No texture fragments found.");
                return false;
            }

            // 在下一帧生成图集以避免阻塞主线程
            await UniTask.NextFrame();

            _lastAtlasResult = _atlasGenerator.GenerateAtlas(allFragments);

            if (!_lastAtlasResult.Success)
            {
                Log.Error("[SimpleDressupController] Failed to generate texture atlas.");
                return false;
            }

            // 更新换装部件的UV映射
            UpdateSlotUVMappings();

            Log.Debug($"[SimpleDressupController] Atlas generated successfully - Size: {_atlasSize}x{_atlasSize}");

            return true;
        }

        /// <summary>
        /// 更新换装部件的UV映射
        /// </summary>
        private void UpdateSlotUVMappings()
        {
            foreach (var slot in _currentSlots)
            {
                if (slot.TextureFragments == null) continue;

                foreach (var fragment in slot.TextureFragments)
                {
                    if (_lastAtlasResult.FragmentUVs.TryGetValue(fragment, out var atlasUV))
                    {
                        fragment.SetAtlasRegion(atlasUV);
                    }
                }
            }
        }

        /// <summary>
        /// 合并网格（支持原始多部件角色）
        /// </summary>
        private async UniTask<bool> CombineMeshesAsync()
        {
            // 准备合并实例
            var combineInstances = new List<MeshCombiner.CombineInstance>();

            // // 1. 首先添加原始角色部件
            // if (_originalBodyParts != null)
            // {
            //     foreach (var bodyPart in _originalBodyParts)
            //     {
            //         if (bodyPart != null && bodyPart.sharedMesh != null)
            //         {
            //             // 从原始角色部件创建 DressupMesh
            //             var bodyMesh = ScriptableObject.CreateInstance<DressupMesh>();
            //             bodyMesh.ExtractFromRenderer(bodyPart);

            //             // 使用原始材质（不使用图集，保持原样）
            //             var originalMaterials = bodyPart.sharedMaterials;

            //             var instance = new MeshCombiner.CombineInstance(bodyMesh, originalMaterials);
            //             ConfigureSubmeshMapping(instance, null); // 传 null 是因为这不是装备槽位

            //             combineInstances.Add(instance);

            //             Log.Debug($"[SimpleDressupController] Added original body part: {bodyPart.name}");
            //         }
            //     }
            // }

            // 2. 然后添加装备部件
            foreach (var slot in _currentSlots)
            {
                if (slot.Mesh == null || !slot.Mesh.IsValid()) continue;

                // 创建使用图集的材质
                var atlasMaterials = CreateAtlasMaterials(slot);

                var instance = new MeshCombiner.CombineInstance(slot.Mesh, atlasMaterials);

                // 配置子网格映射
                ConfigureSubmeshMapping(instance, slot);

                combineInstances.Add(instance);
            }

            if (combineInstances.Count == 0)
            {
                Log.Warning("[SimpleDressupController] No valid meshes to combine.");
                return false;
            }

            // 在下一帧合并网格
            await UniTask.NextFrame();

            _lastCombineResult = _meshCombiner.CombineMeshes(combineInstances, _rootBone);

            if (!_lastCombineResult.Success)
            {
                Log.Error("[SimpleDressupController] Failed to combine meshes.");
                return false;
            }

            Log.Debug($"[SimpleDressupController] Meshes combined successfully - {combineInstances.Count} instances.");
            // Log.Debug($"[SimpleDressupController] Meshes combined successfully - {combineInstances.Count} instances (including {_originalBodyParts?.Length ?? 0} original body parts).");

            return true;
        }

        /// <summary>
        /// 为换装部件创建图集材质
        /// </summary>
        private Material[] CreateAtlasMaterials(DressupSlot slot)
        {
            if (slot.Materials == null || slot.Materials.Length == 0)
                return new Material[0];

            var atlasMaterials = new Material[slot.Materials.Length];

            for (int i = 0; i < slot.Materials.Length; i++)
            {
                var dressupMat = slot.Materials[i];
                if (dressupMat?.SourceMaterial != null)
                {
                    // 创建使用图集纹理的新材质
                    var atlasMat = new Material(dressupMat.SourceMaterial);
                    atlasMat.name = $"{dressupMat.SourceMaterial.name}_Atlas";
                    atlasMat.mainTexture = _lastAtlasResult.AtlasTexture;

                    atlasMaterials[i] = atlasMat;
                }
            }

            return atlasMaterials;
        }

        /// <summary>
        /// 配置子网格映射
        /// </summary>
        private void ConfigureSubmeshMapping(MeshCombiner.CombineInstance instance, DressupSlot slot = null)
        {
            // 简单策略：所有槽位一律按材质顺序映射到子网格
            for (int i = 0; i < instance.TargetSubmeshIndices.Length; i++)
            {
                instance.TargetSubmeshIndices[i] = i;
            }
        }

        /// <summary>
        /// 自动检测并收集原始角色部件
        /// </summary>
        // public void AutoDetectOriginalBodyParts()
        // {
        //     if (_dressupParent == null) return;

        //     var allRenderers = _dressupParent.GetComponentsInChildren<SkinnedMeshRenderer>();
        //     var bodyParts = new List<SkinnedMeshRenderer>();

        //     foreach (var renderer in allRenderers)
        //     {
        //         // 跳过目标渲染器
        //         if (renderer == _targetRenderer) continue;

        //         // 通过名称判断是否为原始身体部件
        //         string lowerName = renderer.name.ToLower();
        //         if (lowerName.Contains("body") || lowerName.Contains("head") ||
        //             lowerName.Contains("torso") || lowerName.Contains("limb") ||
        //             lowerName.Contains("原始") || lowerName.Contains("base"))
        //         {
        //             bodyParts.Add(renderer);
        //             Log.Debug($"[SimpleDressupController] Detected original body part: {renderer.name}");
        //         }
        //     }

        //     _originalBodyParts = bodyParts.ToArray();
        //     Log.Debug($"[SimpleDressupController] Auto-detected {_originalBodyParts.Length} original body parts.");
        // }

        /// <summary>
        /// 应用到目标渲染器
        /// </summary>
        private bool ApplyToTargetRenderer()
        {
            if (_targetRenderer == null)
            {
                Log.Error("[SimpleDressupController] Target renderer is null.");
                return false;
            }

            if (!_lastCombineResult.Success || _lastCombineResult.CombinedMesh == null)
            {
                Log.Error("[SimpleDressupController] No valid combine result.");
                return false;
            }

            try
            {
                // 应用合并的网格
                _targetRenderer.sharedMesh = _lastCombineResult.CombinedMesh;

                // 应用材质
                if (_lastCombineResult.CombinedMaterials != null)
                {
                    _targetRenderer.sharedMaterials = _lastCombineResult.CombinedMaterials;
                }

                // 应用骨骼
                if (_lastCombineResult.Bones != null && _lastCombineResult.Bones.Length > 0)
                {
                    _targetRenderer.bones = _lastCombineResult.Bones;
                }

                // 设置根骨骼
                if (_lastCombineResult.RootBone != null)
                {
                    _targetRenderer.rootBone = _lastCombineResult.RootBone;
                }

                Log.Debug("[SimpleDressupController] Successfully applied to target renderer.");

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[SimpleDressupController] Failed to apply to target renderer - {e.Message}.");
                return false;
            }
        }

        /// <summary>
        /// 从子对象自动收集换装部件
        /// </summary>
        public void CollectDressupSlotsFromChildren()
        {
            if (_dressupParent == null)
            {
                Log.Warning("[SimpleDressupController] DressupParent is not set.");
                return;
            }

            ClearAllSlots();

            var renderers = _dressupParent.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var renderer in renderers)
            {
                if (renderer.gameObject == _targetRenderer?.gameObject) continue;

                // 尝试从名称推断槽位类型
                var slotType = InferSlotTypeFromName(renderer.name);
                SetDressupSlotFromGameObject(renderer.gameObject, slotType);
            }

            Log.Debug($"[SimpleDressupController] Collected {_currentSlots.Count} dress-up components from children.");
        }

        /// <summary>
        /// 从名称推断槽位类型
        /// </summary>
        private DressupSlotType InferSlotTypeFromName(string name)
        {
            var lowerName = name.ToLower();

            if (lowerName.Contains("hair")) return DressupSlotType.Hair;
            if (lowerName.Contains("face")) return DressupSlotType.Face;
            if (lowerName.Contains("top") || lowerName.Contains("shirt")) return DressupSlotType.Top;
            if (lowerName.Contains("bottom") || lowerName.Contains("pants")) return DressupSlotType.Bottom;
            if (lowerName.Contains("shoe")) return DressupSlotType.Shoes;
            if (lowerName.Contains("glove")) return DressupSlotType.Gloves;
            if (lowerName.Contains("hat")) return DressupSlotType.Hat;

            return DressupSlotType.None; // 默认类型
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            _atlasGenerator?.Cleanup();

            // 清理临时创建的材质
            if (_lastCombineResult.CombinedMaterials != null)
            {
                foreach (var mat in _lastCombineResult.CombinedMaterials)
                {
                    if (mat != null)
                        DestroyImmediate(mat);
                }
            }
        }

        /// <summary>
        /// 编辑器调试方法
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnValidate()
        {
            _atlasSize = Mathf.Clamp(_atlasSize, 256, 4096);
            _atlasSize = Mathf.NextPowerOfTwo(_atlasSize);
        }

#if UNITY_EDITOR
        [Header("编辑器调试")]
        [SerializeField] private bool _debugShowAtlas = false;

        private void OnGUI()
        {
            if (!_debugShowAtlas || _lastAtlasResult.AtlasTexture == null) return;

            var rect = new Rect(10, 10, 256, 256);
            GUI.DrawTexture(rect, _lastAtlasResult.AtlasTexture);
            GUI.Label(new Rect(10, 270, 200, 20), $"Atlas: {_atlasSize}x{_atlasSize}");
        }
#endif
    }
}
