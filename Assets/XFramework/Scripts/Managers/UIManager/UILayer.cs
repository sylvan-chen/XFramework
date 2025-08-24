using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        public enum StackSwitchType
        {
            None = 0,
            Hide = 1,
            Pause = 2,
            HideAndPause = 3,
        }

        private readonly int _id;
        private readonly string _name;
        private readonly StackSwitchType _switchType;
        private readonly Canvas _canvas;
        private readonly Stack<UIPanelBase> _panelStack = new();

        public int Id => _id;
        public string Name => _name;
        public StackSwitchType SwitchType => _switchType;
        public Canvas Canvas => _canvas;
        public Transform Transform => _canvas.transform;

        public UILayer(Transform uiRoot, Camera uiCamera, GameConfig.UiLayer config)
        {
            _id = config.Id;
            _name = config.Name;
            _switchType = (StackSwitchType)config.StackSwitchType;

            var layerObj = new GameObject(config.Name);
            layerObj.transform.SetParent(uiRoot, false);

            _canvas = layerObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = uiCamera;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = config.SortingOrder;

            // 设置 Canvas 的其他必要组件
            layerObj.AddComponent<CanvasRenderer>();
            layerObj.AddComponent<GraphicRaycaster>();
            var canvasScaler = layerObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasScaler.referenceResolution = Consts.SystemConsts.ScreenResolution;

            // 初始化栈
            _panelStack.Clear();
        }

        /// <summary>
        /// 将面板推入当前层的栈顶
        /// </summary>
        public void OpenPanel(UIPanelBase panel)
        {
            if (panel == null)
            {
                Log.Error($"[XFramework] [UILayer] Cannot push null panel to stack '{Name}'.");
                return;
            }

            if (_panelStack.Count > 0)
            {
                var topPanel = _panelStack.Peek();
                if (_switchType == StackSwitchType.Hide || _switchType == StackSwitchType.HideAndPause)
                {
                    topPanel.Hide();
                }
                if (_switchType == StackSwitchType.Pause || _switchType == StackSwitchType.HideAndPause)
                {
                    topPanel.Pause();
                }
            }

            panel.Show();
            _panelStack.Push(panel);
            panel.transform.SetParent(Transform, false);
        }

        /// <summary>
        /// 移除指定的面板
        /// </summary>
        public void ClosePanel(UIPanelBase panel)
        {
            if (panel == null)
            {
                Log.Error($"[XFramework] [UILayer] Cannot remove null panel from stack '{Name}'.");
                return;
            }

            if (_panelStack.Count == 0 || !_panelStack.Contains(panel))
            {
                return;
            }

            if (_panelStack.Peek() == panel) // 如果要移除的面板是栈顶面板，则恢复上一个面板
            {
                panel.Hide();
                _panelStack.Pop();
                if (_panelStack.Count > 0)
                {
                    var topPanel = _panelStack.Peek();
                    if (_switchType == StackSwitchType.Hide || _switchType == StackSwitchType.HideAndPause)
                    {
                        topPanel.Show();
                    }
                    if (_switchType == StackSwitchType.Pause || _switchType == StackSwitchType.HideAndPause)
                    {
                        topPanel.Resume();
                    }
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
