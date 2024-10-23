using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XFramework.Resource
{
    public sealed partial class ResourceManager : XFrameworkComponent
    {
        [SerializeField]
        private ResourceMode _resourceMode;

        private bool _isInit = false;
        private IResourceModeKernel _resourceHelper;

        internal override int Priority
        {
            get { return Global.PriorityValue.ResourceManager; }
        }



        public async UniTask InitAsync()
        {
            if (_isInit)
            {
                throw new InvalidOperationException("Init ResourceManager failed. It has already been initialized.");
            }
#if !UNITY_EDITOR
            if (_resourceMode == ResourceMode.EditorSimulate)
            {
                throw new InvalidOperationException("Init ResourceManager failed. ResourceMode EditorSimulate cannot be used in runtime.");
            }
#endif
            switch (_resourceMode)
            {
                case ResourceMode.EditorSimulate:
                    break;
                case ResourceMode.Standalone:
                    break;
                case ResourceMode.Online:
                    break;
                default:
                    throw new NotSupportedException($"ResourceMode {_resourceMode} is not supported.");
            }
            _isInit = true;
        }
    }
}