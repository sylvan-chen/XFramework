using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using XFramework.Utils;

namespace XFramework
{
    public sealed partial class ResourceManager
    {
        private readonly Dictionary<ResourceName, string> _cachedFileSystemNameForResourceName = new();

        private TimeoutController _timeoutController = new();

        public Action OnResourceInitComplete { get; set; }

        /// <summary>
        /// 初始化资源（单机模式）
        /// </summary>
        public async UniTask InitResourcesAsync()
        {
            if (_resourceMode != ResourceMode.Standalone)
            {
                throw new InvalidOperationException("InitResourcesAsync can only be called in standalone mode.");
            }

            // 从 StreamingAssets 中加载清单
            string fileURI = PathHelper.ConvertToWWWFilePath(Path.Combine(ReadOnlyPath, RemoteManifestFileName));
            LoadBytesResult result = await BytesHelper.LoadBytesAsync(fileURI);
            if (result.IsSuccess)
            {
                OnInitResourceSuccess(fileURI, result.Bytes, result.Duration);
            }
            else
            {
                Log.Error($"[XFramwork] [ResourceManager] Init resources failed because loading manifest failed. Error: {result.Error}");
            }
        }

        private void OnInitResourceSuccess(string fileURI, byte[] bytes, float duration)
        {

        }
    }
}