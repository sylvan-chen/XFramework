using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using XFramework.Utils;
using UnityEngine.UI;

namespace XFramework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/UI Manager")]
    public sealed class UIManager : XFrameworkComponent
    {
        [Header("UI 摄像机，为空时默认使用主摄像机")]
        [SerializeField] private Camera _uiCamera;

        private Transform _uiRoot;
        private readonly Dictionary<int, UILayer> _layers = new();
        private readonly Dictionary<int, UIPanelBase> _loadedPanels = new();
        private readonly Dictionary<int, UIPanelBase> _openedPanels = new();
        private readonly List<AssetHandler> _assetHandlers = new();

        private UILayerConfigTable _layerConfigTable;

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

            InitUILayersAsync().Forget();
        }

        private async UniTask InitUILayersAsync()
        {
            _layerConfigTable = await ConfigHelper.LoadConfigAsync<UILayerConfigTable>("uilayer");
            foreach (var config in _layerConfigTable.Configs)
            {
                // 检查是否已经存在该层级
                if (_layers.ContainsKey(config.Id))
                {
                    Log.Warning($"[XFramework] UILayer '{config.Id} - {config.Name}' already exists, skipping initialization.");
                    continue;
                }

                // 创建新的 UILayer 对象
                var uiLayer = new UILayer(_uiRoot, _uiCamera != null ? _uiCamera : Camera.main, config);
                _layers.Add(config.Id, uiLayer);
            }

            // 按照 UILayerType 的顺序排序
            var sortedLayers = _layers.Values.OrderBy(layer => layer.Canvas.sortingOrder).ToArray();
            for (int i = 0; i < sortedLayers.Length; i++)
            {
                sortedLayers[i].Transform.SetSiblingIndex(i);
            }
        }

        private UILayer GetUILayer(int Id)
        {
            if (_layers.TryGetValue(Id, out var layer))
            {
                return layer;
            }
            Log.Error($"[XFramework] UILayer '{Id}' not found.");
            return null;
        }

        public async UniTask<T> OpenPanelAsync<T>(string panelAddress) where T : UIPanelBase
        {
            var configTable = await ConfigHelper.LoadConfigAsync<UIPanelConfigTable>("uipanel");
            var config = configTable.GetConfigByAddress(panelAddress);
            // 检查缓存
            if (_openedPanels.TryGetValue(config.Id, out var openedPanel))
            {
                return openedPanel as T;
            }
            else if (_loadedPanels.TryGetValue(config.Id, out var loadedPanel))
            {
                if (_layers.TryGetValue(loadedPanel.Config.ParentLayer, out var layer))
                {
                    layer.AddPanel(loadedPanel);
                    _openedPanels[config.Id] = loadedPanel;
                }
                return loadedPanel as T;
            }
            // 加载新界面
            var assetHandler = await Global.AssetManager.LoadAssetAsync<T>(panelAddress);
            _assetHandlers.Add(assetHandler);
            var panelObj = await assetHandler.InstantiateAsync();
            if (panelObj.TryGetComponent<T>(out var panel))
            {
                panel.Init(configTable.GetConfigByAddress(panelAddress));
                if (_layers.TryGetValue(panel.Config.ParentLayer, out var layer))
                {
                    layer.AddPanel(panel);
                }
                // 缓存界面
                _loadedPanels[config.Id] = panel;
                _openedPanels[config.Id] = panel;
                Log.Debug($"[XFramework] Opened panel '{panelAddress}' of type '{typeof(T).Name}'.");
            }
            return panel;
        }
    }
}