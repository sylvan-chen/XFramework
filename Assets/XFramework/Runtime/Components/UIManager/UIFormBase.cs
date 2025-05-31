using System.Collections.Generic;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// UI 界面基类
    /// </summary>
    public abstract class UIFormBase : MonoBehaviour
    {
        private string _uiFormID;
        private string _uiFormAssetAddress;

        private bool _available = false;
        private bool _visiable = false;
        private int _originalLayer = 0;

        private bool _pauseOnCovered = true;
        private bool _hideOnCovered = false;

        private readonly List<Transform> _cachedTransforms = new();

        /// <summary>
        /// UI 界面的 ID
        /// </summary>
        public string UIFormID => _uiFormID;

        /// <summary>
        /// UI 界面的资源地址
        /// </summary>
        public string UIFormAssetAddress => _uiFormAssetAddress;

        /// <summary>
        /// UI 界面是否可用（可交互）
        /// </summary>
        public bool Available => _available;

        /// <summary>
        /// UI 界面是否可见
        /// </summary>
        public bool Visiable => _visiable;

        /// <summary>
        /// 初始化 UI 界面
        /// </summary>
        public void Init(string uiFormID, string uiFormAssetAddress, bool pauseOnCovered = true, bool hideOnCovered = false)
        {
            _uiFormID = uiFormID;
            _uiFormAssetAddress = uiFormAssetAddress;
            _pauseOnCovered = pauseOnCovered;
            _hideOnCovered = hideOnCovered;
            _originalLayer = gameObject.layer;

            OnInit();

            Global.UIManager.Register(this);
        }

        protected virtual void OnDestroy()
        {
            Global.UIManager.Unregister(this);
        }

        /// <summary>
        /// 打开 UI 界面
        /// </summary>
        public void Open()
        {
            _available = true;
            SetVisibilityInternal(true);

            OnOpen();
        }

        /// <summary>
        /// 关闭 UI 界面
        /// </summary>
        public void Close()
        {
            OnClose();

            // 递归设置界面和其子对象的层级，使其恢复原层级
            gameObject.GetComponentsInChildren<Transform>(true, _cachedTransforms);
            foreach (Transform trans in _cachedTransforms)
            {
                trans.gameObject.layer = _originalLayer;
            }
            _cachedTransforms.Clear();

            SetVisibilityInternal(false);
            _available = false;
        }

        /// <summary>
        /// 隐藏 UI 界面
        /// </summary>
        public void Hide()
        {
            OnHide();
            SetVisibilityInternal(false);
        }

        /// <summary>
        /// 显示 UI 界面
        /// </summary>
        public void Show()
        {
            SetVisibilityInternal(true);
            OnShow();
        }

        /// <summary>
        /// 暂停 UI 界面
        /// </summary>
        public void Pause()
        {
            OnPause();
            _available = false;
        }

        /// <summary>
        /// 恢复 UI 界面
        /// </summary>
        public void Resume()
        {
            _available = true;
            OnResume();
        }

        /// <summary>
        /// UI 界面初始化时
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// UI 界面打开时
        /// </summary>
        protected virtual void OnOpen()
        {
        }

        /// <summary>
        /// UI 界面关闭时
        /// </summary>
        protected virtual void OnClose()
        {
        }

        /// <summary>
        /// UI 界面显示时
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// UI 界面隐藏时
        /// </summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// UI 界面暂停时
        /// </summary>
        protected virtual void OnPause()
        {
        }

        /// <summary>
        /// UI 界面恢复时
        /// </summary>
        protected virtual void OnResume()
        {
        }

        private void SetVisibilityInternal(bool visiable)
        {
            if (!_available)
            {
                Log.Warning($"[XFramework] [UIFormBase] UIForm {UIFormID} is not available, can not set visibility.");
                return;
            }
            if (_visiable == visiable)
            {
                return;
            }
            _visiable = visiable;
            gameObject.SetActive(_visiable);
        }
    }
}