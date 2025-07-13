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
        [Header("UI 根节点")]
        [SerializeField] private Transform _uiRoot;
        [Header("UI 摄像机，为空时默认使用主摄像机")]
        [SerializeField] private Camera _uiCamera;

        private readonly List<UILayer> _layers = new();

        internal override int Priority
        {
            get => XFrameworkConstant.ComponentPriority.UIManager;
        }

        internal override void Init()
        {
            base.Init();

            if (_uiRoot == null)
            {
                Log.Error("[XFramework] UIManager requires a UI Root transform to be set.");
                return;
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
        }
    }
}