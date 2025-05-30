using System.Collections.Generic;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// UI 管理器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/UI Manager")]
    public sealed class UIManager : XFrameworkComponent
    {
        private Dictionary<int, UIFormBase> _uiFormDict = new(); // 全部 UIForm 的缓存
        private List<UIFormBase> _availableUIForms = new(); // 打开的 UIForm 列表
        private Transform _uiRoot; // UI 根节点

        internal override int Priority
        {
            get => Global.PriorityValue.UIManager;
        }

        internal override void Clear()
        {
            base.Clear();
        }

        /// <summary>
        /// 注册 UIForm 到管理器
        /// </summary>
        /// <param name="uiForm"></param>
        public void Register(UIFormBase uiForm)
        {
            if (!_uiFormDict.ContainsKey(uiForm.UIFormID))
            {
                _uiFormDict.Add(uiForm.UIFormID, uiForm);
            }
            else
            {
                _uiFormDict[uiForm.UIFormID] = uiForm;
            }
        }

        /// <summary>
        /// 从管理器注销 UIForm
        /// </summary>
        public void Unregister(UIFormBase uiForm)
        {
            if (_uiFormDict.ContainsKey(uiForm.UIFormID))
            {
                _uiFormDict.Remove(uiForm.UIFormID);
            }
        }

        /// <summary>
        /// 打开 UIForm
        /// </summary>
        /// <param name="formID">要打开的 UIForm 的 ID</param>
        public void Open(int formID, object userData = null)
        {
            if (!_uiFormDict.ContainsKey(formID))
            {
                // Global.AssetManager.LoadAssetAsync <
            }

            UIFormBase uiForm = _uiFormDict[formID];
            uiForm.Open(userData);
            _availableUIForms.Add(uiForm);
        }

        /// <summary>
        /// 关闭 UIForm
        /// </summary>
        /// <param name="formID">要关闭的 UIForm 的 ID</param>
        public void Close(int formID, object userData = null)
        {
            if (!_uiFormDict.ContainsKey(formID))
            {
                Log.Error($"[XFramework] [UIManager] Close UIForm failed. Missing UIForm with ID {formID}.");
                return;
            }

            UIFormBase uiForm = _uiFormDict[formID];
            if (_availableUIForms.Contains(uiForm))
            {
                _availableUIForms.Remove(uiForm);
            }
            uiForm.Close(userData);
        }

    }
}
