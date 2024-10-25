using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.Resource
{
    /// <summary>
    /// 内置文件系统，包含内置在游戏本体中的文件
    /// </summary>
    internal sealed partial class BuiltinFileSystem : IFileSystem
    {
        private readonly Dictionary<string, VFile> _files = new();

        public string RootDirectory { get; private set; }

        public int FileCount
        {
            get => _files.Count;
        }

        public UniTask InitAsync()
        {
            RootDirectory = PathHelper.Combine(Application.streamingAssetsPath, ResourceManagerConsts.ResourcePackFolderName);

            return UniTask.CompletedTask;
        }

        public async UniTask<string> LoadResourceVersionAsync()
        {
            string versionFilePath = PathHelper.Combine(RootDirectory, ResourceManagerConsts.ResourceVersionFileName);
            string versionFileURI = WebRequestHelper.ConvertToWWWURI(versionFilePath);

            WebRequestResult wwwResult = await WebRequestHelper.WebGetAsync(versionFileURI);
            if (wwwResult.Status == WebRequestStatus.Success)
            {
                string version = wwwResult.Text;
                return version;
            }
            else
            {
                throw new InvalidOperationException(wwwResult.Error);
            }
        }

        public async UniTask<Manifest> LoadManifestAsync()
        {
            string hashFilePath = PathHelper.Combine(RootDirectory, ResourceManagerConsts.ManifestHashFileName);
            string binaryFilePath = PathHelper.Combine(RootDirectory, ResourceManagerConsts.ManifestBinaryFileName);

            string hashFileURI = WebRequestHelper.ConvertToWWWURI(hashFilePath);
            string binaryFileURI = WebRequestHelper.ConvertToWWWURI(binaryFilePath);

            WebRequestResult hashResult = await WebRequestHelper.WebGetAsync(hashFileURI);
            if (hashResult.Status != WebRequestStatus.Success)
            {
                throw new InvalidOperationException(hashResult.Error);
            }
            string correctHash = hashResult.Text;

            WebRequestResult binaryResult = await WebRequestHelper.WebGetAsync(binaryFileURI);
            if (binaryResult.Status != WebRequestStatus.Success)
            {
                throw new InvalidOperationException(binaryResult.Error);
            }
            byte[] manifestData = binaryResult.Data;
            string manifestHash = HashHelper.BytesMD5(manifestData);

            if (manifestHash != correctHash)
            {
                throw new InvalidOperationException($"The manifest file '{binaryFilePath}' is corrupted. The MD5 checksum verification has failed.");
            }

            return ManifestSerilizer.DeserializeFromBytes(manifestData);
        }
    }
}