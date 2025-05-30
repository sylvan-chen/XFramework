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
        private int _uiFormID;
        private string _uiFormAssetName;
        private int _uiFormOrder;
        private bool _isHideCoveredUIForm;

        private bool _isAvailable = false;
        private bool _isVisiable = false;
        private int _originalLayer = 0;

        private readonly List<Transform> _cachedTransforms = new();

        /// <summary>
        /// UI 界面的 ID
        /// </summary>
        public int UIFormID => _uiFormID;

        /// <summary>
        /// UI 界面的名称
        /// </summary>
        public string Name
        {
            get => gameObject.name;
            set => gameObject.name = value;
        }

        /// <summary>
        /// UI 界面的资源名称
        /// </summary>
        public string UIFormAssetName => _uiFormAssetName;

        /// <summary>
        /// UI 界面的顺序
        /// </summary>
        public int UIFormOrder => _uiFormOrder;

        /// <summary>
        /// 是否隐藏被该 UI 界面遮盖的其他界面
        /// </summary>
        public bool IsHideCoveredUIForm => _isHideCoveredUIForm;

        /// <summary>
        /// UI 界面是否可用（未打开的界面为不可用状态）
        /// </summary>
        public bool IsAvailable => _isAvailable;

        /// <summary>
        /// UI 界面是否可见
        /// </summary>
        public bool IsVisiable => _isVisiable;

        /// <summary>
        /// 初始化 UI 界面
        /// </summary>
        public void Init(int uiFormID, string uiFormAssetName, int uiFormOrder, bool isHideCoveredUIForm, object userData)
        {
            _uiFormID = uiFormID;
            _uiFormAssetName = uiFormAssetName;
            _uiFormOrder = uiFormOrder;
            _isHideCoveredUIForm = isHideCoveredUIForm;
            _originalLayer = gameObject.layer;

            OnInit(userData);

            Global.UIManager.Register(this);
        }

        protected virtual void OnDestroy()
        {
            Global.UIManager.Unregister(this);
        }

        /// <summary>
        /// 打开 UI 界面
        /// </summary>
        public void Open(object userData)
        {
            _isAvailable = true;
            Show();

            OnOpen(userData);
        }

        /// <summary>
        /// 关闭 UI 界面
        /// </summary>
        public void Close(object userData)
        {
            OnClose(userData);

            // 递归设置界面和其子对象的层级，使其恢复原层级
            gameObject.GetComponentsInChildren<Transform>(true, _cachedTransforms);
            foreach (Transform trans in _cachedTransforms)
            {
                trans.gameObject.layer = _originalLayer;
            }
            _cachedTransforms.Clear();

            Hide();
            _isAvailable = false;
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
        /// 隐藏 UI 界面
        /// </summary>
        public void Hide()
        {
            OnHide();
            SetVisibilityInternal(false);
        }

        /// <summary>
        /// UI 界面初始化时
        /// </summary>
        /// <param name="userData"></param>
        protected virtual void OnInit(object userData)
        {
        }

        /// <summary>
        /// UI 界面打开时
        /// </summary>
        protected virtual void OnOpen(object userData)
        {
        }

        /// <summary>
        /// UI 界面关闭时
        /// </summary>
        protected virtual void OnClose(object userData)
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

        private void SetVisibilityInternal(bool isVisiable)
        {
            if (!_isAvailable)
            {
                Log.Warning($"[XFramework] [UIFormBase] UIForm {Name} is not available, can not set visibility.");
                return;
            }
            if (_isVisiable == isVisiable)
            {
                return;
            }
            _isVisiable = isVisiable;
            gameObject.SetActive(_isVisiable);
        }
    }
}