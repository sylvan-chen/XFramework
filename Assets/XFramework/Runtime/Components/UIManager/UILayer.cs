using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// UI 层
    /// </summary>
    /// <remarks>
    /// 每个层维护自己的栈，栈顶的界面是当前层显示的界面，每层只能有一个显示界面。
    /// </remarks>
    public class UILayer
    {
        private readonly Canvas _canvas;
        private readonly UILayerType _layerType;
        private readonly Stack<UIPanelBase> _panelStack = new();

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

            // 初始化栈
            _panelStack.Clear();
        }

        /// <summary>
        /// 将面板推入当前层的栈顶
        /// </summary>
        public void AddPanel(UIPanelBase panel)
        {
            if (panel == null)
            {
                Log.Error($"[XFramework] [UILayer] Cannot push null panel to stack '{LayerType}'.");
                return;
            }

            // 暂停并隐藏当前栈顶面板
            if (_panelStack.Count > 0)
            {
                var topPanel = _panelStack.Peek();
                topPanel.Pause();
                topPanel.Hide();
            }

            _panelStack.Push(panel);
            panel.transform.SetParent(Transform, false);
        }

        /// <summary>
        /// 移除指定的面板
        /// </summary>
        public void RemovePanel(UIPanelBase panel)
        {
            if (panel == null)
            {
                Log.Error($"[XFramework] [UILayer] Cannot remove null panel from stack '{LayerType}'.");
                return;
            }

            if (_panelStack.Count == 0 || !_panelStack.Contains(panel))
            {
                return;
            }

            if (_panelStack.Peek() == panel) // 如果要移除的面板是栈顶面板，则隐藏并恢复上一个面板
            {
                panel.Hide();
                _panelStack.Pop();
                if (_panelStack.Count > 0)
                {
                    var topPanel = _panelStack.Peek();
                    topPanel.Resume();
                    topPanel.Show();
                }
            }
            else // 如果要移除的面板不是栈顶面板，则直接从栈中移除
            {
                var tempStack = new Stack<UIPanelBase>();
                while (_panelStack.Count > 0)
                {
                    var currentPanel = _panelStack.Pop();
                    if (currentPanel != panel)
                    {
                        tempStack.Push(currentPanel);
                    }
                }

                // 恢复剩余的面板
                while (tempStack.Count > 0)
                {
                    var remainingPanel = tempStack.Pop();
                    _panelStack.Push(remainingPanel);
                }
            }
        }
    }
}
