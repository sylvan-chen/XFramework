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
        private readonly Dictionary<UILayerType, UILayer> _layers = new();
        private readonly Dictionary<string, UIPanelBase> _loadedPanels = new();
        private readonly Dictionary<string, UIPanelBase> _openedPanels = new();
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

            InitUILayers();
        }

        private void InitUILayers()
        {
            var layerTypes = Enum.GetValues(typeof(UILayerType)).Cast<UILayerType>().ToArray();
            foreach (var layerType in layerTypes)
            {
                // 检查是否已经存在该层级
                if (_layers.Values.Any(layer => layer.LayerType == layerType))
                {
                    Log.Warning($"[XFramework] UILayer '{layerType}' already exists, skipping initialization.");
                    continue;
                }

                // 创建新的 UILayer 对象
                var uiLayer = new UILayer(_uiRoot, layerType, Camera.main);
                _layers.Add(layerType, uiLayer);
            }

            // 按照 UILayerType 的顺序排序
            var sortedLayers = _layers.Values.OrderBy(layer => layer.LayerType).ToArray();
            for (int i = 0; i < sortedLayers.Length; i++)
            {
                sortedLayers[i].Transform.SetSiblingIndex(i);
            }
        }

        private UILayer GetUILayer(UILayerType layerType)
        {
            if (_layers.TryGetValue(layerType, out var layer))
            {
                return layer;
            }
            Log.Error($"[XFramework] UILayer '{layerType}' not found.");
            return null;
        }

        public async UniTask<T> OpenPanelAsync<T>(string panelAddress) where T : UIPanelBase
        {
            if (string.IsNullOrEmpty(panelAddress))
            {
                throw new ArgumentException("Panel address cannot be null or empty.", nameof(panelAddress));
            }
            // 检查缓存
            if (_openedPanels.ContainsKey(panelAddress))
            {
                return _openedPanels[panelAddress] as T;
            }
            // 加载新界面
            var assetHandler = await Global.AssetManager.LoadAssetAsync<T>(panelAddress);
            _assetHandlers.Add(assetHandler);
            var panelObj = await assetHandler.InstantiateAsync();
            if (panelObj.TryGetComponent<T>(out var panel))
            {
                var configTable = await ConfigHelper.LoadConfigAsync<UIPanelConfigTable>("uipanel");
                // 初始化
                panel.Init(configTable.GetConfigByAddress(panelAddress));
                // 添加到对应层级
                GetUILayer(panel.Config.LayerType).AddPanel(panel);

                _loadedPanels[panelAddress] = panel;
                _openedPanels[panelAddress] = panel;
                Log.Debug($"[XFramework] Opened panel '{panelAddress}' of type '{typeof(T).Name}'.");
            }
            return panel;
        }
    }
}