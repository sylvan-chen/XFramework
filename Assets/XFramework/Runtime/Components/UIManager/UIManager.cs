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
        private readonly List<UILayer> _layers = new();

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
            foreach (var layerType in Enum.GetValues(typeof(UILayerType)).Cast<UILayerType>())
            {
                // 检查是否已经存在该层级
                if (_layers.Any(layer => layer.LayerType == layerType))
                {
                    Log.Warning($"[XFramework] UILayer '{layerType}' already exists, skipping initialization.");
                    continue;
                }

                // 创建新的 UILayer 对象
                var uiLayer = new UILayer(_uiRoot, layerType, Camera.main);
                _layers.Add(uiLayer);
            }

            // 按照 UILayerType 的顺序排序
            _layers.Sort((a, b) => a.LayerType.CompareTo(b.LayerType));
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].Transform.SetSiblingIndex(i);
            }
        }
    }
}