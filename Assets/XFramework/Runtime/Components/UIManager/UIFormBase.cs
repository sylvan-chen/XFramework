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
        private string _uiFormName;

        private bool _visiable = false;

        /// <summary>
        /// UI 界面名字，也是其资源名称和唯一标识符
        /// </summary>
        public string UIFormName => _uiFormName;

        /// <summary>
        /// UI 界面是否可见
        /// </summary>
        public bool Visiable => _visiable;

        /// <summary>
        /// 初始化 UI 界面
        /// </summary>
        public void Init(string uiFormName)
        {
            _uiFormName = uiFormName;
            OnInit();
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
        }

        /// <summary>
        /// 恢复 UI 界面
        /// </summary>
        public void Resume()
        {
            OnResume();
        }

        /// <summary>
        /// UI 界面初始化时
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// UI 界面销毁时
        /// </summary>
        protected virtual void OnDestroy()
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
            gameObject.SetActive(_visiable);
            _visiable = visiable;
        }
    }
}