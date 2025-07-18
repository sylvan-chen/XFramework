using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using XFramework.Utils;

namespace XFramework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/UI Manager")]
    public sealed class UIManager : XFrameworkComponent
    {
        private Camera _uiCamera;
        private Transform _uiRoot;
        private readonly Dictionary<int, UILayer> _layers = new();
        private readonly Dictionary<int, UIPanelBase> _loadedPanels = new();
        private readonly Dictionary<int, UIPanelBase> _openedPanels = new();
        private readonly List<AssetHandler> _assetHandlers = new();

        internal override int Priority
        {
            get => Consts.XFrameworkConsts.ComponentPriority.UIManager;
        }

        internal override void Init()
        {
            base.Init();

            if (_uiRoot == null)
            {
                _uiRoot = new GameObject("[UIRoot]").transform;
                _uiRoot.SetParent(null);
                _uiRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                DontDestroyOnLoad(_uiRoot.gameObject);
                Log.Debug("[XFramework] UI root created at runtime.");
            }
            InitUICamera();
            InitUILayers();
        }

        private void InitUICamera()
        {
            // 创建专用的UI摄像机
            var cameraObj = new GameObject("[UICamera]");
            cameraObj.layer = LayerMask.NameToLayer("UI");
            cameraObj.transform.SetParent(_uiRoot);
            cameraObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            _uiCamera = cameraObj.AddComponent<Camera>();
            _uiCamera.clearFlags = CameraClearFlags.Depth;             // 使用深度清除
            _uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");  // 只渲染UI层
            _uiCamera.orthographic = true;                             // 使用正交投影
            _uiCamera.depth = 100;                                     // 确保在其他摄像机之上
            _uiCamera.useOcclusionCulling = false;                     // 不需要遮挡剔除，节约性能
        }

        public void InitUILayers()
        {
            var configTable = ConfigTableHelper.GetTable<UILayerConfigTable>();
            foreach (var config in configTable.Configs)
            {
                // 检查是否已经存在该层级
                if (_layers.ContainsKey(config.Id))
                {
                    Log.Warning($"[XFramework] UILayer '{config.Id} - {config.Name}' already exists, skipping initialization.");
                    continue;
                }

                // 创建新的 UILayer 对象
                var camera = _uiCamera != null ? _uiCamera : Camera.main;
                var uiLayer = new UILayer(_uiRoot, camera, config);
                _layers.Add(config.Id, uiLayer);
            }

            // 按照层级顺序排序节点
            var sortedLayers = _layers.Values.OrderBy(layer => layer.Canvas.sortingOrder).ToArray();
            for (int i = 0; i < sortedLayers.Length; i++)
            {
                sortedLayers[i].Transform.SetSiblingIndex(i);
            }

            // 设置所有层级的层级
            foreach (var layer in _layers.Values)
            {
                layer.Transform.gameObject.layer = LayerMask.NameToLayer("UI");
            }
        }

        public UILayer GetUILayer(int id)
        {
            if (_layers.TryGetValue(id, out var layer))
            {
                return layer;
            }
            Log.Error($"[XFramework] [UIManager] UILayer '{id}' not found.");
            return null;
        }

        public async UniTask<UIPanelBase> OpenPanelAsync(int id)
        {
            UILayer layer;
            // 检查缓存
            if (_openedPanels.TryGetValue(id, out var openedPanel))
            {
                return openedPanel;
            }
            else if (_loadedPanels.TryGetValue(id, out var loadedPanel))
            {
                layer = GetUILayer(loadedPanel.ParentLayerId);
                if (layer == null)
                {
                    Log.Error($"[XFramework] [UIManager] UILayer for panel '{id}' not found.");
                    return null;
                }
                layer.AddPanel(loadedPanel);
                _openedPanels[id] = loadedPanel;
                return loadedPanel;
            }

            var configTable = ConfigTableHelper.GetTable<UIPanelConfigTable>();
            var config = configTable.GetConfigById(id);

            // 加载新界面
            var assetHandler = await Global.AssetManager.LoadAssetAsync<GameObject>(config.Address);
            _assetHandlers.Add(assetHandler);
            var panelObj = await assetHandler.InstantiateAsync();
            if (!panelObj.TryGetComponent<UIPanelBase>(out var panel))
            {
                Log.Error($"[XFramework] [UIManager] UIPanelBase component not found in panel object for '{id}'.");
                Destroy(panelObj);
                return null;
            }
            panel.Init(config);
            layer = GetUILayer(config.ParentLayer);
            if (layer == null)
            {
                Log.Error($"[XFramework] [UIManager] UILayer for panel '{id}' not found.");
                Destroy(panelObj);
                return null;
            }
            layer.AddPanel(panel);
            // 缓存界面
            _loadedPanels[config.Id] = panel;
            _openedPanels[config.Id] = panel;
            Log.Debug($"[XFramework] [UIManager] Opened panel '{panel.Name}' ({panel.Id}).");
            return panel;
        }

        public void ClosePanel(int id)
        {
            if (_openedPanels.TryGetValue(id, out var openedPanel))
            {
                var layer = GetUILayer(openedPanel.ParentLayerId);
                layer?.RemovePanel(openedPanel);
                _openedPanels.Remove(id);
                _loadedPanels.Remove(id);
                Destroy(openedPanel.gameObject);
                Log.Debug($"[XFramework] [UIManager] Closed panel '{openedPanel.Name}' ({openedPanel.Id}).");
            }
            else
            {
                Log.Warning($"[XFramework] [UIManager] Attempted to close panel '{id}' that is not currently opened.");
            }
        }
    }
}