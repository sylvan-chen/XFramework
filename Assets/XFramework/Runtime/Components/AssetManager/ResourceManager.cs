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
        private IResourceManagerKernel _kernel;

        internal override int Priority
        {
            get { return Global.PriorityValue.ResourceManager; }
        }

        internal override void Init()
        {
            base.Init();

            InitAsync().Forget();
        }

        public async UniTask InitAsync()
        {
            if (_isInit)
            {
                throw new InvalidOperationException("Init ResourceManager failed. It has already been initialized.");
            }
            switch (_resourceMode)
            {
                case ResourceMode.EditorSimulate:
#if UNITY_EDITOR
                    _kernel = new EditorSimulateKernel(EditorSimulateBuildPipeline.ScriptableBuildPipeline);
                    break;
#else
                    throw new InvalidOperationException("Init ResourceManager failed. ResourceMode EditorSimulate cannot be used in runtime.");
#endif
                case ResourceMode.Standalone:
                    _kernel = new StandaloneKernel();
                    break;
                case ResourceMode.Online:
                    _kernel = new OnlineKernel();
                    break;
                default:
                    throw new NotSupportedException($"ResourceMode {_resourceMode} is not supported.");
            }
            _isInit = true;

            await _kernel.InitAsync();
        }

    }
}