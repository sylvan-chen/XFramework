using System;
using UnityEngine;
using XFramework.Resource;

namespace XFramework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Resource Manager")]
    public sealed class ResourceManager : XFrameworkComponent
    {
        [SerializeField]
        private bool _enableEditorSimulate = true;

        [SerializeField]
        private ResourceMode _resourceMode;

        [SerializeField]
        private ReadWritePathType _readWritePathType;

        private IResourceHelper _resourceHelper;

        internal override int Priority
        {
            get => Global.PriorityValue.ResourceManager;
        }

        public string ReadOnlyPath
        {
            get;
            private set;
        }

        public string ReadWritePath
        {
            get;
            private set;
        }

        internal override void Init()
        {
            if (_enableEditorSimulate)
            {
                _resourceHelper = new EditorResourceHelper();
            }
            else
            {
                _resourceHelper = new DefaultResourceHelper();
            }

            ReadOnlyPath = Application.streamingAssetsPath;
            ReadWritePath = _readWritePathType switch
            {
                ReadWritePathType.TemporaryCache => Application.temporaryCachePath,
                ReadWritePathType.PersistentData => Application.persistentDataPath,
                _ => throw new NotSupportedException($"ReadWritePathType {_readWritePathType} not supported."),
            };

            if (!_enableEditorSimulate)
            {
                return;
            }
        }
    }
}