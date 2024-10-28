using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XFramework.Resource
{
    public class OnlineKernel : IResourceManagerKernel
    {
        private BuiltinFileSystem _builtinFileSystem;
        private CacheFileSystem _cacheFileSystem;

        private string _resourceVersion;
        private Manifest _manifest;

        public string RemoteRootDirectory { get; set; }

        public async UniTask InitAsync()
        {
            _builtinFileSystem = new BuiltinFileSystem(Application.streamingAssetsPath + "/ResourcePack");
            _cacheFileSystem = new CacheFileSystem(Application.persistentDataPath + "/ResourcePack", RemoteRootDirectory);

            _resourceVersion = await _cacheFileSystem.RequestResourceVersionAsync();
            _manifest = await _cacheFileSystem.LoadManifestAsync(_resourceVersion);

            throw new System.NotImplementedException();
        }

        public List<ManifestBundle> GetUpdatableBundles()
        {
            var result = new List<ManifestBundle>();
            foreach (ManifestBundle bundle in _manifest.Bundles)
            {
                if (_cacheFileSystem.CheckBundleUpdatable(bundle, _resourceVersion))
                {
                    result.Add(bundle);
                }
            }

            return result;
        }

        public async UniTask UpdateBundlesAsync(List<ManifestBundle> bundles, int retryCount = 3, float timeout = 60f)
        {
            foreach (ManifestBundle bundle in bundles)
            {
                await _cacheFileSystem.DownloadFileAsync(bundle.FileName, _resourceVersion, retryCount, timeout);
            }
        }

        public UniTask<object> LoadAssetByPathAsync(string assetPath)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<object> LoadAssetByAddressAsync(string address)
        {
            throw new System.NotImplementedException();
        }
    }
}