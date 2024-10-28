using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.Resource
{
    /// <summary>
    /// 缓存文件系统，用于访问远程资源，并在本地缓存以避免重复下载
    /// </summary>
    internal sealed class CacheFileSystem
    {
        private readonly string _cacheRootDirectory;
        private readonly string _remoteRootDirectory;

        public CacheFileSystem(string cacheRootDirectory, string remoteRootDirectory)
        {
            _cacheRootDirectory = cacheRootDirectory;
            _remoteRootDirectory = remoteRootDirectory;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 获取资源版本
        /// </summary>
        public async UniTask<string> RequestResourceVersionAsync()
        {
            string remoteVersionFilePath = GetRemoteFilePath(ResourceManagerSettings.GetResourceVersionFileName());

            WebRequestResult result = await WebRequestHelper.WebGetBufferAsync(remoteVersionFilePath);
            if (result.Status == WebRequestStatus.Success)
            {
                string version = result.DownloadBuffer.Text;
                return version;
            }
            else
            {
                throw new InvalidOperationException(result.Error);
            }
        }

        /// <summary>
        /// 加载对应版本的资源清单
        /// </summary>
        /// <param name="resourceVersion">资源版本</param>
        public async UniTask<Manifest> LoadManifestAsync(string resourceVersion)
        {
            string manifestHashFileName = ResourceManagerSettings.GetManifestHashFileName(resourceVersion);
            string cacheManifestHashFilePath = GetCacheFilePath(manifestHashFileName, resourceVersion);
            if (!FileHelper.Exists(cacheManifestHashFilePath))
            {
                // 如果本地缓存中没有 Hash 文件，则从远程下载到本地
                string remoteManifestHashFilePath = GetRemoteFilePath(manifestHashFileName);
                WebRequestResult wwwResult = await WebRequestHelper.WebGetFileAsync(remoteManifestHashFilePath, cacheManifestHashFilePath);
                if (wwwResult.Status != WebRequestStatus.Success)
                {
                    throw new InvalidOperationException(wwwResult.Error);
                }
            }
            string correctHash = FileHelper.ReadAllText(cacheManifestHashFilePath);

            string manifestBinaryFileName = ResourceManagerSettings.GetManifestBinaryFileName(resourceVersion);
            string cacheManifestBinaryFilePath = GetCacheFilePath(manifestBinaryFileName, resourceVersion);
            if (!FileHelper.Exists(cacheManifestBinaryFilePath))
            {
                // 如果本地缓存中没有 Manifest 文件，则从远程下载到本地
                string remoteManifestBinaryFilePath = GetRemoteFilePath(manifestBinaryFileName);
                WebRequestResult wwwResult = await WebRequestHelper.WebGetFileAsync(remoteManifestBinaryFilePath, cacheManifestBinaryFilePath);
                if (wwwResult.Status != WebRequestStatus.Success)
                {
                    throw new InvalidOperationException(wwwResult.Error);
                }
            }
            byte[] manifestData = FileHelper.ReadAllBytes(cacheManifestBinaryFilePath);

            string manifestHash = HashHelper.BytesMD5(manifestData);
            if (manifestHash != correctHash)
            {
                throw new InvalidOperationException($"LoadManifestAsync failed. The manifest file '{cacheManifestBinaryFilePath}' is corrupted.");
            }

            return ManifestSerilizer.DeserializeFromBytes(manifestData);
        }

        /// <summary>
        /// 检查资源包是否需要更新
        /// </summary>
        /// <param name="bundle">资源包</param>
        /// <param name="resourceVersion">资源版本</param>
        public bool CheckBundleUpdatable(ManifestBundle bundle, string resourceVersion)
        {
            // 1. bundle 文件是否存在
            string cacheBundleFilePath = GetCacheFilePath(bundle.FileName, resourceVersion);
            if (!FileHelper.Exists(cacheBundleFilePath))
            {
                return true;
            }
            // 2. 验证文件大小
            long fileSize = FileHelper.GetFileSize(cacheBundleFilePath);
            if (fileSize != bundle.FileSize)
            {
                return true;
            }
            // 3. 验证完整性
            byte[] bundleData = FileHelper.ReadAllBytes(cacheBundleFilePath);
            string bundleHash = HashHelper.BytesMD5(bundleData);
            if (bundleHash != bundle.Hash)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 下载远程文件到本地缓存
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="resourceVersion">资源版本</param>
        /// <param name="retryCount">重试次数</param>
        /// <param name="timeout">超时时间</param>
        public async UniTask DownloadFileAsync(string fileName, string resourceVersion, int retryCount = 3, float timeout = 60f)
        {
            string cacheFilePath = GetCacheFilePath(fileName, resourceVersion);
            string remoteFilePath = GetRemoteFilePath(fileName);

            if (FileHelper.Exists(cacheFilePath))
            {
                FileHelper.Delete(cacheFilePath);
            }

            while (retryCount > 0)
            {
                WebRequestResult wwwResult = await WebRequestHelper.WebGetFileAsync(remoteFilePath, cacheFilePath);
                if (wwwResult.Status == WebRequestStatus.Success)
                {
                    return;
                }
                retryCount--;
                if (retryCount == 0)
                {
                    throw new InvalidOperationException(wwwResult.Error);
                }
            }
        }

        public async UniTask<AssetBundle> LoadBundleAsync(string bundleFileName, string resourceVersion)
        {
            string cacheBundleFilePath = GetCacheFilePath(bundleFileName, resourceVersion);
            if (!FileHelper.Exists(cacheBundleFilePath))
            {
                await DownloadFileAsync(bundleFileName, resourceVersion);
            }

            return await AssetBundle.LoadFromFileAsync(cacheBundleFilePath);
        }

        private string GetCacheFilePath(string fileName, string resourceVersion)
        {
            return PathHelper.Combine(_cacheRootDirectory, resourceVersion, fileName);
        }

        private string GetRemoteFilePath(string fileName)
        {
            return PathHelper.Combine(_remoteRootDirectory, fileName);
        }
    }
}