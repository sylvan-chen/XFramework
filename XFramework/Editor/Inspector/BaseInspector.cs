using UnityEditor;
using UnityEngine.UIElements;

namespace XFramework.Editor
{
    public abstract class BaseInspector : UnityEditor.Editor
    {
        private bool _isCompileStart = false;

        public override void OnInspectorGUI()
        {
            if (!_isCompileStart && EditorApplication.isCompiling)
            {
                _isCompileStart = true;
                OnCompileStart();
            }
            else if (_isCompileStart && !EditorApplication.isCompiling)
            {
                _isCompileStart = false;
                OnCompileFinish();
            }
        }

        /// <summary>
        /// 编译开始事件
        /// </summary>
        protected virtual void OnCompileStart()
        {
        }

        /// <summary>
        /// 编译结束事件
        /// </summary>
        protected virtual void OnCompileFinish()
        {
        }
    }
}