using System;
using UnityEngine;

namespace XFramework
{
    /// <summary>
    /// UI 界面基类
    /// </summary>
    public abstract class UIPanelBase : MonoBehaviour
    {
        private int _id;
        private string _name;
        private string _address;
        private int _parentLayerId;
        private bool _isVisible;
        private bool _isPaused;

        public int Id => _id;
        public string Name => _name;
        public string Address => _address;
        public int ParentLayerId => _parentLayerId;
        public bool IsVisible => _isVisible;
        public bool IsPaused => _isPaused;

        public void Init(UIPanelConfig config)
        {
            gameObject.name = config.Name;
            _id = config.Id;
            _name = config.Name;
            _address = config.Address;
            _parentLayerId = config.ParentLayer;

            gameObject.SetActive(false); // 初始状态为隐藏
            _isVisible = false;
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
