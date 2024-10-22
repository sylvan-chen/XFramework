using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace XFramework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Resource Manager")]
    public sealed partial class ResourceManager : XFrameworkComponent
    {
        public const string RemoteManifestFileName = "ResourceManifest.dat";
        public const string LocalManifestFileName = "ResourceManifest_Local.json";

        [SerializeField]
        private bool _enableEditorSimulate = true;

        [SerializeField]
        private ResourceMode _resourceMode;

        [SerializeField]
        private ReadWritePathType _readWritePathType;

        internal override int Priority
        {
            get => Global.PriorityValue.ResourceManager;
        }

        /// <summary>
        /// 只读路径
        /// </summary>
        public string ReadOnlyPath
        {
            get;
            private set;
        }

        /// <summary>
        /// 读写路径
        /// </summary>
        public string ReadWritePath
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前变体
        /// </summary>
        public string CurrentVariant
        {
            get;
            set;
        }

        internal override void Init()
        {
            switch (_resourceMode)
            {
                case ResourceMode.Standalone:
                    InitForStandalone();
                    break;
                case ResourceMode.Online:
                    InitForOnline();
                    break;
                default:
                    throw new NotSupportedException($"ResourceMode {_resourceMode} not supported.");
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

        private void InitForStandalone()
        {

        }

        private void InitForOnline()
        {

        }
    }
}