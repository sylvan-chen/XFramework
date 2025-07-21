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
        private Transform _closedPanelRoot;
        private readonly Dictionary<int, UILayer> _layers = new();
        private readonly Dictionary<int, UIPanelBase> _loadedPanels = new();
        private readonly Dictionary<int, UIPanelBase> _openedPanels = new();
        private readonly List<AssetHandler> _assetHandlers = new();

        internal override int Priority => Consts.XFrameworkConsts.ComponentPriority.UIManager;

        internal override void Init()
        {
            base.Init();

            if (_uiRoot == null)
            {
                _uiRoot = new GameObject("[UIRoot]").transform;
                _uiRoot.SetParent(null);
                _uiRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                Object.DontDestroyOnLoad(_uiRoot.gameObject);
                _closedPanelRoot = new GameObject("[ClosedPanels]").transform;
                _closedPanelRoot.SetParent(_uiRoot, false);
            }
            CreateUICamera();
            CreateUILayers();
        }

        internal override void Shutdown()
        {
            base.Shutdown();

            foreach (var handler in _assetHandlers)
            {
                handler.Release();
            }
            _assetHandlers.Clear();
            _loadedPanels.Clear();
            _openedPanels.Clear();
            _layers.Clear();

            if (_uiRoot != null)
            {
                Object.Destroy(_uiRoot.gameObject);
                _uiRoot = null;
                Log.Debug("[XFramework] UI root destroyed.");
            }
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            base.Update(deltaTime, unscaledDeltaTime);
        }

        private void CreateUICamera()
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

        private void CreateUILayers()
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

        public async UniTask<UIPanelBase> LoadPanelAsync(int id)
        {
            // 检查缓存
            if (_loadedPanels.TryGetValue(id, out var loadedPanel))
            {
                return loadedPanel;
            }

            var configTable = ConfigTableHelper.GetTable<UIPanelConfigTable>();
            var config = configTable.GetConfigById(id);
            var assetHandler = await Global.AssetManager.LoadAssetAsync<GameObject>(config.Address);
            _assetHandlers.Add(assetHandler);
            var panelObj = await assetHandler.InstantiateAsync();
            if (!panelObj.TryGetComponent<UIPanelBase>(out var panel))
            {
                Log.Error($"[XFramework] [UIManager] UIPanelBase component not found in panel object for '{id}'.");
                Object.Destroy(panelObj);
                return null;
            }
            panel.Init(config);
            panel.transform.SetParent(_closedPanelRoot, false);
            _loadedPanels[config.Id] = panel;
            return panel;
        }

        public UIPanelBase OpenPanel(int id)
        {
            if (_openedPanels.TryGetValue(id, out var openedPanel))        // 已经打开
            {
                return openedPanel;
            }
            else if (_loadedPanels.TryGetValue(id, out var loadedPanel))   // 已经加载但未打开
            {
                var layer = GetUILayer(loadedPanel.Config.ParentLayer);
                if (layer == null)
                {
                    Log.Error($"[XFramework] [UIManager] UILayer({loadedPanel.Config.ParentLayer}) for panel({id}) not found.");
                    return null;
                }
                layer.OpenPanel(loadedPanel);
                _openedPanels[id] = loadedPanel;
                return loadedPanel;
            }
            else
            {
                Log.Error($"[XFramework] [UIManager] Panel({id}) is unloaded.");
                return null;
            }
        }

        public void ClosePanel(int id)
        {
            if (_openedPanels.TryGetValue(id, out var openedPanel))
            {
                var layer = GetUILayer(openedPanel.Config.ParentLayer);
                layer?.ClosePanel(openedPanel);
                _openedPanels.Remove(id);
                openedPanel.transform.SetParent(_closedPanelRoot, false);
            }
        }

        public void ClosePanel(UIPanelBase panel)
        {
            ClosePanel(panel.Config.Id);
        }

        public void UnloadPanel(int id)
        {
            if (_loadedPanels.TryGetValue(id, out var loadedPanel))
            {
                ClosePanel(id);
                _loadedPanels.Remove(id);
                loadedPanel.Clear();
                Object.Destroy(loadedPanel.gameObject);
            }
        }

        public void UnloadPanel(UIPanelBase panel)
        {
            UnloadPanel(panel.Config.Id);
        }
    }
}