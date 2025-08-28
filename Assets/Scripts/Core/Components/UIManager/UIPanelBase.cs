using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// UI 界面基类
    /// </summary>
    public abstract class UIPanelBase : MonoBehaviour
    {
        private Table.UiPanelItem _tableItem;
        private bool _isInitialized;
        private bool _isVisible;
        private bool _isPaused;

        public Table.UiPanelItem Config => _tableItem;
        public bool IsInitialized => _isInitialized;
        public bool IsVisible => _isVisible;
        public bool IsPaused => _isPaused;

        public void Init(Table.UiPanelItem tableItem)
        {
            _tableItem = tableItem;
            SetVisibilityInternal(false); // 初始状态为隐藏
            _isPaused = false;

            OnInit();

            _isInitialized = true;
        }

        public void Clear()
        {
            OnClear();

            _tableItem = null;
            _isInitialized = false;
        }

        public void Show()
        {
            SetVisibilityInternal(true);
            OnShow();

            _isVisible = true;
        }

        public void Hide()
        {
            SetVisibilityInternal(false);
            OnHide();

            _isVisible = false;
        }

        public void Pause()
        {
            OnPause();

            _isPaused = true;
        }

        public void Resume()
        {
            OnResume();

            _isPaused = false;
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

        protected virtual void OnClear()
        {
        }

        private void SetVisibilityInternal(bool isVisible)
        {
            gameObject.SetActive(isVisible);

            _isVisible = isVisible;
        }
    }
}
