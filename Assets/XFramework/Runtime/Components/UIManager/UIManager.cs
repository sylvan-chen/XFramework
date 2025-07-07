using System;
using System.Collections.Generic;
using UnityEngine;
using XFramework.Utils;
using YooAsset;

namespace XFramework
{
    /// <summary>
    /// UI 管理器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/UI Manager")]
    public sealed class UIManager : XFrameworkComponent
    {
        private Stack<UIFormBase> _openedUIForms = new(); // 打开的 UIForm 栈
        private Dictionary<string, AssetHandle> _handleDict = new(); // 资源句柄字典

        internal override int Priority
        {
            get => XFrameworkConstant.ComponentPriority.UIManager;
        }

        internal override void Clear()
        {
            base.Clear();

            CloseAll();
            _openedUIForms.Clear();
            _handleDict.Clear();
        }

        /// <summary>
        /// 打开 UIForm
        /// </summary>
        public void Open(string uiFormName)
        {
            UIFormBase topUIForm = _openedUIForms.Peek();
            if (topUIForm != null && topUIForm.UIFormName == uiFormName)
            {
                return; // 防止重复打开
            }

            Global.AssetManager.LoadAssetAsync<GameObject>(uiFormName, (handle) =>
            {
                _handleDict.Add(uiFormName, handle);
                GameObject go = handle.InstantiateSync();
                if (!go.TryGetComponent(out UIFormBase uiForm))
                {
                    throw new ArgumentException($"Open UIForm failed. {uiFormName} missing UIFormBase component.");
                }
                if (topUIForm != null)
                {
                    topUIForm.Pause();
                }
                _openedUIForms.Push(uiForm);
                uiForm.Init(uiFormName);
            });
        }

        /// <summary>
        /// 关闭 UIForm
        /// </summary>
        /// <param name="uiFormName">要关闭的 UIForm 的 ID</param>
        public void Close(string uiFormName)
        {
            if (!_handleDict.ContainsKey(uiFormName))
            {
                Log.Error($"Close UIForm failed. {uiFormName} not found.");
                return;
            }
            if (_openedUIForms.Peek().UIFormName != uiFormName)
            {
                Log.Error($"Close UIForm {uiFormName} failed. You can only close the top UIForm.");
                return;
            }

            if (_handleDict.TryGetValue(uiFormName, out AssetHandle handle))
            {
                Destroy(_openedUIForms.Pop().gameObject);
                UIFormBase topUIForm = _openedUIForms.Peek();
                if (topUIForm != null)
                {
                    topUIForm.Resume();
                }
                handle.Release();
                _handleDict.Remove(uiFormName);
            }
        }

        public void CloseAll()
        {
            while (_openedUIForms.Count > 0)
            {
                UIFormBase uiForm = _openedUIForms.Pop();
                if (_handleDict.TryGetValue(uiForm.UIFormName, out AssetHandle handle))
                {
                    Destroy(uiForm.gameObject);
                    handle.Release();
                    _handleDict.Remove(uiForm.UIFormName);
                }

            }
        }
    }
}
