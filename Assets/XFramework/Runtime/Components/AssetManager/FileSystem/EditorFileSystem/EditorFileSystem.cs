using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.Resource
{
    /// <summary>
    /// 编辑器文件系统，用于编辑器下的模拟
    /// </summary>
    internal sealed class EditorFileSystem
    {
        public string RootDirectory { get; private set; }

        public int FileCount { get; private set; }

        public UniTask InitAsync()
        {
            RootDirectory = PathHelper.Combine(Application.dataPath, ResourceManagerSettings.ResourcePackFolderName);

            return UniTask.CompletedTask;
        }

        public async UniTask<string> LoadResourceVersionAsync()
        {
            string versionFilePath = PathHelper.Combine(RootDirectory, ResourceManagerSettings.ResourceVersionFileName);

            return await FileHelper.ReadAllTextAsync(versionFilePath);
        }

        public async UniTask<Manifest> LoadManifestAsync()
        {
            string hashFilePath = PathHelper.Combine(RootDirectory, ResourceManagerSettings.ManifestHashFileName);
            string binaryFilePath = PathHelper.Combine(RootDirectory, ResourceManagerSettings.ManifestBinaryFileName);
            if (!File.Exists(hashFilePath))
            {
                throw new InvalidOperationException($"LoadManifestAsync failed. {hashFilePath} not found.");
            }
            if (!File.Exists(binaryFilePath))
            {
                throw new InvalidOperationException($"LoadManifestAsync failed. {binaryFilePath} not found.");
            }

            // 先验证清单文件的 MD5 哈希值
            string correctHash = await FileHelper.ReadAllTextAsync(hashFilePath);
            byte[] manifestData = await FileHelper.ReadAllBytesAsync(binaryFilePath);
            string manifestHash = HashHelper.BytesMD5(manifestData);
            if (manifestHash != correctHash)
            {
                throw new InvalidOperationException($"LoadManifestAsync failed. The manifest file '{binaryFilePath}' is corrupted.");
            }

            // 再反序列化清单文件
            return ManifestSerilizer.DeserializeFromBytes(manifestData);
        }
    }
}