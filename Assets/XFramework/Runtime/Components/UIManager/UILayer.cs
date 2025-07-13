using UnityEngine;
using UnityEngine.UI;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// UI 层
    /// </summary>
    public class UILayer
    {
        private readonly Canvas _canvas;
        private readonly UILayerType _layerType;

        public UILayerType LayerType => _layerType;
        public Canvas Canvas => _canvas;
        public Transform Transform => _canvas.transform;

        public UILayer(Transform uiRoot, UILayerType layerType, Camera uiCamera = null)
        {
            var layerObj = new GameObject(layerType.ToString());
            layerObj.transform.SetParent(uiRoot, false);
            _canvas = layerObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = uiCamera == null ? Camera.main : uiCamera;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = (int)layerType;
            _layerType = layerType;

            // 设置 Canvas 的其他必要组件
            layerObj.AddComponent<CanvasRenderer>();
            layerObj.AddComponent<GraphicRaycaster>();
            var canvasScaler = layerObj.AddComponent<CanvasScaler>();
            // TODO：后续增加系统配置类来设定
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
        }
    }
}
