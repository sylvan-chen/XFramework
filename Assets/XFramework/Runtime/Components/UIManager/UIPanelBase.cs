using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum UIPanelType
    {
        /// <summary>
        /// 全屏界面 - 参与覆盖逻辑，会隐藏其他全屏界面
        /// </summary>
        FullScreen,

        /// <summary>
        /// 窗口界面 - 参与覆盖逻辑，会暂停但不隐藏其他普通界面
        /// </summary>
        Window,

        /// <summary>
        /// 弹窗界面 - 不参与覆盖逻辑，可以与其他界面共存
        /// </summary>
        Popup,

        /// <summary>
        /// 固定界面 - 常驻界面，不参与覆盖逻辑
        /// </summary>
        Fixed
    }

    /// <summary>
    /// UI 界面基类
    /// </summary>
    public abstract class UIPanelBase : MonoBehaviour
    {
        private UIPanelConfig _config;
        private bool _isVisible;
        private bool _isPaused;

        public UIPanelConfig Config => _config;
        public bool IsVisible => _isVisible;
        public bool IsPaused => _isPaused;

        public void Init(UIPanelConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (!string.IsNullOrEmpty(_config.Name))
            {
                gameObject.name = _config.Name;
            }
            _isVisible = true;
            _isPaused = false;

            OnInit();
        }

        public void Show()
        {
            _isVisible = true;
            SetVisibilityInternal(true);
            OnShow();
        }

        public void Hide()
        {
            _isVisible = false;
            SetVisibilityInternal(false);
            OnHide();
        }

        public void Pause()
        {
            _isPaused = true;
            OnPause();
        }

        public void Resume()
        {
            _isPaused = false;
            OnResume();
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnPause()
        {
        }

        protected virtual void OnResume()
        {
        }

        private void SetVisibilityInternal(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }
}
